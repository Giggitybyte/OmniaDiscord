Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.Entities
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Entities.Database
Imports OmniaDiscord.Services

Namespace Commands.Modules
    Public Class AdministrationModule
        Inherits OmniaCommandBase

        Private _adminService As AdministrationService

        Sub New(adminService As AdministrationService)
            _adminService = adminService
        End Sub

        <Command("warn"), RequireStaff>
        <Description("Gives a user a warning. An acculative total of 3 warnings will have a user kicked.")>
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

            If Not GuildData.MemberWarnings.ContainsKey(user.Id) Then GuildData.MemberWarnings.Add(user.Id, 0)
            GuildData.MemberWarnings(user.Id) += 1

            Dim embed As New DiscordEmbedBuilder With {
                .Color = DiscordColor.SpringGreen,
                .Description = $"{user.Mention} now has {GuildData.MemberWarnings(user.Id)} warnings."
            }

            If GuildData.MemberWarnings(user.Id) >= 3 Then
                embed.Description &= $"{Environment.NewLine}This user has been kicked."
                Await user.RemoveAsync(reason:=$"Accumulated 3 warnings. Responsible user: {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
                GuildData.MemberWarnings.Remove(user.Id)
            End If

            UpdateGuildData()
            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("warnings")>
        <Description("Displays the number of warnings a user has.")>
        <RequireBotPermissions(Permissions.EmbedLinks)>
        Public Async Function WarningsCommand(ctx As CommandContext, Optional user As DiscordMember = Nothing) As Task
            If user Is Nothing Then user = ctx.Member
            Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                .Color = DiscordColor.CornflowerBlue,
                .Description = $"{user.Mention} has {If(GuildData.MemberWarnings.ContainsKey(user.Id), GuildData.MemberWarnings(user.Id), 0)} warnings."
            })
        End Function

        <Command("forgive"), RequireStaff>
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

            If Not GuildData.MemberWarnings.ContainsKey(user.Id) OrElse GuildData.MemberWarnings(user.Id) = 0 Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"{user.Mention} does not have any warnings to forgive."
                })
                Return
            End If

            GuildData.MemberWarnings(user.Id) -= 1
            Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                .Color = DiscordColor.SpringGreen,
                .Description = $"{user.Mention} now has {GuildData.MemberWarnings(user.Id)} warnings."
            })

            If GuildData.MemberWarnings(user.Id) = 0 Then GuildData.MemberWarnings.Remove(user.Id)
            UpdateGuildData()
        End Function

        <Command("mute")>
        <Description("Prevents the specified user from typing in text channels and speaking in voice channels.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles Or Permissions.ManageChannels Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Helper)>
        Public Async Function MuteCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            Dim role As DiscordRole

            If GuildSettings.MutedRoleId = 0 OrElse Not ctx.Guild.Roles.ContainsKey(GuildSettings.MutedRoleId) Then
                Dim message = Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Orange, .Description = "Configuring muted role..."})
                role = Await _adminService.CreateGuildMutedRoleAsync(ctx.Guild)
                Await message.DeleteAsync
            End If

            If role Is Nothing Then role = ctx.Guild.Roles(GuildSettings.MutedRoleId)

            If user.Id = ctx.Member.Id Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Description = "You cannot mute yourself."})
                Return
            End If

            If GuildData.MutedMembers.Contains(user.Id) AndAlso user.Roles.FirstOrDefault(Function(r) r.Id = role.Id) IsNot Nothing Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Description = "This user is already muted."})
                Return
            End If

            Dim logReason = $"muted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await user.GrantRoleAsync(role, logReason)
            If Not ctx.Guild.Members(user.Id).VoiceState?.IsServerMuted Then Await ctx.Guild.Members(user.Id).SetMuteAsync(True, logReason)

            GuildData.MutedMembers.Add(user.Id)
            UpdateGuildData()

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("unmute")>
        <Description("Allows a previously muted user to speak and send messages.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Helper)>
        Public Async Function UnmuteCommand(ctx As CommandContext, user As DiscordMember) As Task
            If Not GuildData.MutedMembers.Contains(user.Id) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Description = "This user is not muted."})
                Return
            End If

            GuildData.MutedMembers.Remove(user.Id)
            UpdateGuildData()

            Dim logReason = $"unmuted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})"
            If ctx.Guild.Roles.ContainsKey(GuildSettings.MutedRoleId) Then Await user.RevokeRoleAsync(ctx.Guild.Roles(GuildSettings.MutedRoleId), logReason)
            If user.VoiceState?.IsServerMuted Then Await user.SetMuteAsync(False, logReason)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("kick")>
        <Description("Kicks a user from the server.")>
        <RequireBotPermissions(Permissions.KickMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function KickCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Or user.Id = ctx.Guild.CurrentMember.Id Then Return
            If Not IsOmniaRolePositionHigher(ctx, user) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                   .Color = DiscordColor.Red,
                   .Description = $"The highest role that {user.Mention} has is higher than my highest role.{Environment.NewLine}As a result, I cannot kick them."
                })
                Return
            End If

            Dim logReason = $"kicked by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await user.RemoveAsync(logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("ban")>
        <Description("Bans a user from this server. Users who are not currently in this server can still be banned if their user ID is provided.")>
        <RequireBotPermissions(Permissions.BanMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function BanCommand(ctx As CommandContext, user As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Or user.Id = ctx.Guild.CurrentMember.Id Then Return
            If Not IsOmniaRolePositionHigher(ctx, user) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                   .Color = DiscordColor.Red,
                   .Description = $"The highest role that {user.Mention} has is higher than my highest role.{Environment.NewLine}As a result, I cannot ban them."
                })
                Return
            End If

            Dim logReason = $"banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await ctx.Guild.BanMemberAsync(user.Id, reason:=logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

