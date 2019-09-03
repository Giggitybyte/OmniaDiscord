Imports OmniaDiscord.Entities.Media

Namespace Entities.Database
    Public Class GuildData
        Public Property MutedRoleId As ULong ' The role to be used for muted users.

        Public Property MemberWarnings As New Dictionary(Of ULong, Integer) ' Members with warnings.
        Public Property TitleHolders As New Dictionary(Of ULong, GuildTitle) ' Collection of users with staff titles.

        Public Property BlacklistedChannels As New List(Of ULong) ' Collection of channels that Omnia cannot interact with.
        Public Property MutedMembers As New List(Of ULong) ' Members who are not allowed to speak. 
        Public Property DiscJockeys As New List(Of ULong) ' Collection of users allowed to queue music.
        Public Property LobbyChannels As New List(Of ULong) ' Collection of voice channels that will be treated as lobby channels.
        Public Property UserPlaylists As New List(Of OmniaUserPlaylist) ' Collection of user created playlists.
    End Class
End Namespace