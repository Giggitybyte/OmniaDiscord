Imports DSharpPlus
Imports DSharpPlus.EventArgs

Namespace Services
    Public Class GuildLogService
        Private _db As DatabaseService
        Private _client As DiscordShardedClient

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            _client = client
            _db = db

            AddHandler _client.GuildBanAdded, AddressOf LogMemberBan
            AddHandler _client.GuildBanRemoved, AddressOf LogUserUnban
            AddHandler _client.GuildMemberAdded, AddressOf LogMemberJoin
            AddHandler _client.GuildMemberRemoved, AddressOf LogMemberLeave
            AddHandler _client.MessageDeleted, AddressOf LogMessageDeletion
            AddHandler _client.MessageUpdated, AddressOf LogMessageEdit
            AddHandler _client.GuildMemberUpdated, AddressOf LogMemberChange
            AddHandler _client.VoiceStateUpdated, AddressOf LogVoiceChange
        End Sub

        Private Function LogMemberJoin(arg As GuildMemberAddEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Private Function LogMemberLeave(arg As GuildMemberRemoveEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Private Function LogMessageDeletion(arg As MessageDeleteEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Private Function LogMessageEdit(arg As MessageUpdateEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Private Function LogMemberChange(arg As GuildMemberUpdateEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Private Function LogVoiceChange(arg As VoiceStateUpdateEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Public Sub LogMemberKick(targetUser As ULong, responsibleUser As ULong, reason As String)
            Throw New NotImplementedException
        End Sub

        Private Function LogMemberBan(e As GuildBanAddEventArgs) As Task
            ' FIGURE OUT THE BEST WAY TO TELL COMMAND BANS AND UI BANS APART
            Throw New NotImplementedException
        End Function

        Public Sub LogMemberBan()
            ' FIGURE OUT THE BEST WAY TO TELL COMMAND BANS AND UI BANS APART
            Throw New NotImplementedException
        End Sub

        Private Function LogUserUnban(e As GuildBanRemoveEventArgs) As Task
            Throw New NotImplementedException
        End Function

        Public Sub LogUserUnban()
            Throw New NotImplementedException
        End Sub
    End Class
End Namespace