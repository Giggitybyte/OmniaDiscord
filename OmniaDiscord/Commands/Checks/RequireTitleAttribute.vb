Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Services
Imports OmniaDiscord.Services.Entities.Database

Namespace Commands.Checks

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
            ' Check if user is the guild owner
            If ctx.Guild.Owner.Id = ctx.Member.Id Then Return Task.FromResult(True)

            Dim data As GuildData = ctx.Services.GetRequiredService(Of DatabaseService).GetGuildData(ctx.Guild.Id)
            Dim validTitles As New List(Of GuildTitle)

            ' Get the minimum title and all titles above it.
            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle)).Cast(Of GuildTitle)
                If title >= _MinimumTitle Then validTitles.Add(title)
            Next

            ' See if the user has any valid titles.
            For Each title As GuildTitle In validTitles
                For Each userId As ULong In data.StaffTitles(title)
                    If ctx.Member.Id = userId Then Return Task.FromResult(True)
                Next
            Next

            Return Task.FromResult(False)
        End Function

    End Class

End Namespace