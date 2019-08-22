Namespace Entities.Media
    Public Class OmniaUserPlaylist
        Public Property Id As String
        Public Property Name As String
        Public Property CreatorUserId As ULong
        Public Property ThumbnailUrl As String
        Public Property Tracks As New List(Of OmniaMediaInfo)
    End Class
End Namespace