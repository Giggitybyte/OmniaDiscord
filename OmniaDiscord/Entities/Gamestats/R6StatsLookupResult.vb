Imports Newtonsoft.Json

Namespace Entities.Gamestats
    <JsonConverter(GetType(JsonPathConverter))>
    Public Class R6StatsLookupResult
        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("platform")>
        Public Property Platform As String

        <JsonProperty("ubisoft_id")>
        Public Property UbisoftId As String

        <JsonProperty("uplay_id")>
        Public Property UplayId As String

        <JsonProperty("progressionStats.level")>
        Public Property Level As Integer
    End Class
End Namespace