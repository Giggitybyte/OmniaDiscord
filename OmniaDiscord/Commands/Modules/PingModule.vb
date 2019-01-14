Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities

Namespace Commands.Modules

    Public Class PingModule
        Inherits BaseCommandModule

        <Command("ping"), Description("Displays the latency to the Discord gateway.")>
        <RequireBotPermissions(Permissions.SendMessages Or Permissions.EmbedLinks)>
        Public Async Function Ping(ctx As CommandContext) As Task

            Dim latency As Integer = ctx.Client.Ping
            Dim embed As New DiscordEmbedBuilder With {
                .Title = $"{DiscordEmoji.FromName(ctx.Client, ":ping_pong:")} Pong!",
                .Description = $"Websocket: `{latency}ms`"
            }

            Select Case latency
                Case <= 300
                    embed.Color = DiscordColor.SpringGreen
                Case 301 To 699
                    embed.Color = DiscordColor.Yellow
                Case >= 700
                    embed.Color = DiscordColor.Red
            End Select

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

    End Class

End Namespace