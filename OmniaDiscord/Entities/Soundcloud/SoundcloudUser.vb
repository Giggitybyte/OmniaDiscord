Imports Newtonsoft.Json

Namespace Entites.Soundcloud
    Public Class SoundcloudUser

        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("permalink_url")>
        Public Property ProfileUrl As String

        <JsonProperty("avatar_url")>
        Public Property AvatarUrl As String

    End Class

End Namespace