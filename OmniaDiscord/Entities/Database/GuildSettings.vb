Namespace Entities.Database

    Public Class GuildSettings

        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild in which these settings belong to.

        Public Property Prefix As String ' The prefix currently set for this guild.
        Public Property LogChannelId As ULong ' The text channel for log output for this guild.

        Public Property IsDjWhitelistEnabled As Boolean ' Whether or not music commands are restricted.
        Public Property IsVoteToSkipEnabled As Boolean ' Whether or not a vote is needed to skip an audio track.
        Public Property IsAutoMoveEnabled As Boolean ' Whether or not auto move functionality is enabled for this guild.
        Public Property IsLobbySystemEnabled As Boolean ' Whether or not the lobby system is enabled for this guild.

    End Class

End Namespace