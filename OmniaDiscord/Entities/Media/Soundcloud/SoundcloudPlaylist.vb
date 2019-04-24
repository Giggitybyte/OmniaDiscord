Imports Newtonsoft.Json

Namespace Entites.Media.Soundcloud

    Public Class SoundcloudPlaylist

        <JsonProperty("title")>
        Public Property Title As String

        <JsonProperty("duration")>
        Public Property Duration As Integer

        <JsonProperty("permalink_url")>
        Public Property Url As String

        <JsonProperty("tracks")>
        Public Property Tracks As List(Of SoundcloudTrack)

        <JsonProperty("kind")>
        Public Property Kind As String

        <JsonProperty("type")>
        Public Property Type As String

        <JsonProperty("artwork_url")>
        Public Property ArtworkUrl As Object

        <JsonProperty("user")>
        Public Property Creator As SoundcloudUser

        <JsonProperty("errors")>
        Public Property Errors As List(Of String)
    End Class



End Namespace