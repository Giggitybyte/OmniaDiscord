Imports DSharpPlus.CommandsNext
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Entities.Database
Imports OmniaDiscord.Services

Namespace Commands.Bases
    Public MustInherit Class OmniaDbCommandBase
        Inherits BaseCommandModule

        Private _db As DatabaseService
        Private _originaGuildEntry As GuildEntry
        Private _originalUserEntry As UserEntry
        Public ReadOnly Property DbGuild As GuildEntry
        Public ReadOnly Property DbUser As UserEntry

        Public Overrides Function BeforeExecutionAsync(ctx As CommandContext) As Task
            _db = ctx.CommandsNext.Services.GetRequiredService(Of DatabaseService)
            _originaGuildEntry = _db.GetGuildEntry(ctx.Guild.Id)
            _originalUserEntry = _db.GetUserEntry(ctx.User.Id)

            _DbGuild = _db.GetGuildEntry(ctx.Guild.Id)
            _DbUser = _db.GetUserEntry(ctx.User.Id)

            Return MyBase.BeforeExecutionAsync(ctx)
        End Function

        Public Overrides Function AfterExecutionAsync(ctx As CommandContext) As Task
            If Not _DbGuild.Equals(_originaGuildEntry) Then _db.UpdateGuildEntry(_DbGuild)
            If Not _DbUser.Equals(_originalUserEntry) Then _db.UpdateUserEntry(_DbUser)
            Return MyBase.AfterExecutionAsync(ctx)
        End Function
    End Class
End Namespace