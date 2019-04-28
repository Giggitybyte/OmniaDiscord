Imports Newtonsoft.Json

Namespace Entities.Media.Soundcloud
    Public Class SoundcloudUser

        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("permalink_url")>
        Public Property ProfileUrl As String

        <JsonProperty("avatar_url")>
        Public Property AvatarUrl As String

    End Class

End Namespace