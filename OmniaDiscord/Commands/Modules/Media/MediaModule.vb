Imports DSharpPlus
Imports DSharpPlus.CommandsNext.Attributes

Namespace Commands.Modules.Media

    <Group("music"), RequireGuild>
    <Description("")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.AddReactions Or Permissions.UseExternalEmojis Or Permissions.UseVoice Or Permissions.Speak)>
    Public Class MediaModule
        Inherits OmniaCommandBase


    End Class
End Namespace