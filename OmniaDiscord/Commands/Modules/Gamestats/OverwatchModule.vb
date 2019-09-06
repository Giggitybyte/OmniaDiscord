Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Humanizer
Imports Humanizer.Localisation
Imports Overstarch
Imports Overstarch.Entities
Imports Overstarch.Enums
Imports Overstarch.Extensions

Namespace Commands.Modules.Gamestats
    Partial Class GameStatsModule
        <Command("overwatch"), Aliases("ow")>
        <Description("Retrieves an overview of player stats for Blizzard's Overwatch. Valid platforms are PC, PSN, and XBL.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function OverwatchStats(ctx As CommandContext, platform As String, <RemainingText> username As String) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim client As New OverwatchClient
            Dim owPlatform As OverwatchPlatform

            If Not [Enum].TryParse(platform, True, owPlatform) Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Platform"
                    .Description = $"Valid platforms: {String.Join(", ", [Enum].GetNames(owPlatform.GetType).Select(Function(s) s.ToLower))}"
                End With
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim err As Exception
            Dim player As OverwatchPlayer
            Await ctx.TriggerTypingAsync

            Try
                player = Await client.GetPlayerAsync(username, owPlatform)
            Catch ex As Exception
                err = ex
            End Try

            If err IsNot Nothing Then
                If TypeOf err Is FormatException Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Unable To Retrieve Stats"
                        .Description = $"An invalid response was recieved from Blizzard.{Environment.NewLine}Please try again in a moment."
                    End With
                ElseIf TypeOf err Is ArgumentException Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Invalid Username"
                        .Description = $"The username you provided was an invalid username.{Environment.NewLine}Make sure you're entering the correct username/battletag."
                    End With
                Else
                    With embed
                        .Color = DiscordColor.DarkRed
                        .Title = "Unable To Retrieve Stats"
                        .Description = $"Something went wrong while trying to retrieve requested stats.{Environment.NewLine}```{err.Message}```"
                    End With
                End If

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            embed.Color = DiscordColor.SpringGreen
            embed.WithAuthor($"{player.Username} • Level {player.PlayerLevel} • {player.Platform.ToString}", player.ProfileUrl, player.PlayerIconUrl)
            embed.WithFooter("Overwatch", $"{Bot.Config.ResourceUrl}/assets/overwatch/logo.png")

            If player.SkillRatings.Any Then
                Dim sr = player.SkillRatings.GetHighestRole.Value
                embed.ThumbnailUrl = $"{Bot.Config.ResourceUrl}/assets/overwatch/skillrating/{sr}.png"
            Else
                embed.ThumbnailUrl = $"{Bot.Config.ResourceUrl}/assets/overwatch/logo.png"
            End If

            Dim strBuilder As New StringBuilder
            For Each role In [Enum].GetValues(GetType(OverwatchRole))
                Dim sr = player.SkillRatings.GetRole(role)
                If sr > 0 Then
                    strBuilder.Append($"{DiscordEmoji.FromName(ctx.Client, $":omnia_ow{role.ToString.ToLower}:")}")
                    strBuilder.Append($"{sr.ToString("N0")}")
                    strBuilder.Append($"{DiscordEmoji.FromName(ctx.Client, $":omnia_ow{sr.ToRank.ToLower}:")}  ")
                End If
            Next

            embed.Description = strBuilder.ToString
            strBuilder.Clear()

            If player.IsProfilePrivate Then
                embed.AddField("Private Profile", $"This player has their profile set to private.{Environment.NewLine}QP and Competitive stats are unavailable for this player.")
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim qpTimePlayed = If(player.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Game", "Time Played")?.Value, 0)
            Dim qpGamesWon = If(player.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Game", "Games Won")?.Value, 0)
            Dim qpElims = If(player.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Combat", "Eliminations")?.Value, 0)
            Dim qpDeaths = If(player.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Combat", "Deaths")?.Value, 0)
            Dim qpSoloKills = If(player.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Combat", "Solo Kills")?.Value, 0)
            Dim qpMedals = If(player.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Match Awards", "Medals")?.Value, 0)

            strBuilder.Append($"Time Played: `{TimeSpan.FromSeconds(qpTimePlayed).Humanize(maxUnit:=TimeUnit.Hour)}`{Environment.NewLine}")
            strBuilder.Append($"Games Won: `{qpGamesWon.ToString("N0")}`{Environment.NewLine}")
            strBuilder.Append($"K/D Ratio: `{(qpElims / qpDeaths).ToStringNoRounding}`{Environment.NewLine}")
            strBuilder.Append($"Eliminations: `{qpElims.ToString("N0")}`{Environment.NewLine}")
            strBuilder.Append($"Solo Kills: `{qpSoloKills.ToString("N0")}`{Environment.NewLine}")
            strBuilder.Append($"Total Medals: `{qpMedals.ToString("N0")}`")

            embed.AddField("Quick Play", strBuilder.ToString, True)
            strBuilder.Clear()

            Dim compTimePlayed = If(player.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Game", "Time Played")?.Value, 0)
            Dim compGamesPlayed = If(player.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Game", "Games Played")?.Value, 0)
            Dim compGamesWon = If(player.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Game", "Games Won")?.Value, 0)
            Dim compDeaths = If(player.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Combat", "Deaths")?.Value, 0)
            Dim compElims = If(player.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Combat", "Eliminations")?.Value, 0)
            Dim compSoloKills = If(player.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Combat", "Solo Kills")?.Value, 0)

            strBuilder.Append($"Time Played: `{TimeSpan.FromSeconds(compTimePlayed).Humanize(maxUnit:=TimeUnit.Hour)}`{Environment.NewLine}")
            strBuilder.Append($"Games Played: `{compGamesPlayed.ToString("N0")}`{Environment.NewLine}")
            strBuilder.Append($"Games Won: `{compGamesWon.ToString("N0")}`{Environment.NewLine}")
            strBuilder.Append($"K/D Ratio: `{(compElims / compDeaths).ToStringNoRounding}`{Environment.NewLine}")
            strBuilder.Append($"Eliminations: `{compElims.ToString("N0")}`{Environment.NewLine}")
            strBuilder.Append($"Solo Kills: `{compSoloKills.ToString("N0")}`")

            embed.AddField("Competitive", strBuilder.ToString, True)
            strBuilder.Clear()

            If qpTimePlayed > 0 Then FormatOverwatchHeroPlaytime(ctx.Client, player.Stats(OverwatchGamemode.Quickplay), strBuilder)
            embed.AddField("Most Played - QP", If(strBuilder.Length > 0, strBuilder.ToString, "Nobody :("), True)
            strBuilder.Clear()

            If compTimePlayed > 0 Then FormatOverwatchHeroPlaytime(ctx.Client, player.Stats(OverwatchGamemode.Competitive), strBuilder)
            embed.AddField("Most Played - Comp", If(strBuilder.Length > 0, strBuilder.ToString, "Nobody :("), True)
            strBuilder.Clear()

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        Private Sub FormatOverwatchHeroPlaytime(client As DiscordClient, stats As List(Of OverwatchStat), ByRef strBuilder As StringBuilder)
            Dim sortedStats = stats.FilterByName("Time Played").OrderByDescending(Function(s) s.Value).Where(Function(s) s.Hero <> "AllHeroes" AndAlso s.Value <> 0).Take(5).ToList
            If Not sortedStats.Any Then Return

            For Each heroStat In sortedStats
                Dim playtime As String = TimeSpan.FromSeconds(heroStat.Value).Humanize(maxUnit:=TimeUnit.Hour)
                strBuilder.Append($"{DiscordEmoji.FromName(client, $":omnia_{heroStat.Hero.ToLower}icon:")}{heroStat.Hero}: `{playtime}`{Environment.NewLine}")
            Next
        End Sub
    End Class
End Namespace