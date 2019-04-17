Imports DSharpPlus

Namespace Services
    Public Class GuildLogService
        Private _db As DatabaseService
        Private _client As DiscordShardedClient

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            _client = client
            _db = db

            'AddHandler _client.

        End Sub



    End Class
End Namespace