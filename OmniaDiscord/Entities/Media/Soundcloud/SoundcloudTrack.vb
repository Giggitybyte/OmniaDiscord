Imports System.Net
Imports Newtonsoft.Json

Namespace Entities.Media.Soundcloud

    Public Class SoundcloudTrack

        <JsonProperty("kind")>
        Public Property Kind As String

        <JsonProperty("duration")>
        Public Property Duration As Integer

        <JsonProperty("title")>
        Public Property Title As String

        <JsonProperty("user")>
        Public Property Uploader As SoundcloudUser

        <JsonProperty("permalink_url")>
        Public Property Url As String

        <JsonProperty("stream_url")>
        Private Property StreamUrl As String

        <JsonProperty("artwork_url")>
        Public Property ArtworkUrl As String

        <JsonProperty("errors")>
        Public Property Errors As List(Of String)

        Public Function GetDownloadUrl() As String
            Return $"{StreamUrl}?client_id={Bot.Config.SoundcloudClientId}"
        End Function

    End Class

End Namespace