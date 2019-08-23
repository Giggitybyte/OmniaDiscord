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
        <Description("Retrieves an overview player stats for Blizzard's Overwatch. Valid platforms are PC, PSN, and XBL.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function OverwatchStats(ctx As CommandContext, platform As String, <RemainingText> username As String) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim owClient As New OverwatchClient
            Dim owPlayer As OverwatchPlayer
            Dim owPlatform As OverwatchPlatform

            Await ctx.TriggerTypingAsync

            If [Enum].TryParse(platform, True, owPlatform) = False Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Platform"
                    .Description = $"Valid platforms: {String.Join(", ", [Enum].GetNames(owPlatform.GetType).Select(Function(s) s.ToLower))}"
                End With
            Else
                Try
                    owPlayer = Await owClient.GetPlayerAsync(username, owPlatform)

                Catch ex As FormatException
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Unable To Retrieve Stats"
                        .Description = $"An invalid response was recieved from Blizzard.{Environment.NewLine}Please try again in a moment."
                    End With

                Catch ex As ArgumentException
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Invalid Username"
                        .Description = $"The username you provided was an invalid username.{Environment.NewLine}Make sure you're entering the correct username/battletag."
                    End With

                Catch ex As Exception
                    With embed
                        .Color = DiscordColor.DarkRed
                        .Title = "Unable To Retrieve Stats"
                        .Description = $"Something went wrong while trying to retrieve requested stats.{Environment.NewLine}```{ex.GetType.ToString}:{Environment.NewLine}{ex.Message}```"
                    End With

                End Try

                If owPlayer IsNot Nothing Then

                    With embed
                        Dim strBuilder As New StringBuilder

                        If Not owPlayer.SkillRatings.Any Then
                            .ThumbnailUrl = $"{Bot.Config.ResourceUrl}/assets/overwatch/logo.png"
                        Else
                            Dim sr = owPlayer.SkillRatings.GetHighestRole.Value
                            .ThumbnailUrl = $"{Bot.Config.ResourceUrl}/assets/overwatch/skillrating/{sr}.png"
                        End If

                        .Author = New DiscordEmbedBuilder.EmbedAuthor With {
                            .Name = $"{owPlayer.Username} • Level {owPlayer.PlayerLevel} • {owPlayer.Platform.ToString}",
                            .IconUrl = owPlayer.PlayerIconUrl,
                            .Url = owPlayer.ProfileUrl
                        }

                        .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                            .Text = "Overwatch",
                            .IconUrl = $"{Bot.Config.ResourceUrl}/assets/overwatch/logo.png"
                        }

                        .Color = DiscordColor.SpringGreen

                        .Description &= $"Endorsement Level {owPlayer.EndorsementLevel} - "
                        .Description &= $"{DiscordEmoji.FromName(ctx.Client, ":omnia_shotcaller:")}`{CInt(owPlayer.Endorsements(OverwatchEndorsement.Shotcaller) * 100)}%` "
                        .Description &= $"{DiscordEmoji.FromName(ctx.Client, ":omnia_teammate:")}`{CInt(owPlayer.Endorsements(OverwatchEndorsement.GoodTeammate) * 100)}%` "
                        .Description &= $"{DiscordEmoji.FromName(ctx.Client, ":omnia_sportsmanship:")}`{CInt(owPlayer.Endorsements(OverwatchEndorsement.Sportsmanship) * 100)}%`"

                        If owPlayer.IsProfilePrivate Then
                            strBuilder.Append($"This player has their profile set to private.{Environment.NewLine}QP and Competitive stats are unavailable for this player.")
                            strBuilder.Append($"{Environment.NewLine}{Environment.NewLine}If this is your profile, you can modify this setting in Overwatch.")

                            .AddField("Private Profile", strBuilder.ToString)
                        Else

                            Dim qpTimePlayed As Double = If(owPlayer.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Game", "Time Played")?.Value, 0)
                            Dim qpGamesWon As Double = If(owPlayer.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Game", "Games Won")?.Value, 0)
                            Dim qpElims As Double = If(owPlayer.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Combat", "Eliminations")?.Value, 0)
                            Dim qpDeaths As Double = If(owPlayer.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Combat", "Deaths")?.Value, 0)
                            Dim qpSoloKills As Double = If(owPlayer.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Combat", "Solo Kills")?.Value, 0)
                            Dim qpMedals As Double = If(owPlayer.Stats(OverwatchGamemode.Quickplay).GetStatExact("All Heroes", "Match Awards", "Medals")?.Value, 0)

                            strBuilder.Append($"Time Played: `{TimeSpan.FromSeconds(qpTimePlayed).Humanize(maxUnit:=TimeUnit.Hour)}`{Environment.NewLine}")
                            strBuilder.Append($"Games Won: `{qpGamesWon.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"K/D Ratio: `{(qpElims / qpDeaths).ToStringNoRounding}`{Environment.NewLine}")
                            strBuilder.Append($"Eliminations: `{qpElims.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"Solo Kills: `{qpSoloKills.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"Total Medals: `{qpMedals.ToString("N0")}`")

                            .AddField("Quick Play", strBuilder.ToString, True)
                            strBuilder.Clear()

                            Dim compTimePlayed As Double = If(owPlayer.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Game", "Time Played")?.Value, 0)
                            Dim compGamesPlayed As Double = If(owPlayer.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Game", "Games Played")?.Value, 0)
                            Dim compGamesWon As Double = If(owPlayer.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Game", "Games Won")?.Value, 0)
                            Dim compDeaths As Double = If(owPlayer.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Combat", "Deaths")?.Value, 0)
                            Dim compElims As Double = If(owPlayer.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Combat", "Eliminations")?.Value, 0)
                            Dim compSoloKills As Double = If(owPlayer.Stats(OverwatchGamemode.Competitive).GetStatExact("All Heroes", "Combat", "Solo Kills")?.Value, 0)

                            strBuilder.Append($"Time Played: `{TimeSpan.FromSeconds(compTimePlayed).Humanize(maxUnit:=TimeUnit.Hour)}`{Environment.NewLine}")
                            strBuilder.Append($"Games Played: `{compGamesPlayed.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"Games Won: `{compGamesWon.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"K/D Ratio: `{(compElims / compDeaths).ToStringNoRounding}`{Environment.NewLine}")
                            strBuilder.Append($"Eliminations: `{compElims.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"Solo Kills: `{compSoloKills.ToString("N0")}`")

                            .AddField("Competitive", strBuilder.ToString, True)
                            strBuilder.Clear()

                            If qpTimePlayed > 0 Then FormatOverwatchHeroPlaytime(ctx.Client, owPlayer.Stats(OverwatchGamemode.Quickplay), strBuilder)
                            .AddField("Most Played - QP", If(strBuilder.Length > 0, strBuilder.ToString, "Nobody :("), True)
                            strBuilder.Clear()

                            If compTimePlayed > 0 Then FormatOverwatchHeroPlaytime(ctx.Client, owPlayer.Stats(OverwatchGamemode.Competitive), strBuilder)
                            .AddField("Most Played - Comp", If(strBuilder.Length > 0, strBuilder.ToString, "Nobody :("), True)
                            strBuilder.Clear()

                        End If
                    End With

                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)

        End Function

        Private Sub FormatOverwatchHeroPlaytime(client As DiscordClient, stats As List(Of OverwatchStat), ByRef strBuilder As StringBuilder)
            Dim sortedStats As List(Of OverwatchStat) = stats.FilterByName("Time Played").OrderByDescending(Function(s) s.Value).Where(Function(s) s.Hero <> "AllHeroes" AndAlso s.Value <> 0).Take(5).ToList
            If Not sortedStats.Any Then Return

            For Each heroStat In sortedStats
                Dim heroName As String = heroStat.Hero
                Dim playtime As String = TimeSpan.FromSeconds(heroStat.Value).Humanize(maxUnit:=TimeUnit.Hour)

                strBuilder.Append($"{DiscordEmoji.FromName(client, $":omnia_{heroStat.Hero.ToLower}icon:")}{heroName}: `{playtime}`{Environment.NewLine}")
            Next
        End Sub
    End Class
End Namespace