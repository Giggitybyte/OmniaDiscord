Imports System.Text
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.CommandsNext.Entities
Imports DSharpPlus.Entities

Namespace Commands
    Public Class HelpFormatter
        Inherits BaseHelpFormatter

        Private _isCommand As Boolean
        Private _prefix As String
        Private ReadOnly _embed As New DiscordEmbedBuilder With {.Color = DiscordColor.CornflowerBlue}

        Public Sub New(ctx As CommandContext)
            MyBase.New(ctx)
            _prefix = ctx.Prefix
        End Sub

        Public Overrides Function WithCommand(cmd As Command) As BaseHelpFormatter
            _isCommand = True

            With _embed
                .Title = $"{cmd.QualifiedName}"
                .Description = If(cmd.Description, "Description not set.")

                Dim strBuilder As New StringBuilder
                For Each overload In cmd.Overloads.OrderBy(Function(x) x.Priority)
                    If Not overload.Arguments.Any Then
                        strBuilder.AppendLine($"`{_prefix}{cmd.QualifiedName}`")
                        Continue For
                    End If

                    strBuilder.Append($"`{_prefix}{cmd.QualifiedName} ")
                    For Each arg As CommandArgument In overload.Arguments
                        strBuilder.Append($"{If(arg.IsOptional, "(", "[")}{arg.Name}{If(arg.IsOptional, ")", "]")} ")
                    Next
                    strBuilder.Append($"`{Environment.NewLine}")
                Next
                If strBuilder.Length > 0 Then .AddField("Usage", strBuilder.ToString)

                .AddField("Type", GetCommandType(cmd), True)

                Dim aliases = GetCommandAliases(cmd)
                If aliases.Any Then .AddField("Aliases", String.Join(", ", aliases), True)
            End With

            If TypeOf cmd Is CommandGroup Then
                Dim cmdGroup = DirectCast(cmd, CommandGroup)
                If cmdGroup.IsExecutableWithoutSubcommands Then
                    _embed.WithFooter("This command group can be executed without a child command.")
                Else
                    _embed.WithFooter("This command group must be executed with a child command.")
                End If
            End If

            Return Me
        End Function

        Public Overrides Function WithSubcommands(childCommands As IEnumerable(Of Command)) As BaseHelpFormatter
            If _isCommand Then
                _embed.AddField("Child Commands", String.Join(", ", childCommands.Select(Function(cmd As Command) $"`{cmd.Name}`")))
            Else
                _embed.Description = String.Join($", ", childCommands.Where(Function(s) s.Name <> "help").Select(Function(cmd As Command) $"`{cmd.Name}`"))
            End If
            Return Me
        End Function

        Public Overrides Function Build() As CommandHelpMessage
            If Not _isCommand Then
                With _embed
                    .Title = "Available Commands"
                    .WithFooter("Specify a command to see its usage.")
                End With
            End If
            Return New CommandHelpMessage(embed:=_embed)
        End Function

        Private Function GetCommandAliases(cmd As Command) As IEnumerable(Of String)
            If Not cmd.Aliases.Any Then Return New List(Of String)
            If cmd.Parent IsNot Nothing Then Return cmd.Aliases.Select(Function(a) $"`{cmd.Parent.QualifiedName} {a}`")
            Return cmd.Aliases.Select(Function(a As String) $"`{a}`")
        End Function

        Private Function GetCommandType(cmd As Command) As String
            If TypeOf cmd Is CommandGroup Then
                If cmd.Parent IsNot Nothing Then Return "Command Subgroup"
                Return "Command Group"
            End If

            If cmd.Parent IsNot Nothing Then
                Return "Child Command"
            End If

            Return "Command"
        End Function
    End Class
End Namespace