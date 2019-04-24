Namespace Entites.Media

    Public Class OmniaMediaInfo

        ''' <summary>
        ''' The title or name of the media.
        ''' </summary>
        Public Property Title As String

        ''' <summary>
        ''' The artist, user, or channel that uploaded the media.
        ''' </summary>
        Public Property Author As String

        ''' <summary>
        ''' Thumbnail or artwork for the media.
        ''' </summary>
        Public Property ThumbnailUrl As String

        ''' <summary>
        ''' The total duration of the media.
        ''' </summary>
        Public Property Duration As TimeSpan

        ''' <summary>
        ''' The original front-facing url for the media.
        ''' </summary>
        Public Property Url As String

        ''' <summary>
        ''' The direct url to the media. Only applicable to media of type <see cref="OmniaMediaType.Track"/>.
        ''' </summary>
        Public Property DirectUrl As String

        ''' <summary>
        ''' The name of the platform or source of the media.
        ''' </summary>
        Public Property Origin As String

        ''' <summary>
        ''' The type of this media.
        ''' </summary>
        Public Property Type As OmniaMediaType

        ''' <summary>
        ''' The tracks that make up the media. Only applicable to media of type <see cref="OmniaMediaType.Album"/> or <see cref="OmniaMediaType.Playlist"/>.
        ''' </summary>
        Public Property Tracks As New List(Of OmniaMediaInfo)

        ''' <summary>
        ''' The Discord user ID of the user who requested the media.
        ''' </summary>
        Public Property Requester As ULong
    End Class

End Namespace