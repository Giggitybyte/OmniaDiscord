Namespace Entities.Database

    Public Class GuildData

        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild the data belongs to.

        Public Property Cases As Dictionary(Of Integer, GuildCase)
        Public Property MutedMembers As List(Of ULong) ' Members who are not allowed to speak. 
        Public Property StaffTitles As Dictionary(Of GuildTitle, List(Of ULong)) ' Collection of users with staff titles.
        Public Property DiscJockeys As List(Of ULong) ' Collection of users allowed to queue music.

        Sub New()
            _MutedMembers = New List(Of ULong)
            _DiscJockeys = New List(Of ULong)
            _StaffTitles = New Dictionary(Of GuildTitle, List(Of ULong)) From {
                {GuildTitle.Admin, New List(Of ULong)()},
                {GuildTitle.Moderator, New List(Of ULong)()},
                {GuildTitle.Helper, New List(Of ULong)()}
            }
        End Sub

    End Class

End Namespace