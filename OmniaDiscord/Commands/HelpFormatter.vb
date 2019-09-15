Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.CommandsNext.Entities

Namespace Commands
    Public Class HelpFormatter
        Inherits BaseHelpFormatter

        Private _isCommand As Boolean
        Private _prefix As String
        Private ReadOnly _strBuilder As New StringBuilder

        Public Sub New(ctx As CommandContext)
            MyBase.New(ctx)
            _prefix = ctx.Prefix
        End Sub

        Public Overrides Function WithCommand(cmd As Command) As BaseHelpFormatter
            _isCommand = True

            _strBuilder.AppendLine(cmd.QualifiedName)
            _strBuilder.AppendLine(New String("-", cmd.QualifiedName.Count))
            _strBuilder.AppendLine(If(cmd.Description, "The description for this command was not set."))

            Dim aliases = GetCommandAliases(cmd)
            If aliases.Any Then _strBuilder.AppendLine.AppendLine("aliases::").AppendLine(String.Join(", ", aliases))

            Dim usages As New StringBuilder
            usages.Append(GetCommandUsage(cmd))

            If usages.Length > 0 Then
                _strBuilder.AppendLine.AppendLine("usage::").Append(usages.ToString)
            End If

            _strBuilder.AppendLine.AppendLine("// [arg] = required • (arg) = optional")

            Return Me
        End Function

        Public Overrides Function WithSubcommands(childCommands As IEnumerable(Of Command)) As BaseHelpFormatter
            If Not _isCommand Then
                _strBuilder.Append(String.Join($", ", childCommands.Where(Function(s) s.Name.ToLower <> "help").Select(Function(cmd) $"{cmd.Name}")))
            End If

            Return Me
        End Function

        Public Overrides Function Build() As CommandHelpMessage
            Dim message = _strBuilder.ToString

            If Not _isCommand Then
                Dim builder As New StringBuilder
                builder.AppendLine("available commands")
                builder.AppendLine(New String("-", 18))
                builder.AppendLine(message)
                builder.AppendLine().AppendLine("// specify a command to see its usage")

                message = builder.ToString
            End If

            Return New CommandHelpMessage(Formatter.BlockCode(message, "asciidoc"))
        End Function

        Private Function GetCommandAliases(cmd As Command) As IEnumerable(Of String)
            If Not cmd.Aliases.Any Then Return New List(Of String)
            If cmd.Parent IsNot Nothing Then Return cmd.Aliases.Select(Function(a) $"{cmd.Parent.QualifiedName} {a}")
            Return cmd.Aliases
        End Function

        Private Function GetCommandUsage(cmd As Command) As String
            Dim usageBuilder As New StringBuilder
            For Each overload In cmd.Overloads.OrderBy(Function(x) x.Priority)
                If Not overload.Arguments.Any Then
                    usageBuilder.AppendLine($"{_prefix}{cmd.QualifiedName}")
                    Continue For
                End If

                usageBuilder.Append($"{_prefix}{cmd.QualifiedName} ")
                For Each arg As CommandArgument In overload.Arguments
                    usageBuilder.Append($"{If(arg.IsOptional, "(", "[")}{arg.Name}{If(arg.IsOptional, ")", "]")} ")
                Next
                usageBuilder.AppendLine()
            Next

            If TypeOf cmd Is CommandGroup Then
                For Each childCmd In DirectCast(cmd, CommandGroup).Children
                    usageBuilder.Append(GetCommandUsage(childCmd))
                Next
            End If

            Return usageBuilder.ToString
        End Function
    End Class
End Namespace