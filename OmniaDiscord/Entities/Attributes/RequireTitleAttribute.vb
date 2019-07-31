Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Entities.Database
Imports OmniaDiscord.Services

Namespace Entities.Attributes

    ''' <summary>
    ''' Defines the minimum title required to run this command.
    ''' </summary>
    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=False)>
    Public NotInheritable Class RequireTitleAttribute
        Inherits CheckBaseAttribute

        Public ReadOnly Property MinimumTitle As GuildTitle

        Sub New(title As GuildTitle)
            _MinimumTitle = title
        End Sub

        Public Overrides Function ExecuteCheckAsync(ctx As CommandContext, help As Boolean) As Task(Of Boolean)
            If ctx.Guild.Owner.Id = ctx.Member.Id Then Return Task.FromResult(True)

            Dim data As GuildData = ctx.Services.GetRequiredService(Of DatabaseService).GetGuildData(ctx.Guild.Id)
            If Not data.StaffTitles.ContainsKey(ctx.Member.Id) Then Return Task.FromResult(False)

            Dim validTitles As New List(Of GuildTitle)
            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle)).Cast(Of GuildTitle)
                If title >= _MinimumTitle Then validTitles.Add(title)
            Next

            Return Task.FromResult(validTitles.Contains(data.StaffTitles(ctx.Member.Id)))
        End Function

    End Class

End Namespace