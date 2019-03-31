Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.Entities
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Entites
Imports OmniaDiscord.Services

Namespace Commands.Modules
    Public Class AdministrationModule
        Inherits OmniaBaseModule

        Private _softBans As SoftbanService

        Sub New(sbService As SoftbanService)
            _softBans = sbService
        End Sub

        <Command("kick")>
        <Description("Kicks a user from the server.")>
        <RequireBotPermissions(Permissions.KickMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function KickCommand(ctx As CommandContext, user As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim targetMember As [Optional](Of DiscordMember) = Await New DiscordMemberConverter().ConvertAsync(user, ctx)

            If Not targetMember.HasValue Then
                embed.Description = "The user you specified either is not in this server, or doesn't exist."
            Else
                If targetMember.Value.Id = ctx.Member.Id Then
                    embed.Description = "You cannot kick yourself!"
                Else
                    Await targetMember.Value.RemoveAsync(CreateReason(ctx, embed, targetMember, "kicked", reason))
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("ban")>
        <Description("Bans a user from this server. Users who are not currently in this server can still be banned if their user ID is provided.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function BanCommand(ctx As CommandContext, user As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim targetUser As [Optional](Of DiscordUser) = Await New DiscordUserConverter().ConvertAsync(user, ctx)

            If Not targetUser.HasValue Then
                embed.Description = "The user you specified was either invalid or does not exist."
            Else
                If targetUser.Value.Id = ctx.Member.Id Then
                    embed.Description = "You cannot ban yourself!"
                Else
                    Await ctx.Guild.BanMemberAsync(targetUser.Value.Id, 0, CreateReason(ctx, embed, targetUser, "banned", reason))
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("softban"), Aliases("sban")>
        <Description("Bans a user then unbans them after five minutes.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function SoftBanCommand(ctx As CommandContext, user As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim targetMember As [Optional](Of DiscordMember) = Await New DiscordMemberConverter().ConvertAsync(user, ctx)

            If Not targetMember.HasValue Then
                embed.Description = "The user you specified either is not in this server, or doesn't exist."

            Else
                If targetMember.Value.Id = ctx.Member.Id Then
                    embed.Description = "You cannot soft ban yourself!"

                Else
                    Dim guild As DiscordGuild = ctx.Guild
                    Dim userId As ULong = targetMember.Value.Id
                    Dim cts As New CancellationTokenSource

                    _softBans.BanCancellationTokens.TryAdd(ctx.Guild.Id, cts)
                    Await guild.BanMemberAsync(userId, 0, CreateReason(ctx, embed, targetMember.Value, "soft banned", reason))
                    Task.Delay(60000, cts.Token).ContinueWith(Sub() guild.UnbanMemberAsync(userId, "Soft ban ended"), TaskContinuationOptions.NotOnCanceled)
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("unban")>
        <Description("Removes a ban from a previously banned user. Unless the ban was recent, it's better to use a user ID to unban a user.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function UnbanCommand(ctx As CommandContext, user As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim targetUser As [Optional](Of DiscordUser) = Await New DiscordUserConverter().ConvertAsync(user, ctx)

            If Not targetUser.HasValue Then
                embed.Description = "The user you specified was either invalid or does not exist."

            Else
                Dim userBan As DiscordBan = (Await ctx.Guild.GetBansAsync).FirstOrDefault(Function(b) b.User.Id = targetUser.Value.Id)

                If userBan Is Nothing Then
                    embed.Description = "The user you specified is not currently banned."

                Else
                    Dim cts As CancellationTokenSource
                    If _softBans.BanCancellationTokens.TryRemove(ctx.Guild.Id, cts) Then
                        cts.Cancel()
                    End If

                    Await ctx.Guild.UnbanMemberAsync(targetUser.Value, CreateReason(ctx, embed, targetUser.Value, "unbanned", reason))
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        Private Function CreateReason(ctx As CommandContext, ByRef embed As DiscordEmbedBuilder,
                                           target As DiscordUser, action As String, reason As String) As String

            Dim auditLog As String = $"{action} by {ctx.Member.Username}#{ctx.Member.Discriminator}"
            Dim response As String = $"{target.Mention} was {action} by {ctx.Member.Mention}"

            reason = reason.Trim

            If reason.Length > 0 Then
                auditLog &= $": '{reason.Substring(0, If(reason.Length > 448, 448, reason.Length))}'"
                response &= $" with reason{Environment.NewLine}{Formatter.BlockCode(reason)}"
            End If

            With embed
                .Description = response
                .Color = DiscordColor.CornflowerBlue
            End With

            Return auditLog
        End Function
    End Class
End Namespace