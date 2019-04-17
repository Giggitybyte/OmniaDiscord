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
        Inherits OmniaCommandBase

        Private _softBans As SoftbanService
        Private _muteService As MuteService

        Sub New(sbService As SoftbanService, muteService As MuteService)
            _softBans = sbService
            _muteService = muteService
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
                    Task.Delay(300000, cts.Token).ContinueWith(Sub() guild.UnbanMemberAsync(userId, "Soft ban ended"), TaskContinuationOptions.NotOnCanceled)
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
                    If _softBans.BanCancellationTokens.TryRemove(ctx.Guild.Id, cts) Then cts.Cancel()

                    Await ctx.Guild.UnbanMemberAsync(targetUser.Value, CreateReason(ctx, embed, targetUser.Value, "unbanned", reason))
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("mute")>
        <Description("Prevents the specified user from typing in text channels and speaking in voice channels.")>
        <RequireBotPermissions(Permissions.MuteMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function MuteCommand(ctx As CommandContext, user As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim targetMember As [Optional](Of DiscordMember) = Await New DiscordMemberConverter().ConvertAsync(user, ctx)

            If Not targetMember.HasValue Then
                embed.Description = "The user you specified either is not in this server, or doesn't exist."

            Else
                GuildData.MutedMembers.Add(targetMember.Value.Id)
                UpdateGuildData()
                Await targetMember.Value.SetMuteAsync(True, CreateReason(ctx, embed, targetMember.Value, "muted", reason))
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("unmute")>
        <Description("Allows a previously muted user to speak and send messages.")>
        <RequireBotPermissions(Permissions.MuteMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function UnmuteCommand(ctx As CommandContext, user As String) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim targetMember As [Optional](Of DiscordMember) = Await New DiscordMemberConverter().ConvertAsync(user, ctx)

            If Not targetMember.HasValue Then
                embed.Description = "The user you specified either is not in this server, or doesn't exist."

            ElseIf GuildData.MutedMembers.Contains(targetMember.Value.Id) Then
                GuildData.MutedMembers.Remove(targetMember.Value.Id)
                UpdateGuildData()
                Await targetMember.Value.SetMuteAsync(False, CreateReason(ctx, embed, targetMember.Value, "unmuted", String.Empty))
                Await ctx.TriggerTypingAsync

                With embed
                    .Color = DiscordColor.Orange
                    .Description = "Removing channel overwrites..."
                End With

                Dim message As DiscordMessage = Await ctx.RespondAsync(embed:=embed.Build)
                Dim overwrites As New List(Of DiscordOverwrite)
                Dim textChannels As IEnumerable(Of DiscordChannel) = (Await ctx.Guild.GetChannelsAsync).Where(Function(c) c.Type = ChannelType.Text)

                For Each channel As DiscordChannel In textChannels
                    For Each chnOverwrite As DiscordOverwrite In channel.PermissionOverwrites

                        If chnOverwrite.Type = OverwriteType.Member AndAlso
                           (Await chnOverwrite.GetMemberAsync).Id = targetMember.Value.Id Then

                            overwrites.Add(chnOverwrite)
                        End If

                    Next
                Next

                For Each overwrite As DiscordOverwrite In overwrites
                    Await overwrite.DeleteAsync
                Next

                Await message.DeleteAsync

            Else
                embed.Description = "The user you specified is not currently muted."
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("voicekick"), Aliases("vkick")>
        <Description("Generates a temporary voice channel, moves the target user to that voice channel, then deletes the temporary channel. This effectively force disconnects a user.")>
        <RequireBotPermissions(Permissions.MoveMembers Or Permissions.ManageChannels)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function VoiceKickCommand(ctx As CommandContext, user As String) As Task
            'Dim overwrites As New List(Of DiscordOverwriteBuilder)
            'overwrites.Add(New DiscordOverwriteBuilder().For(ctx.Guild.EveryoneRole).Deny(Permissions.AccessChannels))

            'Await ctx.Guild.CreateVoiceChannelAsync($"Temporary Channel - {Utilities.GenerateRandomChars(8)}",
            '                                        overwrites:={New DiscordOverwriteBuilder().For(ctx.Guild.EveryoneRole).Deny(Permissions.AccessChannels)},
            '                                        user_limit:=1,
            '                                        reason:=CreateReason(ctx))
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

        <Group("prune"), Aliases("purge", "remove")>
        <Description("Allows for the bulk deletion of messages. `messageCount` defaults to 100.")>
        <RequireBotPermissions(Permissions.ManageMessages Or Permissions.AddReactions)>
        <RequireStaff>
        Public Class PruneModule
            Inherits OmniaCommandBase

            <GroupCommand>
            Public Async Function PruneCommand(ctx As CommandContext, Optional messageCount As ULong = 100) As Task
                Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, messageCount)).ToList
                messages.RemoveAll(Function(m) m.CreationTimestamp < Date.Now.AddDays(-14))

                Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
            End Function

            <Command("usermessages"), Aliases("user", "u")>
            <Description("Gets all messages from the specified user in the last 1,000 messages and bulk deletes them. `messageCount` defaults to 100.")>
            Public Async Function RemoveMessagesFromSpecificUserCommand(ctx As CommandContext, user As String, Optional messageCount As ULong = 100) As Task
                Await ctx.TriggerTypingAsync
                Dim targetUser As [Optional](Of DiscordUser) = Await New DiscordUserConverter().ConvertAsync(user, ctx)

                If targetUser.HasValue Then
                    Dim twoWeeksAgo As Date = Date.Now.AddDays(-14)
                    Dim initalMessageCount As String
                    Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, 1000)).ToList
                    initalMessageCount = messages.Count.ToString("N0")
                    messages.RemoveAll(Function(m) Not m.Author.Id = targetUser.Value.Id)
                    messages.RemoveAll(Function(m) m.CreationTimestamp < twoWeeksAgo)

                    If messages.Count = 0 Then
                        Dim embed As New DiscordEmbedBuilder With {
                            .Color = DiscordColor.Red,
                            .Title = "Couldn't Delete Messages",
                            .Description = $"Either none of the last {initalMessageCount} messages were from {targetUser.Value.Mention} or none of those messages were sent less than two weeks ago."
                        }

                        Await ctx.RespondAsync(embed:=embed.Build)
                    Else
                        Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                        Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
                    End If

                Else
                    Dim embed As New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Title = "Invalid User",
                        .Description = "The user you specified was either invalid or does not exist."
                    }

                    Await ctx.RespondAsync(embed:=embed.Build)
                End If

            End Function

            <Command("botmessages"), Aliases("bots", "b")>
            <Description("Gets all messages from bots in the last 1,000 messages and bulk deletes them. `messageCount` defaults to 100.")>
            Public Async Function RemoveMessagesFromBotsCommand(ctx As CommandContext, Optional messageCount As ULong = 100) As Task
                Dim twoWeeksAgo As Date = Date.Now.AddDays(-14)
                Dim initalMessageCount As Integer
                Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, 1000)).ToList
                initalMessageCount = messages.Count
                messages.RemoveAll(Function(m) Not m.Author.IsBot)
                messages.RemoveAll(Function(m) m.CreationTimestamp < twoWeeksAgo)

                If messages.Count = 0 Then
                    Dim embed As New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Title = "Couldn't Delete Messages",
                        .Description = $"Either none of the last {initalMessageCount} messages were from a bot or none of those messages were sent less than two weeks ago."
                    }

                    Await ctx.RespondAsync(embed:=embed.Build)
                Else
                    Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                    Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
                End If

            End Function

        End Class

    End Class
End Namespace