Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities

Namespace Commands.Modules

    <Group("lobby")>
    Public Class LobbySystemModule
        Inherits OmniaCommandBase

        <Command("add")>
        Public Async Function AddLobbyCommand(ctx As CommandContext, channel As DiscordChannel) As Task

        End Function

    End Class
End Namespace