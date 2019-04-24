Imports Newtonsoft.Json

Namespace Entities.Gamestats

    Public Class SiegePlayer

        <JsonProperty("profile_id")>
        Public Property UbisoftId As String

        <JsonProperty("nickname")>
        Public Property Username As String

        <JsonProperty("level")>
        Public Property ClearanceLevel As Integer

        <JsonProperty("platform")>
        Public Property Platform As String

        <JsonProperty("region")>
        Public Property Region As String

        <JsonProperty("lootbox_probability")>
        Public Property LootboxChance As Integer

        <JsonProperty("update_time")>
        Public Property LastUpdate As Date

        Public Property RankedCurrent As RankedCurrent
        Public Property RankedOverall As RankedOverall
        Public Property CasualOverall As CasualOverall
        Public Property GeneralStats As GeneralStats
    End Class

    Public Structure RankedCurrent

        <JsonProperty("season")>
        Public Property Season As Integer

        <JsonProperty("max_mmr")>
        Public Property HighestMmr As Double

        <JsonProperty("mmr")>
        Public Property CurrentMmr As Double

        <JsonProperty("next_rank_mmr")>
        Public Property NextRankMmr As Integer

        <JsonProperty("wins")>
        Public Property Wins As Integer

        <JsonProperty("losses")>
        Public Property Losses As Integer

        <JsonProperty("abandons")>
        Public Property Abandons As Integer

        <JsonProperty("rankInfo")>
        Public Property Resources As RankResources

        Structure RankResources

            <JsonProperty("image")>
            Public Property Image As String

            <JsonProperty("name")>
            Public Property RankName As String

        End Structure

    End Structure

    Public Structure RankedOverall

        <JsonProperty("rankedpvp_matchwon")>
        Public Property Wins As Integer

        <JsonProperty("rankedpvp_matchlost")>
        Public Property Losses As Integer

        <JsonProperty("rankedpvp_kills")>
        Public Property Kills As Integer

        <JsonProperty("rankedpvp_death")>
        Public Property Deaths As Integer

        <JsonProperty("rankedpvp_timeplayed")>
        Public Property Playtime As Integer

    End Structure

    Public Structure CasualOverall

        <JsonProperty("casualpvp_matchwon")>
        Public Property Wins As Integer

        <JsonProperty("casualpvp_matchlost")>
        Public Property Losses As Integer

        <JsonProperty("casualpvp_kills")>
        Public Property Kills As Integer

        <JsonProperty("casualpvp_death")>
        Public Property Deaths As Integer

        <JsonProperty("casualpvp_timeplayed")>
        Public Property Playtime As Integer

    End Structure

    Public Structure GeneralStats

        <JsonProperty("generalpvp_bulletfired")>
        Public Property ShotsFired As ULong

        <JsonProperty("generalpvp_bullethit")>
        Public Property ShotsHit As ULong

        <JsonProperty("generalpvp_headshot")>
        Public Property Headshots As Integer

        <JsonProperty("generalpvp_penetrationkills")>
        Public Property PenetrationKills As Integer

        <JsonProperty("generalpvp_meleekills")>
        Public Property MeleeKills As Integer

        <JsonProperty("generalpvp_killassists")>
        Public Property KillAssists As Integer

        <JsonProperty("generalpvp_revive")>
        Public Property Revives As Integer

        <JsonProperty("generalpvp_suicide")>
        Public Property Suicides As Integer

        <JsonProperty("secureareapvp_matchplayed")>
        Public Property SecureAreaMatches As Integer

        <JsonProperty("rescuehostagepvp_matchplayed")>
        Public Property HostageMatches As Integer

        <JsonProperty("plantbombpvp_matchplayed")>
        Public Property BombMatches As Integer

    End Structure

End Namespace