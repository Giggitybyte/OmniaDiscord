Imports System.Collections.Concurrent
Imports System.Text
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Interactivity
Imports DSharpPlus.Lavalink
Imports OmniaDiscord.Extensions
Imports OmniaDiscord.Entites
Imports OmniaDiscord.Services
Imports YoutubeExplode
Imports YoutubeExplode.Models

Namespace Commands.Modules

    <Group("music"), RequireGuild, ModuleLifespan(ModuleLifespan.Transient)>
    <Description("Command group for the playback of music and other audio.")>
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
            Dim mbrChannel As DiscordChannel = ctx.Member.VoiceState?.Channel
            Dim currentChn As DiscordChannel = ctx.Guild.CurrentMember?.VoiceState?.Channel

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

                Dim waitMessage As DiscordMessage = Await ctx.RespondAsync(embed:=embed.Build)
                Await ctx.TriggerTypingAsync
                Dim media As OmniaMediaInfo = Await _mediaRetrieval.GetMediaAsync(url)

                embed = Await QueueMediaAsync(ctx, media)

                Await waitMessage.DeleteAsync()
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("upload")>
        <Description("Queue music, videos, and other audio for playback from a file attachment.")>
        <Cooldown(3, 15, CooldownBucketType.User)>
        Public Async Function UploadCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim validExtensions As String() = {
                ".mp3",
                ".wav",
                ".flac",
                ".ogg",
                ".mp4",
                ".webm"
            }

            If ctx.Message.Attachments.Count > 0 Then
                Dim attachments As New List(Of OmniaMediaInfo)
                Dim media As OmniaMediaInfo = Nothing

                For Each attachment In ctx.Message.Attachments
                    Dim fileName As String = ctx.Message.Attachments.First.FileName
                    Dim extIndex As Integer = fileName.LastIndexOf("."c)

                    If extIndex <> -1 AndAlso validExtensions.Contains(fileName.Substring(extIndex).ToLower) Then
                        Dim info As New OmniaMediaInfo With {
                            .Author = ctx.Member.Mention,
                            .DirectUrl = attachment.Url,
                            .Origin = "File Upload",
                            .Requester = ctx.Member.Id,
                            .ThumbnailUrl = $"{OmniaConfig.ResourceUrl}/assets/omnia/MediaDefault.png",
                            .Title = fileName,
                            .Type = OmniaMediaType.Track,
                            .Url = attachment.Url
                        }

                        attachments.Add(info)
                    End If
                Next

                If attachments.Count = 1 Then
                    media = attachments.First

                ElseIf attachments.Count > 1 Then
                    media = New OmniaMediaInfo With {
                        .Author = attachments.First.Author,
                        .Origin = "Discord",
                        .Requester = attachments.First.Requester,
                        .ThumbnailUrl = attachments.First.ThumbnailUrl,
                        .Title = "File Uploads",
                        .Tracks = attachments,
                        .Type = OmniaMediaType.Playlist
                    }

                Else
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Invalid File Extenstion"
                        .Description = $"Valid extensions: {String.Join(", ", validExtensions)}"
                    End With

                End If

                If media IsNot Nothing Then embed = Await QueueMediaAsync(ctx, media)

            Else
                With embed
                    .Color = DiscordColor.Red
                    .Description = $"Nothing to queue; there was nothing uploaded along with your command."
                End With
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("search")>
        <Description("Searches YouTube for your query and returns the top five results for you to select.")>
        <Cooldown(1, 6, CooldownBucketType.User)>
        Public Async Function SearchCommand(ctx As CommandContext, <RemainingText> query As String) As Task
            Await ctx.TriggerTypingAsync

            Dim ytResults As List(Of Video) = Await New YoutubeClient().SearchVideosAsync(query, 1)
            Dim embed As New DiscordEmbedBuilder

            If ytResults.Count = 0 Then
                With embed
                    .Color = DiscordColor.Red
                    .Description = $"No videos were found with that query."
                End With

                Await ctx.RespondAsync(embed:=embed.Build)
            Else
                Dim builder As New StringBuilder
                Dim emojis As New List(Of DiscordEmoji) From {
                    DiscordEmoji.FromName(ctx.Client, ":one:"),
                    DiscordEmoji.FromName(ctx.Client, ":two:"),
                    DiscordEmoji.FromName(ctx.Client, ":three:"),
                    DiscordEmoji.FromName(ctx.Client, ":four:"),
                    DiscordEmoji.FromName(ctx.Client, ":five:")
                }

                If ytResults.Count > 5 Then ytResults = ytResults.Take(5).ToList

                For index As Integer = 0 To ytResults.Count - 1
                    Dim ytVideo As Video = ytResults(index)
                    Dim title As String = ytVideo.Title
                    Dim details As String = $"{ytVideo.Title}{Environment.NewLine}{Environment.NewLine}Uploaded by {ytVideo.Author}"

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

                Dim message As DiscordMessage = Await ctx.RespondAsync(embed:=embed.Build)

                For index As Integer = 0 To ytResults.Count - 1
                    Await message.CreateReactionAsync(emojis(index))
                Next

                Dim predicate = Function(e As DiscordEmoji)
                                    For Each emoji In emojis
                                        If e = emoji Then Return True
                                    Next
                                    Return False
                                End Function

                Dim interactivity As InteractivityExtension = ctx.Client.GetInteractivity()
                Dim userReaction As ReactionContext = Await interactivity.WaitForMessageReactionAsync(predicate, message, ctx.User, TimeSpan.FromSeconds(15))

                embed.Footer = Nothing

                If userReaction Is Nothing Then
                    embed.Color = DiscordColor.Red
                    Await message?.ModifyAsync(embed:=embed.Build)

                Else
                    Await ctx.TriggerTypingAsync

                    Dim selectedVideo As Video = ytResults(emojis.IndexOf(userReaction.Emoji))
                    Dim media As OmniaMediaInfo = Await _mediaRetrieval.GetMediaAsync(selectedVideo.GetUrl)

                    embed.Color = DiscordColor.SpringGreen
                    Await message?.ModifyAsync(embed:=embed.Build)
                    embed = Await QueueMediaAsync(ctx, media)

                    Await ctx.RespondAsync(embed:=embed.Build)
                End If

                For index As Integer = 0 To ytResults.Count - 1
                    Await message?.DeleteOwnReactionAsync(emojis(index))
                Next
            End If


        End Function

        <Command("stop"), Aliases("die", "leave")>
        <Description("Removes all tracks from the queue and leaves the voice channel.")>
        Public Async Function StopCommand(ctx As CommandContext) As Task
            Dim guildConnection As LavalinkGuildConnection = _lavalink.Node.GetConnection(ctx.Guild)

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
            Dim playbackInfo As GuildPlaybackInfo = _lavalink.GuildInfo(ctx.Guild.Id)

            If playbackInfo?.CurrentTrack IsNot Nothing Then
                Dim embed As New DiscordEmbedBuilder With {
                    .Description = $"To resume playback, type `{ctx.Prefix}music resume`"
                }

                If playbackInfo?.IsPlaying Then
                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Description = .Description.Insert(0, $"Audio playback paused.{Environment.NewLine}")
                    End With

                    _lavalink.Node.GetConnection(ctx.Guild).Pause()
                    _lavalink.GuildInfo(ctx.Guild.Id).IsPlaying = False

                Else
                    With embed
                        .Color = DiscordColor.Orange
                        .Description = .Description.Insert(0, $"Audio playback already paused.{Environment.NewLine}")
                    End With

                End If

                Await ctx.RespondAsync(embed:=embed.Build)
            End If

        End Function

        <Command("resume")>
        <Description("Resumes audio playback.")>
        Public Async Function ResumeCommand(ctx As CommandContext) As Task
            Dim playbackInfo As GuildPlaybackInfo = _lavalink.GuildInfo(ctx.Guild.Id)

            If playbackInfo?.CurrentTrack IsNot Nothing Then
                Dim embed As New DiscordEmbedBuilder

                If playbackInfo?.IsPlaying = False Then
                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Description = "Playback resumed."
                    End With

                    _lavalink.Node.GetConnection(ctx.Guild).Resume()
                    _lavalink.GuildInfo(ctx.Guild.Id).IsPlaying = True

                Else
                    With embed
                        .Color = DiscordColor.Red
                        .Description = "Playback isn't currently paused."
                    End With

                End If

                Await ctx.RespondAsync(embed:=embed.Build)
            End If

        End Function

        <Command("skip")>
        <Description("Skips the current track and begins playback for the next track in the queue.")>
        Public Async Function SkipCommand(ctx As CommandContext) As Task
            Await SkipToCommand(ctx, 1)
        End Function

        <Command("skipto")>
        <Description("Begins playback for the specified track, skipping all other tracks before it.")>
        Public Async Function SkipToCommand(ctx As CommandContext, trackNumber As Integer) As Task
            Dim playbackInfo As GuildPlaybackInfo = _lavalink.GuildInfo(ctx.Guild.Id)
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}

            If playbackInfo?.CurrentTrack Is Nothing Then
                embed.Description = "Nothing is currently playing."

            ElseIf playbackInfo?.MediaQueue.Count > 0 Then
                Dim qCount As Integer = playbackInfo.MediaQueue.Count
                embed.Color = DiscordColor.SpringGreen

                Await ctx.TriggerTypingAsync

                If trackNumber > 0 Then
                    If trackNumber = 1 Then
                        embed.Description = "Skipping current track..."

                    Else
                        Dim skipCount As Integer

                        If trackNumber > qCount Then
                            embed.Description = "Skipping to last track..."
                            skipCount = qCount - 1

                        Else
                            embed.Description = $"Skipping to track {trackNumber}..."
                            skipCount = trackNumber - 1
                        End If

                        Dim newQueue As New ConcurrentQueue(Of OmniaMediaInfo)(_lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Skip(skipCount))
                        _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue = newQueue
                    End If

                    Dim isSuccess As Boolean = Await _lavalink.PlayNextTrackAsync(ctx.Guild)

                    If isSuccess = False Then
                        embed.Description = $"The track you skipped to was unplayable.{Environment.NewLine}"

                        Do Until isSuccess
                            If _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Count > 0 Then
                                isSuccess = Await _lavalink.PlayNextTrackAsync(ctx.Guild)

                                If isSuccess Then
                                    With embed
                                        .Color = DiscordColor.Yellow
                                        .Description &= "Skipping to the next playable track instead."
                                    End With
                                End If

                            Else
                                With embed
                                    .Color = DiscordColor.Red
                                    .Description = "There are no more playable tracks; the queue is now empty."
                                End With

                                Exit Do
                            End If
                        Loop

                    End If

                Else
                    With embed
                        .Color = DiscordColor.Red
                        .Description = "Invalid track number."
                    End With
                End If

            Else
                embed.Description = "Nothing to skip to; the queue is empty."
            End If

            Await ctx.RespondAsync(embed:=embed.Build)

        End Function

        <Command("movetop")>
        <Description("Moves the specified track to the top of the queue.")>
        Public Async Function MoveTopCommand(ctx As CommandContext, trackNumber As Integer) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}

            If trackNumber > 0 Then
                If trackNumber = 1 Then
                    embed.Description = $"You can't move the next track to the top of the queue.{Environment.NewLine}It's already at the top, dingus."

                Else
                    Dim queue As List(Of OmniaMediaInfo) = _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.ToList
                    Dim track As OmniaMediaInfo

                    embed.Color = DiscordColor.SpringGreen

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
                End If
            End If

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

            Else
                Dim strBuilder As New StringBuilder
                Dim pages As New List(Of String)

                Await ctx.TriggerTypingAsync

                For trackNumber As Integer = 1 To playbackQueue.Count
                    Dim title As String = playbackQueue(trackNumber - 1).Title

                    If title.Count > 95 Then
                        title = $"{title.Substring(0, 92)}..."
                    End If

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

                    Await DoQueuePaginationAsync(ctx, pages, 30000)
                Else
                    Await ctx.RespondAsync(Formatter.BlockCode(pages.First, "markdown"))
                End If
            End If

        End Function

        <Command("nowplaying"), Aliases("np", "currenttrack", "current")>
        <Description("Displays all info for the song that is currently playing, including album art and original link.")>
        Public Async Function DisplayCurrentTrack(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder With {.Title = "Currently Playing"}
            Dim currentTrack As OmniaMediaInfo = _lavalink.GuildInfo(ctx.Guild.Id).CurrentTrack

            If currentTrack IsNot Nothing Then
                Dim duration As String = If(currentTrack.Duration.TotalSeconds > 0, Utilities.FormatTimespanToString(currentTrack.Duration), "Unknown")
                Dim requester As DiscordMember = Await ctx.Guild.GetMemberAsync(currentTrack.Requester)

                With embed
                    .Color = DiscordColor.CornflowerBlue

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
                    .Description = "Nothing"
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

            If queue.Count > 0 Then
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
            Else
                With embed
                    .Color = DiscordColor.Red
                    .Description = "There was nothing to remove; the queue is empty."
                End With

            End If

            Await ctx.RespondAsync(embed:=embed)
        End Function
#End Region

#Region "Helper Methods"
        Private Async Function QueueMediaAsync(ctx As CommandContext, media As OmniaMediaInfo) As Task(Of DiscordEmbedBuilder)
            Dim embed As New DiscordEmbedBuilder

            If media Is Nothing Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Cannot Retrieve Media"
                    .Description = "The provided URL was either from an unsupported platform or is inaccessible for me."
                End With

            Else ' Build embed, join voice channel, queue and play media.
                With embed
                    .Color = DiscordColor.SpringGreen
                    .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                        .Text = media.Origin
                    }
                End With

                Dim guildConnection As LavalinkGuildConnection = Await _lavalink.Node.ConnectAsync(ctx.Member.VoiceState.Channel)

                If media.Type = OmniaMediaType.Track Then
                    media.Requester = ctx.Member.Id
                    _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Enqueue(media)

                    With embed
                        .Title = "Queued Track"
                        .Description = $"**[{media.Title}]({media.Url})**{Environment.NewLine}{media.Author}"
                        .ThumbnailUrl = media.ThumbnailUrl

                        If media.Duration.TotalSeconds > 0 Then .Description &= $"{Environment.NewLine}*{Utilities.FormatTimespanToString(media.Duration)}*"
                    End With

                ElseIf media.Type = OmniaMediaType.Album Or media.Type = OmniaMediaType.Playlist Then
                    For Each track As OmniaMediaInfo In media.Tracks
                        track.Requester = ctx.Member.Id
                        _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Enqueue(track)
                    Next

                    With embed
                        .Title = $"Queued {media.Tracks.Count} Tracks"

                        .Description = $"**[{media.Title}]({media.Url})**{Environment.NewLine}"
                        .Description &= $"{media.Author}{Environment.NewLine}{Environment.NewLine}"
                        .Description &= $"Total Playtime: {Utilities.FormatTimespanToString(media.Duration)}"

                        .ThumbnailUrl = media.ThumbnailUrl
                        .Footer.Text &= $" {media.Type}"
                    End With
                End If

                If guildConnection IsNot Nothing AndAlso guildConnection.IsConnected Then
                    If _lavalink.GuildInfo(ctx.Guild.Id).CurrentTrack Is Nothing Then
                        Dim isSuccess As Boolean

                        Do
                            isSuccess = Await _lavalink.PlayNextTrackAsync(ctx.Guild)
                        Loop Until isSuccess
                    End If
                End If
            End If

            Return embed
        End Function

        Private Async Function DoQueuePaginationAsync(ctx As CommandContext, pages As List(Of String), timeout As Integer) As Task
            Dim tsc As New TaskCompletionSource(Of String)
            Dim ct As New CancellationTokenSource(timeout)
            ct.Token.Register(Sub() tsc.TrySetResult(Nothing))

            Dim pageNumber As Integer = 1
            Dim message As DiscordMessage = Await ctx.RespondAsync(Formatter.BlockCode(pages(pageNumber - 1), "markdown"))
            Await ctx.Client.GetInteractivity.GeneratePaginationReactions(message, New PaginationEmojis(ctx.Client))

            Dim handler = Async Function(e)
                              If TypeOf e Is MessageReactionAddEventArgs Or TypeOf e Is MessageReactionRemoveEventArgs Then
                                  If e.Message.Id = message.Id AndAlso e.User.Id <> ctx.Client.CurrentUser.Id AndAlso e.User.Id = ctx.Member.Id Then
                                      Dim emoji As DiscordEmoji = DirectCast(e.Emoji, DiscordEmoji)
                                      Dim emojis As New PaginationEmojis(ctx.Client)

                                      ct.Dispose()
                                      ct = New CancellationTokenSource(timeout)
                                      ct.Token.Register(Sub() tsc.TrySetResult(Nothing))


                                      Select Case emoji
                                          Case emojis.SkipLeft
                                              pageNumber = 1

                                          Case emojis.Left
                                              If pageNumber <> 1 Then pageNumber -= 1

                                          Case emojis.Stop
                                              ct.Cancel()

                                          Case emojis.Right
                                              If pageNumber <> pages.Count Then pageNumber += 1

                                          Case emojis.SkipRight
                                              pageNumber = pages.Count

                                          Case Else
                                              Return
                                      End Select

                                  Else
                                      Return
                                  End If

                              ElseIf TypeOf e Is MessageReactionsClearEventArgs Then
                                  Await ctx.Client.GetInteractivity.GeneratePaginationReactions(message, New PaginationEmojis(ctx.Client))
                              End If

                              If ct.IsCancellationRequested = False Then
                                  Await message.ModifyAsync(Formatter.BlockCode(pages(pageNumber - 1), "markdown"))
                              End If
                          End Function

            Try
                AddHandler ctx.Client.MessageReactionAdded, handler
                AddHandler ctx.Client.MessageReactionRemoved, handler
                AddHandler ctx.Client.MessageReactionsCleared, handler

                Await tsc.Task.ConfigureAwait(False)
            Catch ex As Exception
                Throw

            Finally
                RemoveHandler ctx.Client.MessageReactionAdded, handler
                RemoveHandler ctx.Client.MessageReactionRemoved, handler
                RemoveHandler ctx.Client.MessageReactionsCleared, handler

            End Try

            ct.Dispose()
            Await message.DeleteAllReactionsAsync
        End Function
