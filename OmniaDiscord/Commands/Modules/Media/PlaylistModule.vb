Imports DSharpPlus.CommandsNext.Attributes
Imports OmniaDiscord.Commands.Bases

Namespace Commands.Modules.Media
    Partial Class MediaModule

        <Group("playlist"), Aliases("pl")>
        <Description("Allows for the management of user created playlists.")>
        Public Class PlaylistSubmodule
            Inherits OmniaDbCommandBase

        End Class
    End Class
End Namespace