Imports Newtonsoft.Json

Namespace Services.MediaRetrieval.Entities.Bandcamp
    Public Class BandcampMediaInfo

        <JsonProperty("current")>
        Public Property AlbumInfo As Album

        <JsonProperty("art_id")>
        Public Property ArtId As Long

        <JsonProperty("trackinfo")>
        Public Property Tracks As TrackInfo()

        <JsonProperty("url")>
        Public Property Url As String

        <JsonProperty("artist")>
        Public Property Artist As String

        <JsonProperty("item_type")>
        Public Property MediaType As String

        <JsonProperty("id")>
        Public Property Id As Long

        Public Structure TrackInfo

            <JsonProperty("title_link")>
            Public Property Path As String

            <JsonProperty("title")>
            Public Property Title As String

            <JsonProperty("track_id")>
            Public Property Id As Long

            <JsonProperty("duration")>
            Public Property Duration As Double

            <JsonProperty("file")>
            Public Property Mp3 As Mp3

        End Structure

        Public Class Mp3

            <JsonProperty("mp3-128")>
            Public Property DirectLink As String

        End Class

        Public Structure Album

            <JsonProperty("title")>
            Public Property Title As String

        End Structure

    End Class
End Namespace