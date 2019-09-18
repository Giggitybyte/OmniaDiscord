Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Commands.Bases
Imports OmniaDiscord.Entities.Attributes
Imports OmniaDiscord.Entities.Database
Imports OmniaDiscord.Services

Namespace Commands.Modules
    Public Class AdministrationModule
        Inherits OmniaDbCommandBase

        Private _adminService As AdministrationService

        Sub New(adminService As AdministrationService)
            _adminService = adminService
        End Sub

        <Command("warn"), RequireGuild, RequireStaff>
        <Description("Gives a user a warning." + vbCrLf + " An acculative total of 3 warnings will have a user kicked.")>
        <RequireBotPermissions(Permissions.KickMembers Or Permissions.BanMembers Or Permissions.EmbedLinks)>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function WarnCommand(ctx As CommandContext, user As DiscordMember) As Task
            If user.Id = ctx.Member.Id Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"You cannot issue warnings to yourself."
                })
                Return
            End If

            If Not DbGuild.Data.MemberWarnings.ContainsKey(user.Id) Then DbGuild.Data.MemberWarnings.Add(user.Id, 0)
            DbGuild.Data.MemberWarnings(user.Id) += 1

            Dim embed As New DiscordEmbedBuilder With {
                .Color = DiscordColor.SpringGreen,
                .Description = $"{user.Mention} now has {DbGuild.Data.MemberWarnings(user.Id)} warnings."
            }

            If DbGuild.Data.MemberWarnings(user.Id) >= 3 Then
                embed.Description &= $"{Environment.NewLine}This user has been kicked."
                Await user.RemoveAsync(reason:=$"Accumulated 3 warnings. Responsible user: {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
                DbGuild.Data.MemberWarnings.Remove(user.Id)
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("warnings"), RequireGuild>
        <Description("Displays the number of warnings a user has.")>
        <RequireBotPermissions(Permissions.EmbedLinks)>
        Public Async Function WarningsCommand(ctx As CommandContext, Optional user As DiscordMember = Nothing) As Task
            If user Is Nothing Then user = ctx.Member
            Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                .Color = DiscordColor.CornflowerBlue,
                .Description = $"{user.Mention} has {If(DbGuild.Data.MemberWarnings.ContainsKey(user.Id), DbGuild.Data.MemberWarnings(user.Id), 0)} warnings."
            })
        End Function

        <Command("forgive"), RequireGuild, RequireStaff>
        <Description("Removes a warning from a user.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function ForgiveCommand(ctx As CommandContext, user As DiscordMember) As Task
            If user.Id = ctx.Member.Id Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"You cannot forgive warnings issued to you."
                })
                Return
            End If

            If Not DbGuild.Data.MemberWarnings.ContainsKey(user.Id) OrElse DbGuild.Data.MemberWarnings(user.Id) = 0 Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"{user.Mention} does not have any warnings to forgive."
                })
                Return
            End If

            DbGuild.Data.MemberWarnings(user.Id) -= 1
            Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                .Color = DiscordColor.SpringGreen,
                .Description = $"{user.Mention} now has {DbGuild.Data.MemberWarnings(user.Id)} warnings."
            })

            If DbGuild.Data.MemberWarnings(user.Id) = 0 Then DbGuild.Data.MemberWarnings.Remove(user.Id)
        End Function

        <Command("mute"), RequireGuild>
        <Description("Prevents the specified user from typing in text channels and speaking in voice channels." + vbCrLf + "A mute will persist until the user is explicity unmuted.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles Or Permissions.ManageChannels Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Helper)>
        Public Async Function MuteCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            Dim role As DiscordRole

            If DbGuild.Data.MutedRoleId = 0 OrElse Not ctx.Guild.Roles.ContainsKey(DbGuild.Data.MutedRoleId) Then
                Dim message = Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Orange, .Description = "Configuring muted role..."})
                Await ctx.TriggerTypingAsync
                role = Await _adminService.CreateGuildMutedRoleAsync(ctx.Guild)
                Await message.DeleteAsync
            End If

            If role Is Nothing Then role = ctx.Guild.Roles(DbGuild.Data.MutedRoleId)

            If user.Id = ctx.Member.Id Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Description = "You cannot mute yourself."})
                Return
            End If

            If DbGuild.Data.MutedMembers.Contains(user.Id) AndAlso user.Roles.FirstOrDefault(Function(r) r.Id = role.Id) IsNot Nothing Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Description = "This user is already muted."})
                Return
            End If

            Dim logReason = $"muted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await user.GrantRoleAsync(role, logReason)
            If Not ctx.Guild.Members(user.Id).VoiceState?.IsServerMuted Then Await ctx.Guild.Members(user.Id).SetMuteAsync(True, logReason)
            DbGuild.Data.MutedMembers.Add(user.Id)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("unmute"), RequireGuild>
        <Description("Allows a previously muted user to speak and send messages.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Helper)>
        Public Async Function UnmuteCommand(ctx As CommandContext, user As DiscordMember) As Task
            If Not DbGuild.Data.MutedMembers.Contains(user.Id) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Description = "This user is not muted."})
                Return
            End If

            DbGuild.Data.MutedMembers.Remove(user.Id)

            Dim logReason = $"unmuted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})"
            If ctx.Guild.Roles.ContainsKey(DbGuild.Data.MutedRoleId) Then Await user.RevokeRoleAsync(ctx.Guild.Roles(DbGuild.Data.MutedRoleId), logReason)
            If user.VoiceState?.IsServerMuted Then Await user.SetMuteAsync(False, logReason)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("kick"), RequireGuild>
        <Description("Kicks a user from the server with an optional reason.")>
        <RequireBotPermissions(Permissions.KickMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function KickCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Or user.Id = ctx.Guild.CurrentMember.Id Then Return
            If Not Await RolePositionCheckAsync(ctx, user, "kick") Then Return

            Dim logReason = $"kicked by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await user.RemoveAsync(logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("ban"), RequireGuild>
        <Description("Bans a user from this server with an optional reason." + vbCrLf + " Users who are not currently in this server can still be banned if their user ID is provided.")>
        <RequireBotPermissions(Permissions.BanMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function BanCommand(ctx As CommandContext, user As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Or user.Id = ctx.Guild.CurrentMember.Id Then Return
            If ctx.Guild.Members.ContainsKey(user.Id) AndAlso Not Await RolePositionCheckAsync(ctx, ctx.Guild.Members(user.Id), "ban") Then Return

            Dim logReason = $"banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await ctx.Guild.BanMemberAsync(user.Id, reason:=logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

#Disable Warning BC42358
        <Command("softban"), RequireGuild, Aliases("sban")>
        <Description("Bans then unbans a user after five minutes.")>
        <RequireBotPermissions(Permissions.BanMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function SoftBanCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Or user.Id = ctx.Guild.CurrentMember.Id Then Return
            If Not Await RolePositionCheckAsync(ctx, user, "softban") Then Return

            Dim logReason = $"soft banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await ctx.Guild.BanMemberAsync(user.Id, reason:=logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))

            Dim cts As New CancellationTokenSource
            _adminService.SoftbanTokens.TryAdd(ctx.Guild.Id, cts)
            Task.Delay(300000, cts.Token).ContinueWith(Sub() ctx.Guild.UnbanMemberAsync(user.Id, "Soft ban ended"), TaskContinuationOptions.NotOnCanceled)
        End Function
#Enable Warning BC42358

        <Command("unban"), RequireGuild>
        <Description("Removes a ban from a previously banned user.")>
        <RequireBotPermissions(Permissions.BanMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function UnbanCommand(ctx As CommandContext, userId As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If userId.Id = ctx.Member.Id Then Return
            Dim userBan As DiscordBan = (Await ctx.Guild.GetBansAsync).FirstOrDefault(Function(b) b.User.Id = userId.Id)
            If userBan Is Nothing Then Return

            Dim cts As CancellationTokenSource
            If _adminService.SoftbanTokens.TryRemove(ctx.Guild.Id, cts) Then cts.Cancel()

            Dim logReason = $"unbanned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await ctx.Guild.UnbanMemberAsync(userId.Id, logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        Private Async Function RolePositionCheckAsync(ctx As CommandContext, member As DiscordMember, action As String) As Task(Of Boolean)
            If ctx.Guild.CurrentMember.Roles.Any(Function(r) r.CheckPermission(Permissions.Administrator)) Then Return True
            Dim omniaHighest = ctx.Guild.CurrentMember.Roles.OrderByDescending(Function(r) r.Position).FirstOrDefault
            Dim memberHighest = member.Roles.OrderByDescending(Function(r) r.Position).FirstOrDefault
            Dim result = omniaHighest?.Position > memberHighest?.Position

            If Not result Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                   .Color = DiscordColor.Red,
                   .Description = $"The highest role that {member.Mention} has is higher than my highest role.{Environment.NewLine}As a result, I cannot {action} them."
                })
            End If

            Return result
        End Function
    End Class
End Namespace