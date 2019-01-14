Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes

Namespace Commands.Checks

    <AttributeUsage(AttributeTargets.Class Or AttributeTargets.Method, AllowMultiple:=False, Inherited:=False)>
    Public Class RequireGuildOwnerAttribute
        Inherits CheckBaseAttribute

        Public Overrides Function ExecuteCheckAsync(ctx As CommandContext, help As Boolean) As Task(Of Boolean)
            If ctx.Guild.Owner.Id = ctx.Member.Id Then
                Return Task.FromResult(True)
            Else
                Return Task.FromResult(False)
            End If
        End Function

    End Class

End Namespace