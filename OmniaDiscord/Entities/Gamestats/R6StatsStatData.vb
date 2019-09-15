Imports Newtonsoft.Json

Namespace Entities.Gamestats

    Public Class R6StatsStatData

        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("platform")>
        Public Property Platform As String

        <JsonProperty("ubisoft_id")>
        Public Property UbisoftId As String

        <JsonProperty("uplay_id")>
        Public Property UplayId As String

        <JsonProperty("aliases")>
        Public Property Aliases As PlayerAlias()

        <JsonProperty("last_updated")>
        Public Property LastUpdated As Date

        <JsonProperty("progression")>
        Public Property Progression As PlayerProgression

        <JsonProperty("stats")>
        Public Property Stats As PlayerStats()

        <JsonProperty("operators")>
        Public Property Operators As OperatorStat()

        Public Class PlayerAlias

            <JsonProperty("username")>
            Public Property Username As String

            <JsonProperty("last_seen_at")>
            Public Property LastSeenAt As Date

        End Class

        Public Class PlayerProgression

            <JsonProperty("level")>
            Public Property Level As Integer

            <JsonProperty("lootbox_probability")>
            Public Property LootboxProbability As Integer

            <JsonProperty("total_xp")>
            Public Property TotalXp As Integer
        End Class

        Public Class GeneralStats

            <JsonProperty("assists")>
            Public Property Assists As Integer

            <JsonProperty("barricades_deployed")>
            Public Property BarricadesDeployed As Integer

            <JsonProperty("blind_kills")>
            Public Property BlindKills As Integer

            <JsonProperty("bullets_fired")>
            Public Property BulletsFired As Integer

            <JsonProperty("bullets_hit")>
            Public Property BulletsHit As Integer

            <JsonProperty("dbnos")>
            Public Property Dbnos As Integer

            <JsonProperty("deaths")>
            Public Property Deaths As Integer

            <JsonProperty("distance_travelled")>
            Public Property DistanceTraveled As Long

            <JsonProperty("draws")>
            Public Property Draws As Integer

            <JsonProperty("gadgets_destroyed")>
            Public Property GadgetsDestroyed As Integer

            <JsonProperty("games_played")>
            Public Property GamesPlayed As Integer

            <JsonProperty("headshots")>
            Public Property Headshots As Integer

            <JsonProperty("kd")>
            Public Property KdRatio As Double

            <JsonProperty("kills")>
            Public Property Kills As Integer

            <JsonProperty("losses")>
            Public Property Losses As Integer

            <JsonProperty("melee_kills")>
            Public Property MeleeKills As Integer

            <JsonProperty("penetration_kills")>
            Public Property PenetrationKills As Integer

            <JsonProperty("playtime")>
            Public Property Playtime As Integer

            <JsonProperty("rappel_breaches")>
            Public Property RappelBreaches As Integer

            <JsonProperty("reinforcements_deployed")>
            Public Property ReinforcementsDeployed As Integer

            <JsonProperty("revives")>
            Public Property Revives As Integer

            <JsonProperty("suicides")>
            Public Property Suicides As Integer

            <JsonProperty("wins")>
            Public Property Wins As Integer

            <JsonProperty("wl")>
            Public Property WlRatio As Double
        End Class

        Public Class PlaylistStats

            <JsonProperty("deaths")>
            Public Property Deaths As Integer

            <JsonProperty("draws")>
            Public Property Draws As Integer

            <JsonProperty("games_played")>
            Public Property GamesPlayed As Integer

            <JsonProperty("kd")>
            Public Property KdRatio As Double

            <JsonProperty("kills")>
            Public Property Kills As Integer

            <JsonProperty("losses")>
            Public Property Losses As Integer

            <JsonProperty("playtime")>
            Public Property Playtime As Integer

            <JsonProperty("wins")>
            Public Property Wins As Integer

            <JsonProperty("wl")>
            Public Property WlRatio As Double
        End Class

        Public Class Playlist

            <JsonProperty("casual")>
            Public Property Casual As PlaylistStats

            <JsonProperty("ranked")>
            Public Property Ranked As PlaylistStats

            <JsonProperty("other")>
            Public Property Other As PlaylistStats
        End Class

        Public Class Bomb
            <JsonProperty("best_score")>
            Public Property BestScore As Integer

            <JsonProperty("games_played")>
            Public Property GamesPlayed As Integer

            <JsonProperty("losses")>
            Public Property Losses As Integer

            <JsonProperty("playtime")>
            Public Property Playtime As Integer

            <JsonProperty("wins")>
            Public Property Wins As Integer

            <JsonProperty("wl")>
            Public Property WlRatio As Double
        End Class

        Public Class SecureArea
            Inherits Bomb

            <JsonProperty("kills_as_attacker_in_objective")>
            Public Property KillsAsAttackerInObjective As Integer

            <JsonProperty("kills_as_defender_in_objective")>
            Public Property KillsAsDefenderInObjective As Integer

            <JsonProperty("times_objective_secured")>
            Public Property TimesObjectiveSecured As Integer
        End Class

        Public Class Hostage
            Inherits Bomb

            <JsonProperty("extractions_denied")>
            Public Property ExtractionsDenied As Integer
        End Class

        Public Class Gamemode
            <JsonProperty("bomb")>
            Public Property Bomb As Bomb

            <JsonProperty("secure_area")>
            Public Property SecureArea As SecureArea

            <JsonProperty("hostage")>
            Public Property Hostage As Hostage
        End Class

        Public Class Timestamps
            <JsonProperty("created")>
            Public Property Created As Date

            <JsonProperty("last_updated")>
            Public Property LastUpdated As Date
        End Class

        Public Class PlayerStats
            <JsonProperty("general")>
            Public Property General As GeneralStats

            <JsonProperty("queue")>
            Public Property Playlist As Playlist

            <JsonProperty("gamemode")>
            Public Property Gamemode As Gamemode

            <JsonProperty("timestamps")>
            Public Property Timestamps As Timestamps
        End Class

        Public Class Ability
            <JsonProperty("key")>
            Public Property Key As String

            <JsonProperty("title")>
            Public Property Title As String

            <JsonProperty("value")>
            Public Property Value As Integer
        End Class

        Public Class OperatorStat
            <JsonProperty("kills")>
            Public Property Kills As Integer

            <JsonProperty("deaths")>
            Public Property Deaths As Integer

            <JsonProperty("kd")>
            Public Property KdRatio As Double

            <JsonProperty("wins")>
            Public Property Wins As Integer

            <JsonProperty("losses")>
            Public Property Losses As Integer

            <JsonProperty("wl")>
            Public Property WlRatio As Double

            <JsonProperty("headshots")>
            Public Property Headshots As Integer

            <JsonProperty("dbnos")>
            Public Property Dbnos As Integer

            <JsonProperty("melee_kills")>
            Public Property MeleeKills As Integer

            <JsonProperty("experience")>
            Public Property Experience As Integer

            <JsonProperty("playtime")>
            Public Property Playtime As Integer

            <JsonProperty("abilities")>
            Public Property Abilities As Ability()

            <JsonProperty("operator")>
            Public Property [Operator] As [Operator]
        End Class

        Public Class [Operator]
            <JsonProperty("name")>
            Public Property Name As String

            <JsonProperty("internal_name")>
            Public Property InternalName As String

            <JsonProperty("role")>
            Public Property Role As String

            <JsonProperty("ctu")>
            Public Property Ctu As String

            <JsonProperty("images")>
            Public Property Images As Images
        End Class

        Public Class Images
            <JsonProperty("badge")>
            Public Property Badge As String

            <JsonProperty("bust")>
            Public Property Bust As String

            <JsonProperty("figure")>
            Public Property Figure As String
        End Class
    End Class
End Namespace