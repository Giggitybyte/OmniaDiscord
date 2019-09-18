Imports DSharpPlus.CommandsNext
Imports Lavalink4NET
Imports Lavalink4NET.Player
Imports Microsoft.Extensions.DependencyInjection

Namespace Commands.Bases
    Public Class OmniaMediaCommandBase
        Inherits BaseCommandModule

        Private _lavalink As LavalinkNode
        Private _guildId As ULong
        Public ReadOnly Property Player As VoteLavalinkPlayer

        Public Sub JoinVoiceChannel(channelId As ULong)
            If _Player Is Nothing OrElse _Player.VoiceChannelId Is Nothing Then
                _lavalink.JoinAsync(Of VoteLavalinkPlayer)(_guildId, channelId, True)
            End If
        End Sub

        Public Overrides Function BeforeExecutionAsync(ctx As CommandContext) As Task
            _guildId = ctx.Guild.Id
            _lavalink = ctx.CommandsNext.Services.GetRequiredService(Of IAudioService)
            If _lavalink.HasPlayer(_guildId) Then _Player = _lavalink.GetPlayer(Of VoteLavalinkPlayer)(_guildId)

            Return MyBase.BeforeExecutionAsync(ctx)
        End Function

        Public Overrides Function AfterExecutionAsync(ctx As CommandContext) As Task
            _Player.Dispose()
            Return MyBase.AfterExecutionAsync(ctx)
        End Function
    End Class
End Namespace