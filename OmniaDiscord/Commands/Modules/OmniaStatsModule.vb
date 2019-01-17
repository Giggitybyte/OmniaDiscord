Imports System.IO
Imports System.Reflection
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities

Namespace Commands.Modules
    Public Class OmniaStatsModule
        Inherits BaseCommandModule

        <Command("omniastats"), Aliases("stats")>
        <Description("Displays information and stats for Omnia.")>
        <RequireBotPermissions(Permissions.EmbedLinks)>
        Public Async Function InfoCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.CornflowerBlue

                Dim userCount As Integer
                For Each guild In ctx.Client.Guilds
                    userCount += guild.Value.MemberCount
                Next

                .AddField("User Count", userCount.ToString("N0"), True)
                .AddField("Server Count", ctx.Client.Guilds.Count.ToString("N0"), True)
                .AddField("Uptime", Core.Utilities.FormatTimespan(Date.Now - Process.GetCurrentProcess().StartTime), True)
                .AddField("Database Size", $"{((New FileInfo("Omnia.db").Length / 1024.0F) / 1024.0F).ToString("N2")} MB", True)
                .AddField("Omnia Version", Assembly.GetEntryAssembly().GetName().Version.ToString, True)
                .AddField("DSharpPlus Version", Assembly.GetAssembly(GetType(DiscordClient)).GetName().Version.ToString, True)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

    End Class
End Namespace