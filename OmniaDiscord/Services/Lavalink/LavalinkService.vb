Imports System.Collections.Concurrent
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Lavalink
Imports DSharpPlus.Lavalink.EventArgs
Imports DSharpPlus.Net
Imports OmniaDiscord.Services.Lavalink.Entities

Namespace Services.Lavalink

    Public Class LavalinkService
        Private WithEvents _nodeConnection As LavalinkNodeConnection
        Private _logger As LogService
        Private _omniaConfig As Bot.Configuration

        Public ReadOnly Property GuildInfo As ConcurrentDictionary(Of ULong, GuildPlaybackInfo)

        Public ReadOnly Property Node As LavalinkNodeConnection
            Get
                Return _nodeConnection
            End Get
        End Property

        Sub New(client As DiscordShardedClient, config As Bot.Configuration, logger As LogService)
            AddHandler client.Ready, AddressOf InitLavalinkNode
            AddHandler client.GuildAvailable, AddressOf DiscordGuildAvailableHandler

            _omniaConfig = config
            _logger = logger

            _GuildInfo = New ConcurrentDictionary(Of ULong, GuildPlaybackInfo)
        End Sub

        Private Async Function InitLavalinkNode(args As ReadyEventArgs) As Task
            If _nodeConnection Is Nothing Then
                Dim euConfig As New LavalinkConfiguration With {
                    .SocketEndpoint = New ConnectionEndpoint With {.Hostname = _omniaConfig.LavalinkIpAddress, .Port = 2333},
                    .RestEndpoint = New ConnectionEndpoint With {.Hostname = _omniaConfig.LavalinkIpAddress, .Port = 2333},
                    .Password = _omniaConfig.LavalinkPasscode
                }

                Dim lavalink As LavalinkExtension = args.Client.GetLavalink
                _nodeConnection = Await lavalink.ConnectAsync(euConfig)
            End If
        End Function

        Public Async Function PlayNextTrackAsync(guild As DiscordGuild) As Task(Of Boolean)
            _GuildInfo(guild.Id).MediaQueue.TryDequeue(_GuildInfo(guild.Id).CurrentTrack)

            If _GuildInfo(guild.Id).CurrentTrack IsNot Nothing Then
                Dim lavaRequest As LavalinkLoadResult = Await _nodeConnection.GetTracksAsync(New Uri(_GuildInfo(guild.Id).CurrentTrack.DirectUrl))

                If lavaRequest.LoadResultType = LavalinkLoadResultType.LoadFailed Then
                    Await _logger.PrintAsync(LogLevel.Warning, "Lavalink", $"Unable load URL: { _GuildInfo(guild.Id).CurrentTrack.DirectUrl} (Guild: {guild.Id})")

                Else
                    _nodeConnection.GetConnection(guild)?.Play(lavaRequest.Tracks.First)
                    _GuildInfo(guild.Id).IsPlaying = True
                    Return True

                End If
            End If

            Return False
        End Function

        Private Async Function DisconnectedHandler(e As NodeDisconnectedEventArgs) As Task Handles _nodeConnection.Disconnected
            Await _logger.PrintAsync(LogLevel.Warning, "Lavalink Service", "Luke implement auto reconnect you fuck.")
        End Function

        Private Async Function LavalinkSocketErroredHandler(e As SocketErrorEventArgs) As Task Handles _nodeConnection.LavalinkSocketErrored
            Await _logger.PrintAsync(LogLevel.Error, "Lavalink Service", $"Socket error: {e.Exception.Message}")
        End Function

        Private Async Function PlaybackFinishedHandler(e As TrackFinishEventArgs) As Task Handles _nodeConnection.PlaybackFinished
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
                            Await _logger.PrintAsync(LogLevel.Debug, "Lavalink Service", $"Playing next track for {guild.Id} (Skips: {skipCount})")
                        Else
                            skipCount += 1
                        End If
                    Loop

                End If
            End If
        End Function

        Private Async Function TrackExceptionHandler(e As TrackExceptionEventArgs) As Task Handles _nodeConnection.TrackException
            Await _logger.PrintAsync(LogLevel.Error, "Lavalink Service", $"Track errored in {e.Player.Guild.Id}: {e.Error}")
        End Function

        Private Async Function TrackStuckHandler(e As TrackStuckEventArgs) As Task Handles _nodeConnection.TrackStuck
            Await _logger.PrintAsync(LogLevel.Warning, "Lavalink Service", $"Track got stuck in {e.Player.Guild.Id}. Threshold: {e.ThresholdMilliseconds}ms")
        End Function

        Private Function DiscordGuildAvailableHandler(args As GuildCreateEventArgs) As Task
            If _GuildInfo.ContainsKey(args.Guild.Id) = False Then
                _GuildInfo.TryAdd(args.Guild.Id, New GuildPlaybackInfo)
            End If

            Return Task.CompletedTask
        End Function
    End Class

End Namespace