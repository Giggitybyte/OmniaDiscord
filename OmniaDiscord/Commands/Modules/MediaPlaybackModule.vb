Imports System.Collections.Concurrent
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports DSharpPlus.Interactivity
Imports OmniaDiscord.Extensions
Imports OmniaDiscord.Services
Imports YoutubeExplode
Imports YoutubeExplode.Models
Imports OmniaDiscord.Entities.Media
Imports OmniaDiscord.Utilities
Imports Humanizer
Imports System.Net

Namespace Commands.Modules

    <Group("music"), RequireGuild>
    <Description("Allows for the playback of music and other audio.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.AddReactions Or Permissions.UseExternalEmojis Or Permissions.UseVoice Or Permissions.Speak)>
    Public Class MediaPlaybackModule
        Inherits OmniaCommandBase

        Private _mediaRetrieval As MediaRetrievalService
        Private _lavalink As LavalinkService

        Sub New(mediaRetrieval As MediaRetrievalService, lavalink As LavalinkService)
            _mediaRetrieval = mediaRetrieval
            _lavalink = lavalink
        End Sub

#Region "Command Methods"
        <Command("play"), Aliases("p")>
        <Description("Queue music and other audio for playback from a url.")>
        <Cooldown(3, 15, CooldownBucketType.User)>
        Public Async Function PlayCommand(ctx As CommandContext, url As String) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim mbrChannel = ctx.Member.VoiceState?.Channel
            Dim currentChn = ctx.Guild.CurrentMember?.VoiceState?.Channel

            ' Validation.
            If mbrChannel Is Nothing Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Cannot Queue Media"
                    .Description = "You must join a voice channel before queuing a track."
                End With

            ElseIf mbrChannel.Id <> currentChn?.Id Then

                With embed
                    .Color = DiscordColor.Red
                    .Title = "Cannot Queue Media"
                    .Description = $"I'm already in a voice channel. Please join that channel before queuing a track."
                End With

            Else ' Attempt to queue requested media.
                With embed
                    .Color = DiscordColor.Orange
                    .Title = "Please Wait"
                    .Description = "Retrieving media information..."
                End With

                Dim waitMessage = Await ctx.RespondAsync(embed:=embed.Build)
                Await ctx.TriggerTypingAsync
                Dim media = Await _mediaRetrieval.GetMediaAsync(url)

                embed = Await MediaUtilities.QueueMediaAsync(ctx, _lavalink, media)
                Await waitMessage.DeleteAsync()
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("upload")>
        <Description("Queue music, videos, and other audio for playback from a file attachment. Supports mp3, mp4, m4a, flac, ogg, ogv, oga, wav, mkv, mka, and webm.")>
        <Cooldown(3, 15, CooldownBucketType.User)>
        Public Async Function UploadCommand(ctx As CommandContext) As Task
            If Not ctx.Message.Attachments.Any Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"Nothing to queue; a file was not uploaded along with your command."
                })
                Return
            End If

            Dim attachment = ctx.Message.Attachments.First
            Dim fileBytes As Byte()

            Using wClient As New WebClient
                fileBytes = Await wClient.DownloadDataTaskAsync(attachment.Url)
            End Using

            If Not MediaUtilities.IsPlayableFileType(fileBytes.Take(24).ToArray) Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"The file you've uploaded is not a supported file format."
                })
                Return
            End If

            Await ctx.RespondAsync(embed:=Await MediaUtilities.QueueMediaAsync(ctx, _lavalink, New OmniaMediaInfo With {
                .Author = ctx.Member.Mention,
                .DirectUrl = attachment.Url,
                .Origin = "File Upload",
                .Requester = ctx.Member.Id,
                .ThumbnailUrl = $"{OmniaConfig.ResourceUrl}/assets/omnia/MediaDefault.png",
                .Title = attachment.FileName,
                .Type = OmniaMediaType.Track,
                .Url = attachment.Url
            }))
        End Function

        <Command("search")>
        <Description("Searches YouTube for your query and returns the top five results for you to select.")>
        <Cooldown(1, 6, CooldownBucketType.User)>
        Public Async Function SearchCommand(ctx As CommandContext, <RemainingText> query As String) As Task
            Await ctx.TriggerTypingAsync

            Dim embed As New DiscordEmbedBuilder
            Dim ytResults = Await New YoutubeClient().SearchVideosAsync(query, 1)

            If ytResults.Count = 0 Then
                With embed
                    .Color = DiscordColor.Red
                    .Description = $"No videos were found with that query."
                End With

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            If ytResults.Count > 5 Then ytResults = ytResults.Take(5).ToList

            Dim builder As New StringBuilder
            Dim emojis As New List(Of DiscordEmoji) From {
                DiscordEmoji.FromName(ctx.Client, ":one:"),
                DiscordEmoji.FromName(ctx.Client, ":two:"),
                DiscordEmoji.FromName(ctx.Client, ":three:"),
                DiscordEmoji.FromName(ctx.Client, ":four:"),
                DiscordEmoji.FromName(ctx.Client, ":five:")
            }

            For index As Integer = 0 To ytResults.Count - 1
                Dim ytVideo = ytResults(index)
                Dim title = ytVideo.Title
                Dim details = $"{ytVideo.Title}{Environment.NewLine}{ytVideo.Duration.Humanize}{Environment.NewLine}{Environment.NewLine}Uploaded by {ytVideo.Author}"

                If title.Count > 54 Then title = $"{title.Substring(0, 51)}..."
                builder.Append($"{emojis(index)} **{Formatter.MaskedUrl(title, New Uri(ytVideo.GetUrl), details)}**{Environment.NewLine}")
            Next

            With embed
                .Color = DiscordColor.Orange
                .ThumbnailUrl = $"{OmniaConfig.ResourceUrl}/assets/omnia/YouTube.png"
                .Title = $"Results for `{query}`"
                .Description = builder.ToString.Trim
                .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                    .Text = "React below to select a track!",
                    .IconUrl = $"{OmniaConfig.ResourceUrl}/assets/omnia/ArrowDown.png"
                }
            End With

            Dim message = Await ctx.RespondAsync(embed:=embed.Build)

            For index As Integer = 0 To ytResults.Count - 1
                Await message.CreateReactionAsync(emojis(index))
            Next

            Dim interactivity = ctx.Client.GetInteractivity()
            Dim userReaction = Await interactivity.WaitForReactionAsync(Function(arg) emojis.Contains(arg.Emoji), message, ctx.User, TimeSpan.FromSeconds(15))
            embed.Footer = Nothing

            If userReaction.Result Is Nothing Then
                embed.Color = DiscordColor.Red
                Await message?.ModifyAsync(embed:=embed.Build)

            Else
                Await ctx.TriggerTypingAsync

                Dim selectedVideo = ytResults(emojis.IndexOf(userReaction.Result.Emoji))
                Dim media = Await _mediaRetrieval.GetMediaAsync(selectedVideo.GetUrl)

                embed.Color = DiscordColor.SpringGreen
                Await message?.ModifyAsync(embed:=embed.Build)
                embed = Await MediaUtilities.QueueMediaAsync(ctx, _lavalink, media)

                Await ctx.RespondAsync(embed:=embed.Build)
            End If

            For index As Integer = 0 To ytResults.Count - 1
                Await message?.DeleteOwnReactionAsync(emojis(index))
            Next


        End Function

        <Command("stop"), Aliases("die", "leave")>
        <Description("Removes all tracks from the queue and leaves the voice channel.")>
        Public Async Function StopCommand(ctx As CommandContext) As Task
            Dim guildConnection = _lavalink.Node.GetConnection(ctx.Guild)

            If ctx.Guild.CurrentMember?.VoiceState?.Channel IsNot Nothing Or guildConnection?.IsConnected Then
                guildConnection.Stop()
                guildConnection.SetVolume(100)
                guildConnection.ResetEqualizer()
                guildConnection.Disconnect()

                _lavalink.GuildInfo(ctx.Guild.Id).ResetTrackData()

                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":wave:"))
            End If

        End Function

        <Command("pause")>
        <Description("Pauses audio playback.")>
        Public Async Function PauseCommand(ctx As CommandContext) As Task
            Dim playbackInfo = _lavalink.GuildInfo(ctx.Guild.Id)
            Dim embed As New DiscordEmbedBuilder

            If playbackInfo?.CurrentTrack Is Nothing Then
                embed.Color = DiscordColor.Red
                embed.Description = "Nothing is currently playing; there is nothing to pause."

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            embed.WithFooter($"To resume playback, type {ctx.Prefix}music resume")

            If playbackInfo?.IsPlaying Then
                embed.Color = DiscordColor.SpringGreen
                embed.Description = $"Audio playback has been paused."

                _lavalink.Node.GetConnection(ctx.Guild).Pause()
                _lavalink.GuildInfo(ctx.Guild.Id).IsPlaying = False
            Else
                embed.Color = DiscordColor.Orange
                embed.Description = $"Audio playback is already paused."
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("resume")>
        <Description("Resumes audio playback.")>
        Public Async Function ResumeCommand(ctx As CommandContext) As Task
            Dim playbackInfo = _lavalink.GuildInfo(ctx.Guild.Id)
            Dim embed As New DiscordEmbedBuilder

            If playbackInfo?.CurrentTrack Is Nothing Then
                embed.Color = DiscordColor.Red
                embed.Description = "Nothing is currently playing; there is nothing to resume."

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            If playbackInfo?.IsPlaying = False Then
                embed.Color = DiscordColor.SpringGreen
                embed.Description = "Playback resumed."

                _lavalink.Node.GetConnection(ctx.Guild).Resume()
                _lavalink.GuildInfo(ctx.Guild.Id).IsPlaying = True
            Else
                embed.Color = DiscordColor.Red
                embed.Description = "Playback isn't currently paused."
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("skip")>
        <Description("Skips the current track and begins playback for the next track in the queue. If a track number is specified, all tracks before said track will be skipped, and playback will begin for the specified track.")>
        Public Async Function SkipCommand(ctx As CommandContext, Optional trackNumber As Integer = 1) As Task
            Dim playbackInfo As GuildPlaybackInfo = _lavalink.GuildInfo(ctx.Guild.Id)
            Dim embed As New DiscordEmbedBuilder

            If playbackInfo?.CurrentTrack Is Nothing Then
                embed.Color = DiscordColor.Red
                embed.Description = "Nothing is currently playing."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim queueCount = playbackInfo.MediaQueue.Count

            If queueCount = 0 Then
                embed.Color = DiscordColor.Red
                embed.Description = "Nothing to skip to; the queue is empty."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.TriggerTypingAsync
            Dim skipCount As Integer = 1

            Select Case trackNumber
                Case <= 0
                    embed.Color = DiscordColor.Red
                    embed.Description = "Invalid track number."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return

                Case 1
                    embed.Color = DiscordColor.SpringGreen
                    embed.Description = "Skipped current track."

                Case < queueCount
                    embed.Color = DiscordColor.SpringGreen
                    embed.Description = $"Skipped to track {trackNumber}."
                    skipCount = trackNumber - 1

                Case >= queueCount
                    embed.Color = DiscordColor.SpringGreen
                    embed.Description = "Skipped to last track."
                    skipCount = queueCount - 1
            End Select

            If skipCount > 1 Then
                Dim newQueue As New ConcurrentQueue(Of OmniaMediaInfo)(_lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Skip(skipCount))
                _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue = newQueue
            End If

            Dim isSuccessful As Boolean = Await _lavalink.PlayNextTrackAsync(ctx.Guild)
            If Not isSuccessful Then
                embed.Description = $"The track you skipped to was unplayable.{Environment.NewLine}"

                Do Until isSuccessful Or _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Count = 0
                    isSuccessful = Await _lavalink.PlayNextTrackAsync(ctx.Guild)
                Loop

                If _lavalink.GuildInfo(ctx.Guild.Id).CurrentTrack Is Nothing And _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Count = 0 Then
                    With embed
                        .Color = DiscordColor.Red
                        .Description = "There are no more playable tracks; the queue is now empty."
                    End With

                ElseIf isSuccessful Then
                    With embed
                        .Color = DiscordColor.Yellow
                        .Description &= "Skipped to the next playable track instead."
                    End With
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("movetop")>
        <Description("Moves the specified track to the top of the queue.")>
        Public Async Function MoveTopCommand(ctx As CommandContext, trackNumber As Integer) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}

            If trackNumber <= 0 Then
                embed.Description = "Invalid track number."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            If trackNumber = 1 Then
                embed.Description = $"You can't move the next track to the top of the queue.{Environment.NewLine}It's already at the top, dingus."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim queue = _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.ToList
            Dim track As OmniaMediaInfo

            If trackNumber > queue.Count Then
                embed.Description = "Moved the last track in the queue to the top."
                track = queue.Last
            Else
                embed.Description = $"Moved track {trackNumber} to the top of the queue."
                track = queue.Item(trackNumber - 1)
            End If

            queue.Remove(track)
            queue.Insert(0, track)
            _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue = New ConcurrentQueue(Of OmniaMediaInfo)(queue)

            embed.Color = DiscordColor.SpringGreen
            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("queue"), Aliases("q")>
        <Description("Displays all the tracks currently in the playback queue.")>
        <RequireBotPermissions(Permissions.ManageMessages)>
        <Cooldown(1, 6, CooldownBucketType.Guild)>
        Public Async Function DisplayQueueCommand(ctx As CommandContext) As Task
            Dim playbackQueue As ConcurrentQueue(Of OmniaMediaInfo) = _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue

            If playbackQueue.Count = 0 Then
                Dim embed As New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = "The playback queue is empty."
                }

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.TriggerTypingAsync
            Dim strBuilder As New StringBuilder
            Dim pages As New List(Of String)

            For trackNumber As Integer = 1 To playbackQueue.Count
                Dim title As String = playbackQueue(trackNumber - 1).Title
                If title.Count > 95 Then title = $"{title.Substring(0, 92)}..."
                strBuilder.Append($"{trackNumber}. {title}{Environment.NewLine}")
            Next

            pages = strBuilder.ToString.SplitAtOccurence(Environment.NewLine, 20)

            For page As Integer = 1 To pages.Count
                pages(page - 1) = $"Queued Tracks{Environment.NewLine}============={Environment.NewLine}{pages(page - 1).Trim}"
            Next

            If pages.Count > 1 Then
                For page As Integer = 1 To pages.Count
                    pages(page - 1) &= $"{Environment.NewLine}{Environment.NewLine}Page {page} of {pages.Count}"
                Next

                Await MediaUtilities.DoQueuePaginationAsync(ctx, pages, 30000)

            Else
                Await ctx.RespondAsync(Formatter.BlockCode(pages.First, "markdown"))
            End If

        End Function

        <Command("nowplaying"), Aliases("np", "currenttrack", "current")>
        <Description("Displays all info for the song that is currently playing, including album art and original link.")>
        Public Async Function DisplayCurrentTrack(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim currentTrack As OmniaMediaInfo = _lavalink.GuildInfo(ctx.Guild.Id).CurrentTrack

            If currentTrack IsNot Nothing Then
                Dim duration As String = If(currentTrack.Duration.TotalSeconds > 0, currentTrack.Duration.Humanize(2), "Unknown")
                Dim requester As DiscordMember = Await ctx.Guild.GetMemberAsync(currentTrack.Requester)

                With embed
                    .Color = DiscordColor.CornflowerBlue
                    .Title = "Currently Playing"

                    .AddField("Title", currentTrack.Title)
                    .AddField("Author", currentTrack.Author, True)
                    .AddField("Duration", duration, True)
                    .AddField("Source", $"[{currentTrack.Origin}]({currentTrack.Url})", True)
                    .AddField("Played By", requester.Mention, True)

                    .ThumbnailUrl = currentTrack.ThumbnailUrl
                End With
            Else
                With embed
                    .Color = DiscordColor.Red
                    .Description = "Nothing is currently playing."
                End With
            End If

            Await ctx.RespondAsync(embed:=embed)
        End Function

        <Command("clear")>
        <Description("Removes all tracks from the queue without stopping playback.")>
        Public Async Function ClearCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim queueCount As Integer = _lavalink.GuildInfo(ctx.Guild.Id)?.MediaQueue.Count

            If queueCount > 0 Then
                With embed
                    .Color = DiscordColor.SpringGreen
                    .Description = $"Cleared {queueCount} tracks."
                End With

                _lavalink.GuildInfo(ctx.Guild.Id)?.MediaQueue.Clear()
            Else
                With embed
                    .Color = DiscordColor.Red
                    .Description = "There was nothing to clear; the queue is empty."
                End With
            End If

            Await ctx.RespondAsync(embed:=embed)
        End Function

        <Command("remove")>
        <Description("Removes a specific track from the queue.")>
        Private Async Function RemoveCommand(ctx As CommandContext, trackNumber As Integer) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim queue As List(Of OmniaMediaInfo) = _lavalink.GuildInfo(ctx.Guild.Id)?.MediaQueue.ToList

            If queue.Count = 0 Then
                embed.Color = DiscordColor.Red
                embed.Description = "There was nothing to remove; the queue is empty."

                Await ctx.RespondAsync(embed:=embed)
                Return
            End If

            Dim track As OmniaMediaInfo

            If trackNumber > queue.Count Then
                track = queue.Last
                embed.Description = "Removed last track in the queue."
            Else
                track = queue(trackNumber - 1)
                embed.Description = $"Removed track {trackNumber} from the queue."
            End If

            queue.Remove(track)
            _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue = New ConcurrentQueue(Of OmniaMediaInfo)(queue)

            embed.Color = DiscordColor.SpringGreen
            embed.Description &= $"{Environment.NewLine}{queue.Count - 1} tracks remain in the queue."

            Await ctx.RespondAsync(embed:=embed)
        End Function
#End Region

        <Group("playlist"), Aliases("pl")>
        <Description("Allows for the management of user created playlists.")>
        Public Class PlaylistSubmodule
            Inherits OmniaCommandBase

            Private _mediaRetrieval As MediaRetrievalService
            Private _lavalink As LavalinkService

            Sub New(mediaRetrieval As MediaRetrievalService, lavalink As LavalinkService)
                _mediaRetrieval = mediaRetrieval
                _lavalink = lavalink
            End Sub

            <Command("create"), Priority(1)>
            <Description("Create a new playlist.")>
            Public Async Function CreatePlaylistCommand(ctx As CommandContext, playlistName As String, thumbnailUrl As String, <RemainingText> urls As String()) As Task
                If GuildData.UserPlaylists.Any(Function(p) p.Name.ToLower = playlistName.ToLower) Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = $"A playlist with that name already exists."
                    })
                    Return
                End If

                Dim playlist As New DiscordUserPlaylist With {
                    .CreatorUserId = ctx.Member.Id,
                    .Name = playlistName,
                    .ThumbnailUrl = thumbnailUrl
                }

                GuildData.UserPlaylists.Add(playlist)
                UpdateGuildData()

                Dim emoji = DiscordEmoji.FromName(ctx.Client, ":ballot_box_with_check:")
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.SpringGreen,
                    .Description = $"{emoji} Playlist created."
                })

                Await AddTracksCommand(ctx, playlistName, urls)
            End Function

            <Command("create"), Priority(0)>
            Public Async Function CreatePlaylistCommand(ctx As CommandContext, playlistName As String, <RemainingText> urls As String()) As Task
                Await CreatePlaylistCommand(ctx, playlistName, String.Empty, urls)
            End Function

            <Command("delete")>
            <Description("Delete a playlist.")>
            Public Async Function DeletePlaylistCommand(ctx As CommandContext, playlistName As String) As Task
                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
                Dim playlist = GuildData.UserPlaylists.FirstOrDefault(Function(p) p.Name.ToLower = playlistName.ToLower)

                If playlist Is Nothing Then
                    embed.Description = "Cannot delete playlist; no playlists exist with that name."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If Not ctx.Member.Id = ctx.Guild.Owner.Id And Not ctx.Member.Id = playlist.CreatorUserId Then
                    embed.Description = $"You cannot delete this playlist.{Environment.NewLine}A playlist can only be deleted by either it's creator or the owner of this server."
                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                GuildData.UserPlaylists.Remove(playlist)
                UpdateGuildData()

                Dim emoji = DiscordEmoji.FromName(ctx.Client, ":ballot_box_with_check:")
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.SpringGreen,
                    .Description = $"{emoji} Playlist deleted."
                })
            End Function

            <Command("add")>
            <Description("Add tracks to a playlist.")>
            Public Async Function AddTracksCommand(ctx As CommandContext, playlistName As String, <RemainingText> trackUrls As String()) As Task
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
                    .Description = "Retrieving information for the provided URL(s)."
                End With

                Dim waitMessage = Await ctx.RespondAsync(embed:=embed.Build)
                Dim goodUrls As New List(Of String)
                Dim badUrls As New List(Of String)

                For Each url In trackUrls
                    Dim media = Await _mediaRetrieval.GetMediaAsync(url)

                    If media Is Nothing Then
                        badUrls.Add(url)
                        Continue For
                    End If

                    Select Case media.Type
                        Case OmniaMediaType.Track
                            goodUrls.Add(media.Url)

                        Case Else
                            Dim tracks = media.Tracks.Select(Function(t) t.Url)
                            goodUrls.AddRange(tracks)

                    End Select
                Next

                If Not goodUrls.Any Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Invalid URL(s)"
                        .Description = "URL(s) provided were invalid."
                    End With

                    Await ctx.RespondAsync(embed:=embed.Build)
                    Return
                End If

                If badUrls.Any Then
                    With embed
                        .Title = "Tracks Added"
                        .Description = $"Out of {trackUrls.Count}, only {goodUrls.Count} were valid.{Environment.NewLine}"
                        .Description &= $"Invalid URLs: {Formatter.BlockCode(String.Join(", ", badUrls.Select(Function(u) $"`{u}`")))}"
                    End With
                Else
                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Title = "Tracks Added"
                        .Description = $"{goodUrls.Count.ToMetric} tracks were added to `{playlist.Name}`."
                    End With
                End If

                playlist.TrackUrls.AddRange(goodUrls)
                GuildData.UserPlaylists.Add(playlist)
                UpdateGuildData()

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("remove")>
            <Description("Remove tracks from a playlist.")>
            Public Async Function RemoveTracksCommand(ctx As CommandContext, playlistName As String, <RemainingText> trackNumbers As Integer()) As Task
                Throw New NotImplementedException
            End Function

            <Command("enqueue")>
            <Description("Add all tracks from a playlist to the playback queue.")>
            Public Async Function EnqueuePlaylistCommand(ctx As CommandContext) As Task
                Throw New NotImplementedException
            End Function

            <Command("thumbnail")>
            <Description("Add or change the thumbnail for a playlist.")>
            Public Async Function PlaylistThumbnailCommand(ctx As CommandContext, playlistName As String, thumbnailUrl As String) As Task
                Throw New NotImplementedException
            End Function
        End Class
    End Class

End Namespace