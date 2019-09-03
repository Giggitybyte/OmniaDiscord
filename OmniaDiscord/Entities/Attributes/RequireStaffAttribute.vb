Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Entities.Database
Imports OmniaDiscord.Services

Namespace Entities.Attributes

    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=False)>
    Public NotInheritable Class RequireStaffAttribute
        Inherits CheckBaseAttribute

        Public Overrides Function ExecuteCheckAsync(ctx As CommandContext, help As Boolean) As Task(Of Boolean)
            If ctx.Guild.Owner.Id = ctx.Member.Id Then Return Task.FromResult(True)
            Dim data As GuildData = ctx.Services.GetService(Of DatabaseService).GetGuildEntry(ctx.Guild.Id).Data
            Return Task.FromResult(data.TitleHolders.ContainsKey(ctx.Member.Id))
        End Function

    End Class

End Namespace