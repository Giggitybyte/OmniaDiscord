﻿Imports System.Collections.Concurrent
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Lavalink
Imports DSharpPlus.Lavalink.EventArgs
Imports DSharpPlus.Net
Imports OmniaDiscord.Entities.Media

Namespace Services

    Public Class LavalinkService
        Private _nodeConnection As LavalinkNodeConnection
        Private _logger As LogService

        Public ReadOnly Property GuildInfo As ConcurrentDictionary(Of ULong, GuildPlaybackInfo)

        Public ReadOnly Property Node As LavalinkNodeConnection
            Get
                Return _nodeConnection
            End Get
        End Property

        Sub New(shardedClient As DiscordShardedClient, logger As LogService)
            AddHandler shardedClient.GuildCreated, Function(args)
                                                       AddGuildInfo(args.Guild.Id)
                                                       Return Task.CompletedTask
                                                   End Function

            AddHandler shardedClient.GuildDeleted, Function(args)
                                                       If _GuildInfo.TryRemove(args.Guild.Id, Nothing) Then
                                                           _logger.Print(LogLevel.Debug,
                                                                         "Lavalink Service",
                                                                         $"Removed playback information for guild {args.Guild.Id}")
                                                       End If

                                                       Return Task.CompletedTask
                                                   End Function

            AddHandler shardedClient.VoiceStateUpdated, AddressOf VoiceDisconnectedHandlerAsync

            _logger = logger
            _GuildInfo = New ConcurrentDictionary(Of ULong, GuildPlaybackInfo)
        End Sub

        Public Async Sub InitLavalinkNode(client As DiscordClient)
            If _nodeConnection IsNot Nothing Then Return

            For Each guild In client.Guilds.Keys
                AddGuildInfo(guild)
            Next

            Dim lavaConfig As New LavalinkConfiguration With {
                .SocketEndpoint = New ConnectionEndpoint With {.Hostname = Bot.Config.LavalinkIpAddress, .Port = 2333},
                .RestEndpoint = New ConnectionEndpoint With {.Hostname = Bot.Config.LavalinkIpAddress, .Port = 2333},
                .Password = Bot.Config.LavalinkPasscode
            }

            Dim lavalink As LavalinkExtension = client.GetLavalink
            _nodeConnection = Await lavalink.ConnectAsync(lavaConfig)

            AddHandler _nodeConnection.PlaybackFinished, AddressOf PlaybackFinishedHandler
            AddHandler _nodeConnection.TrackException, AddressOf TrackExceptionHandler
            AddHandler _nodeConnection.TrackStuck, AddressOf TrackStuckHandler
            AddHandler _nodeConnection.Disconnected, AddressOf NodeDisconnectHandler
            AddHandler _nodeConnection.LavalinkSocketErrored, AddressOf LavalinkSocketErroredHandler
        End Sub

        Private Function VoiceDisconnectedHandlerAsync(e As VoiceStateUpdateEventArgs) As Task
            If e.User.Id = e.Guild.CurrentMember.Id AndAlso e.Guild.CurrentMember.VoiceState?.Channel Is Nothing Then
                Dim guildConnection = _nodeConnection?.GetConnection(e.Guild)
                If guildConnection Is Nothing Then Return Task.CompletedTask

                guildConnection.Stop()
                guildConnection.SetVolume(100)
                guildConnection.ResetEqualizer()
                guildConnection.Disconnect()
                _GuildInfo(e.Guild.Id).ResetTrackData()
            End If

            Return Task.CompletedTask
        End Function

        Public Async Function PlayNextTrackAsync(guild As DiscordGuild) As Task(Of Boolean)
            _GuildInfo(guild.Id).MediaQueue.TryDequeue(_GuildInfo(guild.Id).CurrentTrack)

            If _GuildInfo(guild.Id).CurrentTrack IsNot Nothing Then
                Dim lavaRequest As LavalinkLoadResult = Await _nodeConnection.GetTracksAsync(New Uri(_GuildInfo(guild.Id).CurrentTrack.DirectUrl))

                If lavaRequest.LoadResultType = LavalinkLoadResultType.LoadFailed Then
                    Await _logger.PrintAsync(LogLevel.Warning,
                                             "Lavalink",
                                             $"Unable load URL: { _GuildInfo(guild.Id).CurrentTrack.DirectUrl} (Guild: {guild.Id})")

                Else
                    _nodeConnection.GetConnection(guild)?.Play(lavaRequest.Tracks.First)
                    _GuildInfo(guild.Id).IsPlaying = True
                    Return True

                End If
            End If

            Return False
        End Function

        Private Async Function NodeDisconnectHandler(e As NodeDisconnectedEventArgs) As Task
            Await _logger.PrintAsync(LogLevel.Warning, "Lavalink Service", "Luke implement auto reconnect you fuck.")
        End Function

        Private Async Function LavalinkSocketErroredHandler(e As SocketErrorEventArgs) As Task
            Await _logger.PrintAsync(LogLevel.Error, "Lavalink Service", $"Socket error: {e.Exception.Message}")
        End Function

        Private Async Function PlaybackFinishedHandler(e As TrackFinishEventArgs) As Task
            Dim guild As DiscordGuild = e.Player.Guild

            Await _logger.PrintAsync(LogLevel.Debug, "Lavalink Service", $"Track ended playback in {guild.Id}. Reason: {e.Reason}.")

            If e.Reason <> TrackEndReason.Replaced And e.Reason <> TrackEndReason.Stopped Then
                _GuildInfo(guild.Id).CurrentTrack = Nothing
                _GuildInfo(guild.Id).IsPlaying = False
                _GuildInfo(guild.Id).SkipVotes.Clear()

                If _GuildInfo(guild.Id).MediaQueue.Count > 0 Then
                    Dim isLoadingSuccessful As Boolean
                    Dim skipCount As Integer

                    Do Until _GuildInfo(guild.Id).MediaQueue.Count = 0 OrElse isLoadingSuccessful
                        isLoadingSuccessful = Await PlayNextTrackAsync(guild)

                        If isLoadingSuccessful Then
                            Await _logger.PrintAsync(LogLevel.Debug,
                                                     "Lavalink Service",
                                                     $"Playing next track for {guild.Id} (Skips: {skipCount})")
                        Else
                            skipCount += 1
                        End If
                    Loop

                End If
            End If

        End Function

        Private Async Function TrackExceptionHandler(e As TrackExceptionEventArgs) As Task
            Await _logger.PrintAsync(LogLevel.Error, "Lavalink Service", $"Track errored in {e.Player.Guild.Id}: {e.Error}")
        End Function

        Private Async Function TrackStuckHandler(e As TrackStuckEventArgs) As Task
            Await _logger.PrintAsync(LogLevel.Warning, "Lavalink Service", $"Track got stuck in {e.Player.Guild.Id}. Threshold: {e.ThresholdMilliseconds}ms")
        End Function

        Public Sub AddGuildInfo(guild As ULong)
            If Not _GuildInfo.ContainsKey(guild) AndAlso _GuildInfo.TryAdd(guild, New GuildPlaybackInfo) Then
                _logger.Print(LogLevel.Debug, "Lavalink Service", $"Created playback information for guild {guild}")
            End If
        End Sub
    End Class

End Namespace