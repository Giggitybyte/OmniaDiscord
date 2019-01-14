Namespace Services.Entities.Database
    Public Class GuildData

        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild in which the data belongs to.

        Public Property StaffTitles As Dictionary(Of GuildTitle, List(Of ULong)) ' Collection of users with staff titles
        Public Property DiscJockeys As List(Of ULong) ' Collection of users allowed to queue music.

        Sub New()
            _DiscJockeys = New List(Of ULong)
            _StaffTitles = New Dictionary(Of GuildTitle, List(Of ULong)) From {
                {GuildTitle.ADMIN, New List(Of ULong)()},
                {GuildTitle.MODERATOR, New List(Of ULong)()},
                {GuildTitle.HELPER, New List(Of ULong)()}
            }
        End Sub

    End Class
End Namespace