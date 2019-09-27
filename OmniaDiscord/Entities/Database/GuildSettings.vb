Imports OmniaDiscord.Entities.Attributes

Namespace Entities.Database
    Public Class GuildSettings
        <GuildSetting("Custom Prefix", "prefix", GetType(String), GuildTitle.Admin)>
        Public Property Prefix As String ' The prefix currently set for this guild.

        <GuildSetting("Vote to Skip", "votetoskip", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsVoteToSkipEnabled As Boolean ' Whether or not a vote is needed to skip an audio track.

        <GuildSetting("Auto Move System", "automove", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsAutoMoveEnabled As Boolean ' Whether or not auto move functionality is enabled for this guild.

        <GuildSetting("Lobby System", "lobbysystem", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsLobbySystemEnabled As Boolean ' Whether or not the lobby system is enabled for this guild.

        <GuildSetting("Channel Blacklist", "blacklist", GetType(Boolean), GuildTitle.Moderator)>
        Public Property IsChannelBlacklistEnabled As Boolean 'Whether or not the channel blacklist is enabled for this guild.
    End Class
End Namespace