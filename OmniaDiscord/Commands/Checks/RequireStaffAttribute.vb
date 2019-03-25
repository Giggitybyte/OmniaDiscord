Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Entites
Imports OmniaDiscord.Services

Namespace Commands.Checks

    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=False)>
    Public NotInheritable Class RequireStaffAttribute
        Inherits CheckBaseAttribute

        Public Overrides Function ExecuteCheckAsync(ctx As CommandContext, help As Boolean) As Task(Of Boolean)
            ' Check if user is the guild owner
            If ctx.Guild.Owner.Id = ctx.Member.Id Then Return Task.FromResult(True)

            Dim data As GuildData = ctx.Services.GetService(Of DatabaseService).GetGuildData(ctx.Guild.Id)

            ' Check if user has a title.
            If data.StaffTitles(GuildTitle.Admin).Contains(ctx.Member.Id) Or
               data.StaffTitles(GuildTitle.Moderator).Contains(ctx.Member.Id) Or
               data.StaffTitles(GuildTitle.Helper).Contains(ctx.Member.Id) Then Return Task.FromResult(True)


            Return Task.FromResult(False)
        End Function

    End Class

End Namespace