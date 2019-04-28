Imports Newtonsoft.Json

Namespace Entities.Gamestats
    Public Class SiegeSearchResult
        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("platform")>
        Public Property Platform As String

        <JsonProperty("ubisoft_id")>
        Public Property UbisoftId As String

        <JsonProperty("uplay_id")>
        Public Property UplayId As String
    End Class
End Namespace