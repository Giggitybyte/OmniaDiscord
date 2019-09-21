Imports System.Collections.Concurrent
Imports System.Threading
Imports Lavalink4NET
Imports Lavalink4NET.Decoding
Imports Lavalink4NET.Events
Imports Lavalink4NET.Player

Namespace Entities.Media

    ''' <summary>
    ''' A custom Lavalink player with a voting and queuing system.
    ''' </summary>
    Public Class OmniaLavalinkPlayer
        Inherits LavalinkPlayer

        Private _interruptTimestamp As TimeSpan?

        Public Shadows ReadOnly Property CurrentTrack As OmniaMediaInfo
        Public ReadOnly Property Queue As ConcurrentQueue(Of OmniaMediaInfo)
        Public Property RepeatMode As TrackRepeatMode

        Public Sub New(lavalinkSocket As LavalinkSocket, client As IDiscordClientWrapper, guildId As ULong)
            MyBase.New(lavalinkSocket, client, guildId, False)
            _Queue = New ConcurrentQueue(Of OmniaMediaInfo)
        End Sub

        Public Overrides Async Function OnTrackEndAsync(eventArgs As TrackEndEventArgs) As Task
            If eventArgs.MayStartNext Then Await SkipAsync()
            Await MyBase.OnTrackEndAsync(eventArgs)
        End Function

        Public Shadows Async Function PlayAsync(track As OmniaMediaInfo, Optional enqueue As Boolean = True, Optional startTime As TimeSpan? = Nothing, Optional endTime As TimeSpan? = Nothing) As Task(Of Integer)
            EnsureNotDestroyed()
            EnsureConnected()
            _interruptTimestamp = Nothing

            If enqueue AndAlso State = PlayerState.Playing Then
                _Queue.Enqueue(track)
                If State = PlayerState.NotPlaying Then Await SkipAsync()
                Return Queue.Count
            End If

            Await MyBase.PlayAsync(TrackDecoder.DecodeTrack(track.TrackIdentifier), startTime, endTime, False)
            Return 0
        End Function

        Public Async Function PlayTopAsync(track As OmniaMediaInfo) As Task
            EnsureNotDestroyed()

            If track Is Nothing Then
                Throw New ArgumentNullException(NameOf(track))
            End If

            If State = PlayerState.NotPlaying Then
                Await PlayAsync(track, False)
                Return
            End If

            _Queue.Prepend(track)
        End Function

        Public Async Function InterruptTrackAsync(track As OmniaMediaInfo) As Task(Of Boolean)
            If State = PlayerState.NotPlaying Then
                Await PlayAsync(track, False)
                Return False
            End If

            _interruptTimestamp = TrackPosition
            _Queue.Prepend(CurrentTrack)

            Await PlayAsync(track, False)
            Return True
        End Function

        Public Function SkipAsync(Optional count As Integer = 1) As Task
            If count <= 0 Or Not _Queue.Any Then Return Task.CompletedTask

            EnsureNotDestroyed()
            EnsureConnected()

            Dim track As OmniaMediaInfo = Nothing

            If _interruptTimestamp.HasValue Then
                Dim timestamp = _interruptTimestamp
                _interruptTimestamp = Nothing

                _Queue.TryDequeue(track)
                Return PlayAsync(track, False, timestamp.Value)
            End If

            If RepeatMode = TrackRepeatMode.CurrentTrack AndAlso CurrentTrack IsNot Nothing Then
                Return PlayAsync(CurrentTrack)
            End If

            While Math.Max(Interlocked.Decrement(count), count + 1) > 0
                If _Queue.Count < 1 Then Return DisconnectAsync() ' CHANGE THIS

                If RepeatMode = TrackRepeatMode.AllTracks AndAlso track IsNot Nothing Then
                    _Queue.Enqueue(track)
                End If

                _Queue.TryDequeue(track)
            End While

            Return PlayAsync(track)
        End Function

        Public Overrides Function StopAsync(Optional disconnect As Boolean = False) As Task
            _Queue.Clear()
            Return MyBase.StopAsync(disconnect)
        End Function

        Private Sub ShuffleTracks()

        End Sub
    End Class
End Namespace