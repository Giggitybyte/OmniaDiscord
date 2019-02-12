﻿Imports System.Collections.Concurrent
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
Imports OmniaDiscord.Services.Lavalink
Imports OmniaDiscord.Services.Lavalink.Entities
Imports OmniaDiscord.Services.MediaRetrieval
Imports OmniaDiscord.Services.MediaRetrieval.Entities

Namespace Commands.Modules

    <Group("music"), RequireGuild>
    <Description("Command group for the playback of music and other audio.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.AddReactions Or Permissions.UseExternalEmojis Or Permissions.UseVoice Or Permissions.Speak)>
    Public Class MediaPlaybackModule
        Inherits OmniaBaseModule

        Private _mediaRetrieval As MediaRetrievalService
        Private _lavalink As LavalinkService

        Sub New(mediaRetrieval As MediaRetrievalService, lavalink As LavalinkService)
            _mediaRetrieval = mediaRetrieval
            _lavalink = lavalink
        End Sub

        <Command("play"), Aliases("p")>
        <Description("Queue music and other audio for playback from a url.")>
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

                If media Is Nothing Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Cannot Retrieve Media"
                        .Description = "The link you provided was either from an unsupported platform or is inaccessible for me."
                    End With

                Else ' Build embed, join voice channel, queue and play media.
                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                            .Text = media.Origin
                        }
                    End With

                    Dim guildConnection As LavalinkGuildConnection = Await _lavalink.Node.ConnectAsync(mbrChannel)

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
                        If _lavalink.GuildInfo(ctx.Guild.Id).CurrentTrack Is Nothing Then Await _lavalink.PlayNextTrackAsync(ctx.Guild)
                    End If
                End If

                Await waitMessage.DeleteAsync()
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("stop"), Aliases("die", "leave")>
        <Description("Removes all tracks from the queue and leaves the voice channel.")>
        Public Async Function StopCommand(ctx As CommandContext) As Task
            Dim guildConnection As LavalinkGuildConnection = _lavalink.Node.GetConnection(ctx.Guild)

            If ctx.Guild.CurrentMember?.VoiceState IsNot Nothing Or guildConnection?.IsConnected Then
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
                        .Description = $"{DiscordEmoji.FromName(ctx.Client, ":ok_hand:")} Playback resumed."
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
                        embed.Description = "Skipped current track."

                    Else
                        Dim skipCount As Integer

                        If trackNumber > qCount Then
                            embed.Description = "Skipped to last track."
                            skipCount = qCount - 1

                        Else
                            embed.Description = $"Skipped to track number **{trackNumber}**."
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
                                        .Description &= "Skipped to the next playable track instead."
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
        Public Async Function DisplayQueueCommand(ctx As CommandContext) As Task
            Dim playbackQueue As List(Of OmniaMediaInfo) = _lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.ToList

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

                If pages.Count > 1 Then
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

        Private Async Function DoQueuePaginationAsync(ctx As CommandContext, pages As List(Of String), timeout As Integer) As Task
            For page As Integer = 1 To pages.Count
                Dim index As Integer = page - 1
                pages(index) = $"Page {page.ToString("N0")}{Environment.NewLine}============={Environment.NewLine}{pages(index).Trim}"
            Next

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

                                      If emoji = emojis.SkipLeft AndAlso pageNumber > 1 Then
                                          pageNumber = 1

                                      ElseIf emoji = emojis.Left AndAlso pageNumber <> 1 Then
                                          pageNumber -= 1

                                      ElseIf emoji = emojis.[Stop] Then
                                          ct.Cancel()

                                      ElseIf emoji = emojis.Right Then
                                          If pageNumber <> pages.Count Then pageNumber += 1

                                      ElseIf emoji = emojis.SkipRight Then
                                          pageNumber = pages.Count

                                      Else
                                          Return

                                      End If

                                  Else
                                      Return
                                  End If

                              ElseIf TypeOf e Is MessageReactionsClearEventArgs Then
                                  Await ctx.Client.GetInteractivity.GeneratePaginationReactions(message, New PaginationEmojis(ctx.Client))
                              End If

                              If ct.IsCancellationRequested = False Then Await message.ModifyAsync(Formatter.BlockCode(pages(pageNumber - 1), "markdown"))
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

        <Group("test"), RequireOwner>
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