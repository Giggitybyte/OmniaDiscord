Imports Newtonsoft.Json

Namespace Entities.Gamestats
    <JsonConverter(GetType(JsonPathConverter))>
    Public Class R6TabSeasonalData
        <JsonProperty("seasonal.total_rankedwins")>
        Public Property Wins As Integer

        <JsonProperty("seasonal.total_rankedlosses")>
        Public Property Losses As Integer

        <JsonProperty("seasonal.total_rankedkills")>
        Public Property Kills As Integer

        <JsonProperty("seasonal.total_rankeddeaths")>
        Public Property Deaths As Integer
    End Class
End Namespace
