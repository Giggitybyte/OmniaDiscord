Imports System.Reflection
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities

Namespace Commands.Modules

    <Group("info"), Aliases("i"), RequireGuild>
    <Description("Command group that displays information about various Discord related things.")>
    <RequireBotPermissions(Permissions.EmbedLinks)>
    Public Class InformationModule
        Inherits BaseCommandModule

        <Command("omnia")>
        <Description("Displays information and stats for Omnia.")>
        Public Async Function StatsCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.CornflowerBlue

                Dim uniqueUsers As New HashSet(Of ULong)
                For Each guild In ctx.Client.Guilds
                    For Each member In guild.Value.Members
                        uniqueUsers.Add(member.Value.Id)
                    Next
                Next

                .AddField("User Count", uniqueUsers.Count.ToString("N0"), True)
                .AddField("Server Count", ctx.Client.Guilds.Count.ToString("N0"), True)
                .AddField("Shard Count", ctx.Client.ShardCount, True)
                .AddField("Uptime", Utilities.FormatTimespanToString(Date.Now - Process.GetCurrentProcess().StartTime), True)
                .AddField("Ping", $"{ctx.Client.Ping.ToString("N0")}ms", True)
                .AddField("DSharpPlus Version", Assembly.GetAssembly(GetType(DiscordClient)).GetName().Version.ToString, True)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("server")>
        <Description("Displays info about this server.")>
        Public Async Function ServerInfoCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim creationTimeDifference As TimeSpan = Date.Now - ctx.Guild.CreationTimestamp

            With embed
                .Color = DiscordColor.CornflowerBlue

                .AddField("Owner", ctx.Guild.Owner.Mention, True)
                .AddField("User Count", ctx.Guild.MemberCount.ToString("N0"), True)
                .AddField("Voice Region", ctx.Guild.VoiceRegion.Name, True)
                .AddField("Discord ID", ctx.Guild.Id, True)
                .AddField("Omnia Shard", ctx.Client.ShardId, True)
                .AddField("AFK Timer", $"{TimeSpan.FromSeconds(ctx.Guild.AfkTimeout).TotalMinutes} minutes", True)
                .AddField("Creation Date", $"{ctx.Guild.CreationTimestamp.ToString("g")} ({Utilities.FormatTimespanToString(creationTimeDifference, True)} ago)")

                .WithAuthor(ctx.Guild.Name, iconUrl:=ctx.Guild.IconUrl)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("user")>
        <Description("Displays info about a user.")>
        Public Async Function UserInfoCommand(ctx As CommandContext, Optional discordUser As DiscordMember = Nothing) As Task
            If discordUser Is Nothing Then discordUser = ctx.Member

            Dim creationTimeDifference As TimeSpan = Date.Now - discordUser.CreationTimestamp
            Dim joinTimeDifference As TimeSpan = Date.Now - discordUser.JoinedAt
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.CornflowerBlue

                .AddField("Discord ID", discordUser.Id, True)
                .AddField("Profile Picture", $"[Direct Link]({discordUser.AvatarUrl})", True)
                .AddField("Server Join Date", $"{discordUser.JoinedAt.ToString("g")} ({Utilities.FormatTimespanToString(joinTimeDifference, True)} ago)", True)
                .AddField("Account Creation Date", $"{discordUser.CreationTimestamp.ToString("g")} ({Utilities.FormatTimespanToString(creationTimeDifference, True)} ago)", True)

                .WithAuthor(discordUser.Username, iconUrl:=discordUser.AvatarUrl)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

    End Class

End Namespace