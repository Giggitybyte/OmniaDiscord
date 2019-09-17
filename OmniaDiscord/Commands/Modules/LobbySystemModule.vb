Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities

Namespace Commands.Modules
    <Group("lobby")>
    <Description("Displays all lobby channels. Child commands allow for the addition and removal of lobby channels.")>
    Public Class LobbySystemModule
        Inherits OmniaCommandBase

        <GroupCommand>
        Public Async Function DisplayLobbiesCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder With {
                .Color = DiscordColor.CornflowerBlue,
                .Title = "Lobby Channels"
            }

            For Each channelId In DbGuild.Data.LobbyChannels
                Dim channel As DiscordChannel
                If Not ctx.Guild.Channels.TryGetValue(channelId, channel) Then Continue For
                embed.Description &= $"{channel.Mention}{Environment.NewLine}"
            Next

            If String.IsNullOrWhiteSpace(embed.Description) Then embed.Description = "No lobbies have been set yet."
            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("add")>
        <Description("Adds a channel to the lobby channel list.")>
        Public Async Function AddLobbyCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            If Not channel.Type = ChannelType.Voice Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"A lobby channel must be a voice channel, not {channel.Type.ToString.ToLower} channel."
                })
                Return
            End If

            If DbGuild.Data.LobbyChannels.Contains(channel.Id) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"{channel.Mention} is already a lobby channel."
                })
                Return
            End If

            DbGuild.Data.LobbyChannels.Add(channel.Id)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("remove")>
        <Description("Removes a channel from the lobby channel list.")>
        Public Async Function RemoveLobbyCommand(ctx As CommandContext, channel As DiscordChannel) As Task
            If Not DbGuild.Data.LobbyChannels.Contains(channel.Id) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"{channel.Mention} is not a lobby channel."
                })
                Return
            End If

            DbGuild.Data.LobbyChannels.Remove(channel.Id)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function
    End Class
End Namespace