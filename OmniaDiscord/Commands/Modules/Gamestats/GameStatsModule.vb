Imports DSharpPlus
Imports DSharpPlus.CommandsNext.Attributes
Namespace Commands.Modules.Gamestats
    <Group("gamestats"), Aliases("gs")>
    <Description("Command group for the retrival of player stats for several popular multiplayer games.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.UseExternalEmojis)>
    Public Class GameStatsModule
        Inherits OmniaCommandBase
    End Class
End Namespace