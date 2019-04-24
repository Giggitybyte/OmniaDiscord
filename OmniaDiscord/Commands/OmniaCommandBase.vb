Imports DSharpPlus.CommandsNext
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Entites.Database
Imports OmniaDiscord.Services

Namespace Commands
    Public MustInherit Class OmniaCommandBase
        Inherits BaseCommandModule

        Public ReadOnly Property OmniaConfig As Bot.Configuration
        Public Property GuildData As GuildData
        Public Property GuildSettings As GuildSettings
        Private _db As DatabaseService

        Public Overrides Function BeforeExecutionAsync(ctx As CommandContext) As Task
            _db = ctx.Client.GetCommandsNext.Services.GetRequiredService(Of DatabaseService)

            _GuildData = _db.GetGuildData(ctx.Guild.Id)
            _GuildSettings = _db.GetGuildSettings(ctx.Guild.Id)
            _OmniaConfig = ctx.Client.GetCommandsNext.Services.GetRequiredService(Of Bot.Configuration)

            Return MyBase.BeforeExecutionAsync(ctx)
        End Function

        Public Sub UpdateGuildData()
            _db.UpdateGuildData(_GuildData)
        End Sub

        Public Sub UpdateGuildSettings()
            _db.UpdateGuildSettings(_GuildSettings)
        End Sub
    End Class
End Namespace