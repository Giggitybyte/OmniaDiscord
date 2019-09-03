Namespace Entities.Database
    Public Class GuildEntry
        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild this entry belongs to.

        Public Property Settings As New GuildSettings
        Public Property Data As New GuildData
    End Class
End Namespace