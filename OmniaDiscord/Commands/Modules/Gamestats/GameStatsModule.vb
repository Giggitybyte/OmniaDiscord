Imports DSharpPlus
Imports DSharpPlus.CommandsNext.Attributes
Imports OmniaDiscord.Commands.Bases

Namespace Commands.Modules.Gamestats
    <Group("gamestats"), Aliases("gs")>
    <Description("The child commands of this commmand allow for the retrival of player stats for several popular multiplayer games.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.UseExternalEmojis)>
    Public Class GameStatsModule
        Inherits OmniaDbCommandBase

    End Class
End Namespace