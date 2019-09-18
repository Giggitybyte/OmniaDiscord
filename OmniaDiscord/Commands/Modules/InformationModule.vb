Imports System.Reflection
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Humanizer
Imports Humanizer.Localisation

Namespace Commands.Modules
    <Group("info"), Aliases("i"), RequireGuild>
    <Description("Displays various discord related information.")>
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
                .AddField("Uptime", (Date.Now - Process.GetCurrentProcess().StartTime).Humanize(minUnit:=TimeUnit.Second), True)
                .AddField("Ping", $"{ctx.Client.Ping.ToString("N0")}ms", True)
                .AddField("DSharpPlus Version", Assembly.GetAssembly(GetType(DiscordClient)).GetName().Version.ToString, True)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("server")>
        <Description("Displays info about this server.")>
        Public Async Function ServerInfoCommand(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.CornflowerBlue

                .AddField("Owner", ctx.Guild.Owner.Mention, True)
                .AddField("Discord ID", Formatter.InlineCode(ctx.Guild.Id), True)
                .AddField("Voice Region", Formatter.InlineCode(ctx.Guild.VoiceRegion.Name), True)
                .AddField("Omnia Shard", Formatter.InlineCode(ctx.Client.ShardId), True)
                .AddField("User Count", Formatter.InlineCode(ctx.Guild.Members.Where(Function(m) Not m.Value.IsBot).Count), True)
                .AddField("Bot Count", Formatter.InlineCode(ctx.Guild.Members.Where(Function(m) m.Value.IsBot).Count), True)
                .AddField("Creation Date", $"`{ctx.Guild.CreationTimestamp.ToString("g")} ({ctx.Guild.CreationTimestamp.ToLocalTime.Humanize})`")

                .WithAuthor(ctx.Guild.Name, iconUrl:=ctx.Guild.IconUrl)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("user")>
        <Description("Displays info about a user.")>
        Public Async Function UserInfoCommand(ctx As CommandContext, Optional discordUser As DiscordMember = Nothing) As Task
            If discordUser Is Nothing Then discordUser = ctx.Member
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.CornflowerBlue
                .AddField("Discord ID", discordUser.Id, True)
                .AddField("Profile Picture", $"[Direct Link]({discordUser.AvatarUrl})", True)
                .AddField("Server Join Date", $"{discordUser.JoinedAt.ToString("g")} ({discordUser.JoinedAt.ToLocalTime.Humanize})", True)
                .AddField("Account Creation Date", $"{discordUser.CreationTimestamp.ToString("g")} ({discordUser.CreationTimestamp.ToLocalTime.Humanize})", True)
                .WithAuthor(discordUser.Username, iconUrl:=discordUser.AvatarUrl)
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function
    End Class
End Namespace