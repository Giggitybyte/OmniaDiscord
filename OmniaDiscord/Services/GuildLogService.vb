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

        Public Sub LogMemberKick(targetUser As ULong, responsibleUser As ULong, reason As String)

        End Sub

        Private Function LogMemberBan(e As GuildBanAddEventArgs) As Task
            ' FIGURE OUT THE BEST WAY TO TELL COMMAND BANS AND UI BANS APART
        End Function

        Public Sub LogMemberBan()
            ' FIGURE OUT THE BEST WAY TO TELL COMMAND BANS AND UI BANS APART
        End Sub

        Private Function LogUserUnban(e As GuildBanRemoveEventArgs) As Task

        End Function

        Public Sub LogUserUnban()

        End Sub

        Private Function LogMemberJoin(arg As GuildMemberAddEventArgs) As Task

        End Function

        Private Function LogMemberLeave(arg As GuildMemberRemoveEventArgs) As Task

        End Function

        Private Function LogMessageDeletion(arg As MessageDeleteEventArgs) As Task

        End Function

        Private Function LogMessageEdit(arg As MessageUpdateEventArgs) As Task

        End Function

        Private Function LogMemberChange(arg As GuildMemberUpdateEventArgs) As Task

        End Function

        Private Function LogVoiceChange(arg As VoiceStateUpdateEventArgs) As Task

        End Function
    End Class
End Namespace