Namespace Entities.Database

    Public Class GuildData

        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild the data belongs to.

        Public Property MutedMembers As New List(Of ULong) ' Members who are not allowed to speak. 
        Public Property MemberWarnings As New Dictionary(Of ULong, Integer) ' Members with warnings.
        Public Property DiscJockeys As New List(Of ULong) ' Collection of users allowed to queue music.
        Public Property StaffTitles As New Dictionary(Of ULong, GuildTitle) ' Collection of users with staff titles.
        Public Property LobbyChannels As New List(Of ULong) ' Collection of voice channels that will be treated as lobby channels.
        Public Property GameChannels As New List(Of ULong) ' Collection of voice channels that will be used in the auto move system.

    End Class

End Namespace