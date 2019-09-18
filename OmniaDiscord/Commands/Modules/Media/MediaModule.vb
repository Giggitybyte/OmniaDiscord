Imports DSharpPlus
Imports DSharpPlus.CommandsNext.Attributes
Imports OmniaDiscord.Commands.Bases

Namespace Commands.Modules.Media

    <Group("music"), RequireGuild>
    <Description("Allows for the playback of music and other audio.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.AddReactions Or Permissions.UseExternalEmojis Or Permissions.UseVoice Or Permissions.Speak)>
    Public Class MediaModule
        Inherits OmniaMediaCommandBase


    End Class
End Namespace