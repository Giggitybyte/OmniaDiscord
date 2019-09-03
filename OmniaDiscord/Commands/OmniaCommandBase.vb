Imports DSharpPlus.CommandsNext
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Entities.Database
Imports OmniaDiscord.Services

Namespace Commands
    Public MustInherit Class OmniaCommandBase
        Inherits BaseCommandModule

        Private _originalGuildEntry As GuildEntry
        Public ReadOnly Property DbGuild As GuildEntry
        Public ReadOnly Property Database As DatabaseService

        Public Overrides Function BeforeExecutionAsync(ctx As CommandContext) As Task
            _Database = ctx.Client.GetCommandsNext.Services.GetRequiredService(Of DatabaseService)
            _originalGuildEntry = _Database.GetGuildEntry(ctx.Guild.Id)
            _DbGuild = _Database.GetGuildEntry(ctx.Guild.Id)

            Return MyBase.BeforeExecutionAsync(ctx)
        End Function

        Public Overrides Function AfterExecutionAsync(ctx As CommandContext) As Task
            If Not _DbGuild.Equals(_originalGuildEntry) Then _Database.UpdateGuildEntry(_DbGuild)
            Return MyBase.AfterExecutionAsync(ctx)
        End Function
    End Class
End Namespace