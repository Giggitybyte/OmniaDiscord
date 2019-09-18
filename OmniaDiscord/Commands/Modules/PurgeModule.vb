Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.Entities
Imports OmniaDiscord.Entities.Attributes

Namespace Commands.Modules
    <Group("purge"), Aliases("prune", "remove")>
    <Description("Bulk deletes messages. The message count defaults to 100.")>
    <RequireBotPermissions(Permissions.ManageMessages Or Permissions.AddReactions)>
    <RequireStaff>
    Public Class PurgeModule
        Inherits OmniaCommandBase

        <GroupCommand>
        Public Async Function PruneCommand(ctx As CommandContext, Optional messageCount As ULong = 100) As Task
            Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, messageCount)).ToList
            messages.RemoveAll(Function(m) m.CreationTimestamp < Date.Now.AddDays(-14))

            Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
        End Function

        <Command("user"), Aliases("u")>
        <Description("Gets messages from the specified user in the last two weeks and bulk deletes them." + vbCrLf + "The message count defaults to 100.")>
        Public Async Function RemoveMessagesFromSpecificUserCommand(ctx As CommandContext, targetUser As String, Optional messageCount As ULong = 100) As Task
            Await ctx.TriggerTypingAsync
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim convert = Await TryCast(New DiscordUserConverter(), IArgumentConverter(Of DiscordUser)).ConvertAsync(targetUser, ctx)
            Dim user = If(convert.HasValue, convert.Value, Nothing)

            If user Is Nothing Then
                embed.Title = "Invalid User"
                embed.Description = "The user you specified was either invalid or does not exist."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim twoWeeksAgo As Date = Date.Now.AddDays(-14)
            Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, messageCount)).ToList
            messages.RemoveAll(Function(m) Not m.Author.Id = user.Id And m.CreationTimestamp > twoWeeksAgo)

            If messages.Count = 0 Then
                embed.Title = "Couldn't Delete Messages"
                embed.Description = $"No messages sent within last two weeks were sent by {user.Mention}"
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.Channel.DeleteMessagesAsync(messages, $"Bulk message deletion by {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id})")
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wastebasket:"))
        End Function

        <Command("bots"), Aliases("b")>
        <Description("Retrieves all messages from bots in the last two weeks and bulk deletes them." + vbCrLf + "The message count defaults to 100.")>
        Public Async Function RemoveMessagesFromBotsCommand(ctx As CommandContext, Optional messageCount As ULong = 100) As Task
            Dim twoWeeksAgo As Date = Date.Now.AddDays(-14)
            Dim messages As List(Of DiscordMessage) = (Await ctx.Channel.GetMessagesBeforeAsync(ctx.Message.Id, messageCount)).ToList
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
End Namespace