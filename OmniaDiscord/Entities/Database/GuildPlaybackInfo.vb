Imports System.Collections.Concurrent
Imports OmniaDiscord.Entites.Media

Namespace Entites.Database

    Public Class GuildPlaybackInfo
        Public Property CurrentTrack As OmniaMediaInfo
        Public Property SkipVotes As HashSet(Of ULong)
        Public Property MediaQueue As ConcurrentQueue(Of OmniaMediaInfo)
        Public Property IsPlaying As Boolean
        Public Property RepeatMode As TrackRepeatMode
        Public Property Volume As Integer

        Sub New()
            _CurrentTrack = Nothing
            _SkipVotes = New HashSet(Of ULong)
            _IsPlaying = False
            _RepeatMode = TrackRepeatMode.Off
            _MediaQueue = New ConcurrentQueue(Of OmniaMediaInfo)
            _Volume = 100
        End Sub

        Public Sub ResetTrackData()
            _CurrentTrack = Nothing
            _SkipVotes.Clear()
            _MediaQueue.Clear()
            _RepeatMode = TrackRepeatMode.Off
            _Volume = 100
        End Sub
    End Class

End Namespace
