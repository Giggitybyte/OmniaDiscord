Imports Newtonsoft.Json

Namespace Entities.Gamestats

    Public Class SiegeSeasons
        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("platform")>
        Public Property Platform As String

        <JsonProperty("ubisoft_id")>
        Public Property UbisoftId As String

        <JsonProperty("uplay_id")>
        Public Property UplayId As String

        <JsonProperty("last_updated")>
        Public Property LastUpdated As Date

        <JsonProperty("seasons")>
        Public Property Seasons As Dictionary(Of String, SeasonInfo)
    End Class

    Public Class SeasonInfo
        <JsonProperty("id")>
        Public Property Id As Integer

        <JsonProperty("name")>
        Public Property Name As String

        <JsonProperty("key")>
        Public Property Key As String

        <JsonProperty("start_date")>
        Public Property StartDate As Date

        <JsonProperty("end_date")>
        Public Property EndDate As Date

        <JsonProperty("rankings")>
        Public Property Rankings As Dictionary(Of String, Integer?)

        <JsonProperty("regions")>
        Public Property Regions As Dictionary(Of String, List(Of RegionRanking))
    End Class

    Public Class RegionRanking

        <JsonProperty("id")>
        Public Property Id As Integer

        <JsonProperty("season_id")>
        Public Property SeasonId As Integer

        <JsonProperty("region")>
        Public Property Region As String

        <JsonProperty("abandons")>
        Public Property Abandons As Integer

        <JsonProperty("losses")>
        Public Property Losses As Integer

        <JsonProperty("max_mmr")>
        Public Property MaxMmr As Double

        <JsonProperty("max_rank")>
        Public Property MaxRank As Integer

        <JsonProperty("mmr")>
        Public Property Mmr As Double

        <JsonProperty("next_rank_mmr")>
        Public Property NextRankMmr As Integer

        <JsonProperty("prev_rank_mmr")>
        Public Property PrevRankMmr As Integer

        <JsonProperty("rank")>
        Public Property Rank As Integer

        <JsonProperty("skill_mean")>
        Public Property SkillMean As Double

        <JsonProperty("skill_standard_deviation")>
        Public Property SkillStandardDeviation As Double

        <JsonProperty("created_for_date")>
        Public Property CreatedForDate As Date

        <JsonProperty("wins")>
        Public Property Wins As Integer
    End Class

End Namespace