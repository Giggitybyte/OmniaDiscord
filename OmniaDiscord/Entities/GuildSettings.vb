﻿Namespace Entites

    Public Class GuildSettings

        Public Property Id As Integer ' Database ID.
        Public Property GuildId As ULong ' The guild in which these settings belong to.

        Public Property Prefix As String ' The prefix currently set for this guild.
        Public Property IsDjWhitelistEnabled As Boolean ' Whether or not music commands are restricted.
        Public Property IsVoteToSkipEnabled As Boolean ' Weather or not a vote is needed to skip an audio track.

    End Class

End Namespace