Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.CommandsNext.Entities
Imports DSharpPlus.Entities

Namespace Commands

    Public Class HelpFormatter
        Inherits BaseHelpFormatter

        Private _command As Command
        Private _embed As DiscordEmbedBuilder

        Public Sub New(ctx As CommandContext)
            MyBase.New(ctx)
            _embed = New DiscordEmbedBuilder With {.Color = DiscordColor.CornflowerBlue}
        End Sub

        Public Overrides Function WithCommand(command As Command) As BaseHelpFormatter
            _command = command
            _embed.AddField($"Command", $"`{command.QualifiedName}`", True)

            Dim cmdGroup As CommandGroup = TryCast(command, CommandGroup)

            If cmdGroup IsNot Nothing AndAlso cmdGroup.Aliases?.Any Then
                _embed.AddField("Aliases", String.Join(", ", cmdGroup.Aliases.Select(Function(altName As String) $"`{altName}`")), True)

            ElseIf command.Aliases?.Any Then

                If command.Parent IsNot Nothing Then
                    _embed.AddField("Aliases", String.Join(", ", command.Aliases.Select(Function(altName As String) $"`{command.Parent.QualifiedName} {altName}`")), True)
                Else
                    _embed.AddField("Aliases", String.Join(", ", command.Aliases.Select(Function(altName As String) $"`{altName}`")), True)
                End If

            End If

            _embed.AddField("Description", $"{If(command.Description, "Description not set.")}")

            If command.Overloads?.Any Then

                Dim overloadsWithArgs As New List(Of CommandOverload)
                Dim overloadsWithNoArgs As New List(Of CommandOverload)

                For Each overload As CommandOverload In command.Overloads
                    If overload.Arguments.Count = 0 Then
                        overloadsWithNoArgs.Add(overload)
                    Else
                        overloadsWithArgs.Add(overload)
                    End If
                Next

                Dim messageString As String = String.Empty
                If overloadsWithArgs.Count > 0 Then

                    For Each overload As CommandOverload In overloadsWithArgs.OrderBy(Function(x) x.Priority)

                        messageString &= $"`{command.QualifiedName} "

                        For Each arg As CommandArgument In overload.Arguments
                            messageString &= $"{If(arg.IsOptional, "(", "[")}{arg.Name}{If(arg.IsOptional, ")", "]")} "
                        Next

                        messageString &= $"`{Environment.NewLine}"
                    Next

                    If overloadsWithNoArgs.Count > 0 Then messageString &= $"{Environment.NewLine}This command can also be executed without any arguments"

                    _embed.AddField("Arguments", messageString.Trim)
                End If
            End If

            If cmdGroup IsNot Nothing Then
                If cmdGroup.IsExecutableWithoutSubcommands Then
                    _embed.WithFooter("This command can be executed without a subcommand.")
                Else
                    _embed.WithFooter("This command must be executed with a subcommand.")
                End If
            End If

            Return Me
        End Function

        Public Overrides Function WithSubcommands(subcommands As IEnumerable(Of Command)) As BaseHelpFormatter
            If _command Is Nothing Then
                _embed.Description = String.Join($", ", subcommands.Where(Function(s) s.Name <> "help").Select(Function(cmd As Command) $"`{cmd.Name}`"))
            Else
                _embed.AddField("Subcommands", String.Join(", ", subcommands.Select(Function(cmd As Command) $"`{cmd.Name}`")))
            End If

            Return Me
        End Function

        Public Overrides Function Build() As CommandHelpMessage
            If _command Is Nothing Then
                With _embed
                    .Title = "Available Commands"
                    .WithFooter("Specify a command to see its usage.")
                End With
            End If

            Return New CommandHelpMessage(embed:=_embed)
        End Function

    End Class

End Namespace