#End Region

        <Group("test"), RequireOwner, ModuleLifespan(ModuleLifespan.Transient)>
        <Description("Command group containing subcommands to test specific parts of Lavalink playback.")>
        Public Class DebugModule
            Inherits BaseCommandModule

            Private _lavalink As LavalinkService

            Sub New(lavalink As LavalinkService)
                _lavalink = lavalink
            End Sub

            <Command("equalizer"), Aliases("eq")>
            Public Async Function EqTestCommand(ctx As CommandContext, band As Integer, gain As Single) As Task
                Dim connection As LavalinkGuildConnection = _lavalink.Node.GetConnection(ctx.Guild)
                connection.AdjustEqualizer(New LavalinkBandAdjustment(band, gain))

                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
            End Function

            <Command("equalizer")>
            Public Async Function EqTestCommand(ctx As CommandContext, gain As Single) As Task
                Dim connection As LavalinkGuildConnection = _lavalink.Node.GetConnection(ctx.Guild)
                Dim bands As New List(Of LavalinkBandAdjustment)

                For band As Integer = 0 To 14
                    bands.Add(New LavalinkBandAdjustment(band, gain))
                Next

                connection.AdjustEqualizer(bands.ToArray)

                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
            End Function

            <Command("equalizer")>
            Public Async Function EqTestCommand(ctx As CommandContext) As Task
                Dim connection As LavalinkGuildConnection = _lavalink.Node.GetConnection(ctx.Guild)
                connection.ResetEqualizer()

                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":sweat_drops:"))
            End Function

        End Class

    End Class

End Namespace