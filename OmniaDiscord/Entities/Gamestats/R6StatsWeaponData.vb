Imports Newtonsoft.Json

Namespace Entities.Gamestats
    Public Class R6StatsWeaponData
        <JsonProperty("username")>
        Public Property Username As String

        <JsonProperty("platform")>
        Public Property Platform As String

        <JsonProperty("ubisoft_id")>
        Public Property UbisoftId As String

        <JsonProperty("uplay_id")>
        Public Property UplayId As String

        <JsonProperty("last_updated")>
        Public Property LastUpdated As DateTime

        <JsonProperty("categories")>
        Public Property Categories As CategoryStats()

        <JsonProperty("weapons")>
        Public Property Weapons As WeaponStats()

        Public Class CategoryStats

            <JsonProperty("kills")>
            Public Property Kills As Integer

            <JsonProperty("deaths")>
            Public Property Deaths As Integer

            <JsonProperty("kd")>
            Public Property Kd As Double

            <JsonProperty("headshots")>
            Public Property Headshots As Integer

            <JsonProperty("headshot_percentage")>
            Public Property HeadshotPercentage As Double

            <JsonProperty("times_chosen")>
            Public Property TimesChosen As Integer

            <JsonProperty("bullets_fired")>
            Public Property BulletsFired As Integer

            <JsonProperty("bullets_hit")>
            Public Property BulletsHit As Integer

            <JsonProperty("created")>
            Public Property Created As Date

            <JsonProperty("last_updated")>
            Public Property LastUpdated As Date

            <JsonProperty("category")>
            Public Property Category As Category
        End Class

        Public Class WeaponStats

            <JsonProperty("kills")>
            Public Property Kills As Integer

            <JsonProperty("deaths")>
            Public Property Deaths As Integer

            <JsonProperty("kd")>
            Public Property Kd As Double

            <JsonProperty("headshots")>
            Public Property Headshots As Integer

            <JsonProperty("headshot_percentage")>
            Public Property HeadshotPercentage As Double

            <JsonProperty("times_chosen")>
            Public Property TimesChosen As Integer

            <JsonProperty("bullets_fired")>
            Public Property BulletsFired As Integer

            <JsonProperty("bullets_hit")>
            Public Property BulletsHit As Integer

            <JsonProperty("created")>
            Public Property Created As Date

            <JsonProperty("last_updated")>
            Public Property LastUpdated As Date

            <JsonProperty("weapon")>
            Public Property Weapon As Weapon
        End Class

        Public Class Category

            <JsonProperty("name")>
            Public Property Name As String

            <JsonProperty("internal_name")>
            Public Property InternalName As String
        End Class

        Public Class Weapon

            <JsonProperty("name")>
            Public Property Name As String

            <JsonProperty("internal_name")>
            Public Property InternalName As String

            <JsonProperty("category")>
            Public Property Category As Category
        End Class
    End Class
End Namespace