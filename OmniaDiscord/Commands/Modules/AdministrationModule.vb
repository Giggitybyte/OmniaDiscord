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

        Private _adminService As AdministrativeService
        Private _userConverter As DiscordUserConverter

        Sub New(adminService As AdministrativeService)
            _adminService = adminService
            _userConverter = New DiscordUserConverter
        End Sub

        <Command("kick")>
        <Description("Kicks a user from the server.")>
        <RequireBotPermissions(Permissions.KickMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function KickCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Then Return
            Await user.RemoveAsync($"kicked by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: {If(reason?.Any, reason, "None.")}")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("ban")>
        <Description("Bans a user from this server. Users who are not currently in this server can still be banned if their user ID is provided.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function BanCommand(ctx As CommandContext, user As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Then Return
            Await ctx.Guild.BanMemberAsync(user.Id, reason:=$"banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: {If(reason?.Any, reason, "None.")}")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

#Disable Warning BC42358
        <Command("softban"), Aliases("sban")>
        <Description("Bans then unbans a user after five minutes.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function SoftBanCommand(ctx As CommandContext, user As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If user.Id = ctx.Member.Id Then Return
            Dim auditLog = $"soft banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: {If(reason?.Any, reason, "None.")}"
            Await ctx.Guild.BanMemberAsync(user.Id, reason:=auditLog)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))

            Dim cts As New CancellationTokenSource
            _adminService.SoftbanTokens.TryAdd(ctx.Guild.Id, cts)
            Task.Delay(300000, cts.Token).ContinueWith(Sub() ctx.Guild.UnbanMemberAsync(user.Id, "Soft ban ended"), TaskContinuationOptions.NotOnCanceled)
        End Function
#Enable Warning BC42358

        <Command("unban")>
        <Description("Removes a ban from a previously banned user.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function UnbanCommand(ctx As CommandContext, userId As DiscordUser, <RemainingText> Optional reason As String = "") As Task
            If userId.Id = ctx.Member.Id Then Return
            Dim userBan As DiscordBan = (Await ctx.Guild.GetBansAsync).FirstOrDefault(Function(b) b.User.Id = userId.Id)
            If userBan Is Nothing Then Return

            Dim cts As CancellationTokenSource
            If _adminService.SoftbanTokens.TryRemove(ctx.Guild.Id, cts) Then cts.Cancel()

            Await ctx.Guild.UnbanMemberAsync(userId.Id, $"unbanned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: {If(reason?.Any, reason, "None.")}")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("mute")>
        <Description("Prevents the specified user from typing in text channels and speaking in voice channels.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles)>
        <RequireTitle(GuildTitle.Helper)>
        Public Async Function MuteCommand(ctx As CommandContext, user As DiscordMember, <RemainingText> Optional reason As String = "") As Task
            If user Is Nothing OrElse user.Id = ctx.User.Id OrElse GuildData.MutedMembers.Contains(If(user?.Id, 0)) Then Return

            GuildData.MutedMembers.Add(user.Id)
            UpdateGuildData()

            Dim auditLog = $"muted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). Reason: {If(reason?.Any, reason, "None.")}"
            If Not ctx.Guild.Members(user.Id).VoiceState?.IsServerMuted Then Await ctx.Guild.Members(user.Id).SetMuteAsync(True, auditLog)

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("unmute")>
        <Description("Allows a previously muted user to speak and send messages.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function UnmuteCommand(ctx As CommandContext, user As DiscordMember) As Task
            If user Is Nothing OrElse user?.Id = ctx.User.Id OrElse Not GuildData.MutedMembers.Contains(If(user?.Id, 0)) Then Return

            GuildData.MutedMembers.Remove(user.Id)
            UpdateGuildData()

            Dim member = ctx.Guild.Members(user.Id)
            If member?.VoiceState?.IsServerMuted Then Await member.SetMuteAsync(False, $"unmuted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")

            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":hourglass:"))

            Dim channels = (Await ctx.Guild.GetChannelsAsync)
            For Each channel In channels
                For Each chnOverwrite As DiscordOverwrite In channel.PermissionOverwrites
                    If chnOverwrite.Type = OverwriteType.Member AndAlso (Await chnOverwrite.GetMemberAsync).Id = member.Id Then
                        Dim perms As Permissions

                        Select Case chnOverwrite.Type
                            Case ChannelType.Category
                                perms = Permissions.AddReactions Or Permissions.ReadMessageHistory Or Permissions.Speak
                            Case ChannelType.Text
                                perms = Permissions.AddReactions Or Permissions.ReadMessageHistory
                            Case ChannelType.Voice
                                perms = Permissions.Speak
                        End Select

                        Await chnOverwrite.UpdateAsync(deny:=chnOverwrite.Denied And perms)
                    End If
                Next
            Next

            Await ctx.Message.DeleteOwnReactionAsync(DiscordEmoji.FromName(ctx.Client, ":hourglass:"))
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
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
                Dim initalMessageCount As Integer
                Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, 1000)).ToList
                initalMessageCount = messages.Count
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