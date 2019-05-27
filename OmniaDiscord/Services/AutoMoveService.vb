Imports DSharpPlus
Imports DSharpPlus.EventArgs

Namespace Services
    Public Class AutoMoveService
        Private _db As DatabaseService

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            _db = db
            AddHandler client.VoiceStateUpdated, AddressOf AutoMoveSystem
        End Sub

        Private Function AutoMoveSystem(e As VoiceStateUpdateEventArgs) As Task
            Return Task.CompletedTask ' TODO
        End Function
    End Class
End Namespace
