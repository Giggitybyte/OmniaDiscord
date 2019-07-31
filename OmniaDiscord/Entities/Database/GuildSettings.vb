Imports OmniaDiscord.Entities.Attributes

Namespace Entities.Database

    ''' <summary>
    ''' Variables that users can modify.
    ''' </summary>
    Public Class GuildSettings

        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild these settings belong to.

        <GuildSetting("Custom Prefix", "prefix", GetType(String), GuildTitle.Admin)>
        Public Property Prefix As String ' The prefix currently set for this guild.

        <GuildSetting("DJ Whitelist Enabled", "djwhitelist", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsDjWhitelistEnabled As Boolean ' Whether or not music commands are restricted to whitelisted users.

        <GuildSetting("Vote to Skip Enabled", "votetoskip", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsVoteToSkipEnabled As Boolean ' Whether or not a vote is needed to skip an audio track.

        <GuildSetting("Auto Move Enabled", "automove", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsAutoMoveEnabled As Boolean ' Whether or not auto move functionality is enabled for this guild.

        <GuildSetting("Lobby System Enabled", "lobbysystem", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsLobbySystemEnabled As Boolean ' Whether or not the lobby system is enabled for this guild.
    End Class

End Namespace