#Disable Warning BC42358
        <Command("softban"), Aliases("sban")>
        <Description("Bans then unbans a user after five minutes.")>
        <RequireBotPermissions(Permissions.BanMembers Or Permissions.AddReactions)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function SoftBanCommand(ctx As CommandContext, user As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Or user.Id = ctx.Guild.CurrentMember.Id Then Return
            If Not IsOmniaRolePositionHigher(ctx, user) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                   .Color = DiscordColor.Red,
                   .Description = $"The highest role that {user.Mention} has is higher than my highest role.{Environment.NewLine}As a result, I cannot softban them."
                })
                Return
            End If

            Dim logReason = $"soft banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: "
            logReason &= If(String.IsNullOrWhiteSpace(reason), "none provided", New String(reason.Take(512 - logReason.Count).ToArray))

            Await ctx.Guild.BanMemberAsync(user.Id, reason:=logReason)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))

            Dim cts As New CancellationTokenSource
            _adminService.SoftbanTokens.TryAdd(ctx.Guild.Id, cts)
            Task.Delay(300000, cts.Token).ContinueWith(Sub() ctx.Guild.UnbanMemberAsync(user.Id, "Soft ban ended"), TaskContinuationOptions.NotOnCanceled)
        End Function
#Enable Warning BC42358

        <Command("unban")>
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

        Private Function IsOmniaRolePositionHigher(ctx As CommandContext, member As DiscordMember) As Boolean
            If ctx.Guild.CurrentMember.Roles.Any(Function(r) r.CheckPermission(Permissions.Administrator)) Then Return True
            Dim omniaHighest = ctx.Guild.CurrentMember.Roles.OrderByDescending(Function(r) r.Position).FirstOrDefault
            Dim memberHighest = member.Roles.OrderByDescending(Function(r) r.Position).FirstOrDefault
            Return omniaHighest?.Position > memberHighest?.Position
        End Function

        <Group("prune"), Aliases("purge", "remove")>
        <Description("Bulk deletes messages. `messageCount` defaults to 100.")>
        <RequireBotPermissions(Permissions.ManageMessages Or Permissions.AddReactions)>
        <RequireStaff>
        Public Class PruneModule
            Inherits OmniaCommandBase

            <GroupCommand>
            Public Async Function PruneCommand(ctx As CommandContext, Optional messageCount As ULong = 100) As Task
                Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, messageCount)).ToList
                messages.RemoveAll(Function(m) m.CreationTimestamp < Date.Now.AddDays(-14))

                Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
            End Function

            <Command("usermessages"), Aliases("user", "u")>
            <Description("Gets all messages from the specified user in the last 1,000 messages and bulk deletes them. `messageCount` defaults to 100.")>
            Public Async Function RemoveMessagesFromSpecificUserCommand(ctx As CommandContext, targetUser As String, Optional messageCount As ULong = 100) As Task
                Await ctx.TriggerTypingAsync
                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim convert = Await New DiscordUserConverter().ConvertAsync(targetUser, ctx)
                Dim user = If(convert.HasValue, convert.Value, Nothing)

                If user Is Nothing Then
                    With embed
                        .Title = "Invalid User"
                        .Description = "The user you specified was either invalid or does not exist."
                    End With

                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                Dim twoWeeksAgo As Date = Date.Now.AddDays(-14)
                Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, 1000)).ToList
                messages.RemoveAll(Function(m) Not m.Author.Id = user.Id And m.CreationTimestamp > twoWeeksAgo)

                If messages.Count = 0 Then
                    With embed
                        .Title = "Couldn't Delete Messages"
                        .Description = $"No messages sent within last two weeks were sent by {user.Mention}"
                    End With

                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
            End Function

            <Command("botmessages"), Aliases("bots", "b")>
            <Description("Gets all messages from bots in the last 1,000 messages and bulk deletes them. `messageCount` defaults to 100.")>
            Public Async Function RemoveMessagesFromBotsCommand(ctx As CommandContext, Optional messageCount As ULong = 100) As Task
                Dim twoWeeksAgo As Date = Date.Now.AddDays(-14)
                Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, 1000)).ToList
                messages.RemoveAll(Function(m) Not m.Author.IsBot)
                messages.RemoveAll(Function(m) m.CreationTimestamp < twoWeeksAgo)

                If messages.Count = 0 Then
                    Dim embed As New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Title = "Couldn't Delete Messages",
                        .Description = "No messages sent within the last two weeks were sent by a bot."
                    }

                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
            End Function

        End Class

    End Class
End Namespace