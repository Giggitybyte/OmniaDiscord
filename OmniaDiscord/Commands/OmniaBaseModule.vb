Imports DSharpPlus.CommandsNext
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Bot
Imports OmniaDiscord.Services
Imports OmniaDiscord.Services.Entities.Database

Namespace Commands
    Public MustInherit Class OmniaBaseModule
        Inherits BaseCommandModule

        Public ReadOnly Property Utilities As Utilities
        Public ReadOnly Property OmniaConfig As Configuration
        Public Property GuildData As GuildData
        Public Property GuildSettings As GuildSettings
        Private _db As DatabaseService

        Public Overrides Function BeforeExecutionAsync(ctx As CommandContext) As Task
            _db = ctx.Client.GetCommandsNext.Services.GetRequiredService(Of DatabaseService)

            _GuildData = _db.GetGuildData(ctx.Guild.Id)
            _GuildSettings = _db.GetGuildSettings(ctx.Guild.Id)
            _Utilities = ctx.Client.GetCommandsNext.Services.GetService(Of Utilities)
            _OmniaConfig = ctx.Client.GetCommandsNext.Services.GetRequiredService(Of Configuration)

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