Imports System.Collections.Concurrent
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

        Private _muteService As MuteService
        Private _softBanTokens As ConcurrentDictionary(Of ULong, CancellationTokenSource)
        Private _userConverter As DiscordUserConverter
        Private _memberConverter As DiscordMemberConverter

        Sub New(muteService As MuteService)
            _muteService = muteService
            _userConverter = New DiscordUserConverter
            _memberConverter = New DiscordMemberConverter
        End Sub

        <Command("kick")>
        <Description("Kicks a user from the server.")>
        <RequireBotPermissions(Permissions.KickMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function KickCommand(ctx As CommandContext, targetMember As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim member = (Await _memberConverter.ConvertAsync(targetMember, ctx)).Value

            If member Is Nothing Then embed.Description = "The user you specified either is not in this server, or doesn't exist."
            If member?.Id = ctx.Member.Id Then embed.Description = "You cannot kick yourself!"
            If embed.Description?.Any Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await member.RemoveAsync($"kicked by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("ban")>
        <Description("Bans a user from this server. Users who are not currently in this server can still be banned if their user ID is provided.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function BanCommand(ctx As CommandContext, targetUser As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim user = (Await _userConverter.ConvertAsync(targetUser, ctx)).Value

            If user Is Nothing Then embed.Description = "The user you specified was either invalid or does not exist."
            If user?.Id = ctx.User.Id Then embed.Description = "You cannot ban yourself!"
            If embed.Description?.Any Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.Guild.BanMemberAsync($"banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

#Disable Warning BC42358
        <Command("softban"), Aliases("sban")>
        <Description("Bans a user then unbans them after five minutes.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function SoftBanCommand(ctx As CommandContext, targetUser As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim member = (Await _memberConverter.ConvertAsync(targetUser, ctx)).Value

            If member Is Nothing Then embed.Description = "The user you specified either is not in this server, or doesn't exist."
            If member?.Id = ctx.User.Id Then embed.Description = "You cannot soft ban yourself!"
            If embed.Description?.Any Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.Guild.BanMemberAsync($"soft banned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))

            Dim cts As New CancellationTokenSource
            _softBanTokens.TryAdd(ctx.Guild.Id, cts)
            Task.Delay(300000, cts.Token).ContinueWith(Sub() ctx.Guild.UnbanMemberAsync(member.Id, "Soft ban ended"), TaskContinuationOptions.NotOnCanceled)
        End Function
#Enable Warning BC42358

        <Command("unban")>
        <Description("Removes a ban from a previously banned user.")>
        <RequireBotPermissions(Permissions.BanMembers)>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function UnbanCommand(ctx As CommandContext, userId As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim user = (Await _userConverter.ConvertAsync(userId, ctx)).Value

            If user Is Nothing Then embed.Description = "The user you specified was either invalid or does not exist."
            If user?.id = ctx.User.Id Then embed.Description = "You cannot unban yourself!"

            Dim userBan As DiscordBan = (Await ctx.Guild.GetBansAsync).FirstOrDefault(Function(b) b.User.Id = user.Id)
            If userBan Is Nothing Then embed.Description = "The user you specified is not currently banned."

            If embed.Description?.Any Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim cts As CancellationTokenSource
            If _softBanTokens.TryRemove(ctx.Guild.Id, cts) Then cts.Cancel()

            Await ctx.Guild.UnbanMemberAsync($"unbanned by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("mute")>
        <Description("Prevents the specified user from typing in text channels and speaking in voice channels.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles)>
        <RequireTitle(GuildTitle.Helper)>
        Public Async Function MuteCommand(ctx As CommandContext, targetUser As String, <RemainingText> Optional reason As String = "") As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim member = (Await _memberConverter.ConvertAsync(targetUser, ctx)).Value

            If member Is Nothing Then embed.Description = "The user you specified either is not in this server, or doesn't exist."
            If member?.Id = ctx.User.Id Then embed.Description = "You cannot mute yourself!"
            If GuildData.MutedMembers.Contains(member.Id) Then embed.Description = "The user you specified is already muted."
            If embed.Description?.Any Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            GuildData.MutedMembers.Add(member.Id)
            UpdateGuildData()

            Await member.SetMuteAsync($"muted by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}). {If(reason, "")}")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("unmute")>
        <Description("Allows a previously muted user to speak and send messages.")>
        <RequireBotPermissions(Permissions.MuteMembers Or Permissions.ManageRoles)>
        <RequireTitle(GuildTitle.Moderator)>
        Public Async Function UnmuteCommand(ctx As CommandContext, targetUser As String) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim member = (Await _memberConverter.ConvertAsync(targetUser, ctx)).Value

            If member Is Nothing Then embed.Description = "The user you specified either is not in this server, or doesn't exist."
            If member?.Id = ctx.User.Id Then embed.Description = "You cannot mute yourself!"
            If Not GuildData.MutedMembers.Contains(member?.Id) Then embed.Description = "The user you specified is not currently muted."
            If embed.Description?.Any Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            GuildData.MutedMembers.Remove(member.Id)
            UpdateGuildData()
            Await member.SetMuteAsync(False, $"unmuted by {ctx.Member.Username}#{ctx.Member.Discriminator}")

            Await ctx.TriggerTypingAsync

            With embed
                .Color = DiscordColor.Orange
                .Description = "Removing channel overwrites..."
            End With

            Dim message As DiscordMessage = Await ctx.RespondAsync(embed:=embed.Build)

            For Each channel As DiscordChannel In (Await ctx.Guild.GetChannelsAsync)
                For Each chnOverwrite As DiscordOverwrite In channel.PermissionOverwrites
                    If chnOverwrite.Type = OverwriteType.Member AndAlso (Await chnOverwrite.GetMemberAsync).Id = member.Id Then Await chnOverwrite.DeleteAsync
                Next
            Next

            Await message.DeleteAsync
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
                Dim user = (Await New DiscordUserConverter().ConvertAsync(targetUser, ctx)).Value

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