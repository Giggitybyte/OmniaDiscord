Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Humanizer
Imports Humanizer.Localisation
Imports OmniaDiscord.Entities.Media
Imports OmniaDiscord.Extensions
Imports OmniaDiscord.Services
Imports OmniaDiscord.Utilities

Namespace Commands.Modules.Media
    Partial Class MediaModule

        <Group("playlist"), Aliases("pl")>
        <Description("Allows for the management of user created playlists.")>
        Public Class PlaylistSubmodule
            Inherits OmniaCommandBase

            Private _lavalink As LavalinkService

            Sub New(lavalink As LavalinkService)
                _lavalink = lavalink
            End Sub

            <Command("create")>
            <Description("Create a new playlist with the specified name.")>
            Public Async Function CreatePlaylistCommand(ctx As CommandContext, <RemainingText> playlistName As String) As Task
                If GuildData.UserPlaylists.Any(Function(p) p.Name.ToLower = playlistName.ToLower) Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = $"Cannot create playlist; a playlist with that name already exists."
                    })
                    Return
                End If

                Dim playlist As New OmniaUserPlaylist With {
                    .CreatorUserId = ctx.Member.Id,
                    .Name = playlistName,
                    .ThumbnailUrl = $"{Bot.Config.ResourceUrl}/assets/omnia/MediaDefault.png"
                }

                GuildData.UserPlaylists.Add(playlist)
                UpdateGuildData()

                Dim emoji = DiscordEmoji.FromName(ctx.Client, ":ballot_box_with_check:")
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.SpringGreen,
                    .Description = $"{emoji} Playlist created"
                })
            End Function

            <Command("delete")>
            <Description("Delete a playlist from the database.")>
            Public Async Function DeletePlaylistCommand(ctx As CommandContext, <RemainingText> playlistName As String) As Task
                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = "Cannot delete playlist; no playlists exist with that name."
                    })
                    Return
                End If

                If Not ctx.Member.Id = ctx.Guild.Owner.Id And Not ctx.Member.Id = playlist.CreatorUserId Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = $"You cannot delete this playlist.{Environment.NewLine}A playlist can only be deleted by either it's creator or the owner of this server."
                    })
                    Return
                End If

                GuildData.UserPlaylists.Remove(playlist)
                UpdateGuildData()

                Dim emoji = DiscordEmoji.FromName(ctx.Client, ":ballot_box_with_check:")
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.SpringGreen,
                    .Description = $"{emoji} Playlist deleted"
                })
            End Function

            <Command("add")>
            <Description("Add tracks to a playlist.")>
            Public Async Function AddTracksCommand(ctx As CommandContext, playlistName As String, ParamArray trackUrls As String()) As Task
                If Not trackUrls.Any Then Return

                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    embed.Description = "Cannot modify playlist; no playlists exist with that name."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If Not ctx.Member.Id = playlist.CreatorUserId Then
                    embed.Description = $"You cannot add tracks to this playlist.{Environment.NewLine}A playlist can only be altered by it's creator."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                GuildData.UserPlaylists.Remove(playlist)

                With embed
                    .Color = DiscordColor.Orange
                    .Title = "Please Wait..."
                    .Description = $"Retrieving information for the provided URL{If(trackUrls.Count > 1, "s", String.Empty)}..."
                End With

                Dim waitMessage = Await ctx.RespondAsync(embed:=embed.Build)
                Dim retrievalResult = Await MediaRetrievalUtilities.GetMultipleMediaAsync(trackUrls)
                Await waitMessage.DeleteAsync

                embed = New DiscordEmbedBuilder

                If Not retrievalResult.ValidUrls.Any Then
                    With embed
                        .Color = DiscordColor.Red
                        .Description = $"The URL{If(trackUrls.Count > 1, "s", String.Empty)} provided were invalid."
                    End With

                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If retrievalResult.InvalidUrls.Any Then
                    With embed
                        .Description = $"{retrievalResult.ValidUrls.Count} out of {trackUrls.Count} URLs were valid.{Environment.NewLine}"
                        .Description &= $"All valid URLs were added to the playlist.{Environment.NewLine}{Environment.NewLine}"
                        .Description &= $"Invalid URLs: {Formatter.BlockCode(String.Join(", ", retrievalResult.InvalidUrls))}"
                        .Color = DiscordColor.Orange
                    End With
                Else
                    embed.Color = DiscordColor.SpringGreen
                    embed.Description = $"All tracks were successfully added to `{playlist.Name}`."
                End If

                playlist.Tracks.AddRange(retrievalResult.ValidUrls)
                GuildData.UserPlaylists.Add(playlist)
                UpdateGuildData()

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("tracks"), Aliases("list")>
            <Description("Displays all tracks contained within a playlist.")>
            Public Async Function ListTracksCommand(ctx As CommandContext, <RemainingText> playlistName As String) As Task
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    Dim embed As New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = "No playlists exist with that name."
                    }
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If Not playlist.Tracks.Any Then
                    Dim embed As New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = "This playlist is empty."
                    }
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                Await ctx.TriggerTypingAsync
                Dim strBuilder As New StringBuilder
                Dim pages As New List(Of String)

                For trackNumber As Integer = 1 To playlist.Tracks.Count
                    Dim title As String = playlist.Tracks(trackNumber - 1).Title
                    If title.Count > 95 Then title = $"{title.Substring(0, 92)}..."
                    strBuilder.Append($"{trackNumber}. {title}{Environment.NewLine}")
                Next

                pages = strBuilder.ToString.Trim.SplitAtOccurence(Environment.NewLine, 20)

                For page As Integer = 1 To pages.Count
                    pages(page - 1) = $"Playlist Tracks{Environment.NewLine}============={Environment.NewLine}{pages(page - 1).Trim}"
                Next

                If pages.Count > 1 Then
                    For page As Integer = 1 To pages.Count
                        pages(page - 1) &= $"{Environment.NewLine}{Environment.NewLine}Page {page} of {pages.Count}"
                    Next

                    Await MediaPlaybackUtilities.DoStringPaginationAsync(ctx, pages, 30000)

                Else
                    Await ctx.RespondAsync(Formatter.BlockCode(pages.First, "markdown"))
                End If
            End Function

            <Command("remove")>
            <Description("Remove tracks from a playlist.")>
            Public Async Function RemoveTracksCommand(ctx As CommandContext, playlistName As String, ParamArray trackNumbers As Integer()) As Task
                If Not trackNumbers.Any Then Return

                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    embed.Description = "Cannot modify playlist; no playlists exist with that name."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If Not ctx.Member.Id = playlist.CreatorUserId Then
                    embed.Description = $"You cannot add tracks to this playlist.{Environment.NewLine}A playlist can only be altered by it's creator."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                trackNumbers = trackNumbers.Where(Function(n) n > 0 AndAlso n <= playlist.Tracks.Count)
                If Not trackNumbers.Any Then
                    embed.Description = $"The track numbers your provided"
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                GuildData.UserPlaylists.Remove(playlist)

                For Each track In trackNumbers
                    playlist.Tracks.RemoveAt(track - 1)
                Next

                With embed
                    .Color = DiscordColor.SpringGreen
                    .Description = $"Removed track(s) {String.Join(", ", trackNumbers.Select(Function(n) $"`{n}`"))}"
                End With

                GuildData.UserPlaylists.Add(playlist)
                UpdateGuildData()

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("enqueue"), Aliases("queue", "q")>
            <Description("Add all tracks from a playlist to the playback queue.")>
            Public Async Function EnqueuePlaylistCommand(ctx As CommandContext, <RemainingText> playlistName As String) As Task
                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    embed.Description = "Cannot queue playlist; no playlists exist with that name."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                Await ctx.TriggerTypingAsync
                Dim plCreator = Await ctx.Client.GetUserAsync(playlist.CreatorUserId)
                Dim totalDuration As TimeSpan
                playlist.Tracks.ForEach(Sub(t) totalDuration = totalDuration.Add(t.Duration))

                With embed
                    .Color = DiscordColor.SpringGreen
                    .ThumbnailUrl = If(playlist.ThumbnailUrl, $"{Bot.Config.ResourceUrl}/assets/omnia/MediaDefault.png")
                    .Title = "Playlist Queued"

                    .Description = $"**{playlist.Name}**{Environment.NewLine}"
                    .Description &= $"Curated by {plCreator.Mention}{Environment.NewLine}"
                    .Description &= $"{Environment.NewLine}"
                    .Description &= $"Total tracks: {playlist.Tracks.Count}{Environment.NewLine}"
                    .Description &= $"Total duration: {totalDuration.Duration.Humanize(2, maxUnit:=TimeUnit.Hour)}"
                End With

                Await ctx.RespondAsync(embed:=embed.Build)

                For Each track In playlist.Tracks
                    Await MediaPlaybackUtilities.AddMediaToQueueAsync(ctx, _lavalink, track)
                Next
            End Function

            <Command("thumbnail")>
            <Description("Add or change the thumbnail for a playlist.")>
            Public Async Function PlaylistThumbnailCommand(ctx As CommandContext, playlistName As String, thumbnailUrl As String) As Task
                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    embed.Description = "Cannot modify playlist; no playlists exist with that name."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If Not ctx.Member.Id = playlist.CreatorUserId Then
                    embed.Description = $"You cannot change the thumbnail for this playlist.{Environment.NewLine}A playlist can only be altered by it's creator."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                GuildData.UserPlaylists.Remove(playlist)
                playlist.ThumbnailUrl = thumbnailUrl
                GuildData.UserPlaylists.Add(playlist)
                UpdateGuildData()

                With embed
                    Dim emoji = DiscordEmoji.FromName(ctx.Client, ":ballot_box_with_check:")
                    .Color = DiscordColor.SpringGreen
                    .Description = $"{emoji} Thumbnail set"
                End With

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function
        End Class
    End Class
End Namespace