Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Humanizer
Imports Humanizer.Localisation
Imports Newtonsoft.Json
Imports OmniaDiscord.Entities.Gamestats
Imports Overstarch
Imports Overstarch.Entities
Imports Overstarch.Enums
Imports Overstarch.Extensions
Namespace Commands.Modules

    <Group("gamestats"), Aliases("gs")>
    <Description("Command group for the retrival of player stats for several popular multiplayer games.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.UseExternalEmojis)>
    Public Class GameStatsModule
        Inherits OmniaCommandBase

#Region "Commands"
        <Command("siege"), Aliases("r6s", "r6")>
        <Description("Retrieves various player stats for Ubisoft's Rainbow Six Siege. Valid platforms are PC, PSN, and XBL. Valid regions are NA, EU, and AS.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function RainbowSixSiegeCommand(ctx As CommandContext, platform As String, region As String, <RemainingText> username As String) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim validPlatforms As New List(Of String) From {"PC", "XBL", "PSN"}
            Dim validRegions As New Dictionary(Of String, String) From {
                {"NA", "ncsa"},
                {"EU", "emea"},
                {"AS", "apac"}
            }

            If Not validPlatforms.Contains(platform.ToUpper) Then
                With embed
                    .Title = "Invalid Platform"
                    .Description = $"The platform you provided was not a valid platform.{Environment.NewLine}Valid platforms: {String.Join(", ", validPlatforms)}"
                End With

            ElseIf Not validRegions.ContainsKey(region.ToUpper) Then
                With embed
                    .Title = "Invalid Region"
                    .Description = $"The region you provided was not a valid region.{Environment.NewLine}Valid regions: {String.Join(", ", validRegions.Keys)}"
                End With

            End If

            If Not String.IsNullOrEmpty(embed.Description) Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.TriggerTypingAsync

            Dim baseUrl As String = "https://r6stats.com/api/"
            Dim jsonSettings As New JsonSerializerSettings With {
                .NullValueHandling = NullValueHandling.Ignore,
                .MissingMemberHandling = MissingMemberHandling.Ignore
            }

            Dim searchJson As String = Utilities.GetJson($"{baseUrl}/player-search/{Uri.EscapeUriString(username)}/{platform}")
            Dim player As SiegeSearchResult

            If Not String.IsNullOrEmpty(searchJson) Then
                Dim players As List(Of SiegeSearchResult) = JsonConvert.DeserializeObject(Of List(Of SiegeSearchResult))(searchJson, jsonSettings)
                player = players.FirstOrDefault(Function(r) r.Username.ToLower = username.ToLower)
            End If

            If player Is Nothing Then
                With embed
                    .Title = "Player Not Found"
                    .Description = "A player profile could not be found with the input provided."
                    .Description &= $"{Environment.NewLine}Make sure you have the correct username, region, and platform."
                End With

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim statJson As String = Utilities.GetJson($"{baseUrl}/stats/{player.UbisoftId}")
            Dim statData As SiegeStatData
            Dim seasonData As SiegeSeasonData

            If Not statJson Is Nothing Then
                statData = JsonConvert.DeserializeObject(Of SiegeStatData)(statJson, jsonSettings)
                seasonData = JsonConvert.DeserializeObject(Of SiegeSeasonData)(Utilities.GetJson($"{baseUrl}/stats/{player.UbisoftId}/seasonal"), jsonSettings)
            End If

            If statData Is Nothing Or seasonData Is Nothing Then
                With embed
                    .Title = "Unable To Retrieve Stats"
                    .Description = $"Something went wrong while getting player data from R6Stats."
                End With

                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim strBuilder As New StringBuilder
            Dim embeds As New Dictionary(Of DiscordEmoji, DiscordEmbed)
            Dim stats As Stats = statData.Stats.OrderByDescending(Function(s) s.Timestamps.LastUpdated).FirstOrDefault
            Dim currentSeason As SeasonInfo = seasonData.Seasons.FirstOrDefault.Value
            Dim ranking As RegionRanking = currentSeason.Regions(validRegions(region.ToUpper)).OrderByDescending(Function(r) r.CreatedForDate).FirstOrDefault

            embed = New DiscordEmbedBuilder

            With embed
                strBuilder.AppendLine($"K/D Ratio: `{(stats.Queue.Ranked.Kills / stats.Queue.Ranked.Deaths).ToStringNoRounding}`")
                strBuilder.AppendLine($"Kills: `{stats.Queue.Ranked.Kills.ToString("N0")}`")
                strBuilder.AppendLine($"Deaths: `{stats.Queue.Ranked.Deaths.ToString("N0")}`")
                strBuilder.AppendLine($"W/L Ratio: `{(stats.Queue.Ranked.Wins / stats.Queue.Ranked.Losses).ToStringNoRounding}`")
                strBuilder.AppendLine($"Wins: `{stats.Queue.Ranked.Wins.ToString("N0")}`")
                strBuilder.AppendLine($"Losses: `{stats.Queue.Ranked.Losses.ToString("N0")}`")
                strBuilder.AppendLine($"Total Matches: `{stats.Queue.Ranked.GamesPlayed.ToString("N0")}`")
                strBuilder.AppendLine($"K/M Average: `{(stats.Queue.Ranked.Kills / stats.Queue.Ranked.GamesPlayed).ToStringNoRounding}`")
                strBuilder.AppendLine($"Time Played: `{TimeSpan.FromSeconds(stats.Queue.Ranked.Playtime).Humanize(maxUnit:=TimeUnit.Hour)}`")

                .AddField("Ranked Overall", strBuilder.ToString, True)
                strBuilder.Clear()


                strBuilder.AppendLine($"Current Rank: `{SiegeRanks.GetRankFromId(ranking.Rank).rankName}`")
                strBuilder.AppendLine($"Current MMR: `{ranking.Mmr}`")
                strBuilder.AppendLine($"Highest Rank: `{SiegeRanks.GetRankFromId(ranking.MaxRank).rankName}`")
                strBuilder.AppendLine($"Highest MMR: `{ranking.MaxMmr}`")
                strBuilder.AppendLine($"W/L Ratio: `{(ranking.Wins / ranking.Losses).ToStringNoRounding}`")
                strBuilder.AppendLine($"Wins: `{ranking.Wins.ToString("N0")}`")
                strBuilder.AppendLine($"Losses: `{ranking.Losses.ToString("N0")}`")
                strBuilder.AppendLine($"Abandons: `{ranking.Abandons.ToString("N0")}`")
                If Not ranking.Rank = 0 AndAlso Not ranking.NextRankMmr = 0 Then strBuilder.AppendLine($"Next Rank In: `{(ranking.NextRankMmr - ranking.Mmr).ToStringNoRounding} MMR`")

                .AddField($"{currentSeason.Name}", strBuilder.ToString, True)
                strBuilder.Clear()


                strBuilder.AppendLine($"K/D Ratio: `{(stats.Queue.Casual.Kills / stats.Queue.Casual.Deaths).ToStringNoRounding}`")
                strBuilder.AppendLine($"Kills: `{stats.Queue.Casual.Kills.ToString("N0")}`")
                strBuilder.AppendLine($"Deaths: `{stats.Queue.Casual.Deaths.ToString("N0")}`")
                strBuilder.AppendLine($"W/L Ratio: `{(stats.Queue.Casual.Wins / stats.Queue.Casual.Losses).ToStringNoRounding}`")
                strBuilder.AppendLine($"Wins: `{stats.Queue.Casual.Wins.ToString("N0")}`")
                strBuilder.AppendLine($"Losses: `{stats.Queue.Casual.Losses.ToString("N0")}`")
                strBuilder.AppendLine($"Total Matches: `{stats.Queue.Casual.GamesPlayed.ToString("N0")}`")
                strBuilder.AppendLine($"K/M Average: `{(stats.Queue.Casual.Kills / stats.Queue.Casual.GamesPlayed).ToStringNoRounding()}`")
                strBuilder.AppendLine($"Time Played: `{TimeSpan.FromSeconds(stats.Queue.Casual.Playtime).Humanize(maxUnit:=TimeUnit.Hour)}`")

                .AddField("Casual Overall", strBuilder.ToString, True)
                strBuilder.Clear()


                For Each season In seasonData.Seasons.Values.Take(9)
                    If Not season.Id = currentSeason.Id Then
                        Dim last As RegionRanking = season.Regions(validRegions(region.ToUpper)).OrderByDescending(Function(r) r.CreatedForDate).FirstOrDefault
                        strBuilder.AppendLine($"{season.Name}: `{SiegeRanks.GetRankFromId(last.MaxRank).rankName}`")
                    End If
                Next

                .AddField("Previous Rankings", strBuilder.ToString, True)
                strBuilder.Clear()


                Dim timeDifference As TimeSpan = Date.Now - ranking.CreatedForDate.ToLocalTime
                Dim humanizedTime As String = If(timeDifference <= TimeSpan.FromSeconds(10), "A Moment Ago", $"{timeDifference.Humanize(maxUnit:=TimeUnit.Hour)} ago")

                .Color = DiscordColor.SpringGreen
                .ThumbnailUrl = SiegeRanks.GetRankFromId(ranking.Rank).url

                .Author = New DiscordEmbedBuilder.EmbedAuthor With {
                    .Name = $"{player.Username} • Level {statData.Progression.Level} • {platform.ToUpper}",
                    .IconUrl = $"https://ubisoft-avatars.akamaized.net/{player.UbisoftId}/default_146_146.png?appId=39baebad-39e5-4552-8c25-2c9b919064e2",
                    .Url = $"https://r6stats.com/stats/{player.UbisoftId}/"
                }

                .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                    .Text = $"Rainbow Six Siege  •  Updated {humanizedTime}",
                    .IconUrl = "https://i.imgur.com/F8SVRpS.png"
                }
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("overwatch"), Aliases("ow")>
        <Description("Retrieves an overview player stats for Blizzard's Overwatch. Valid platforms are PC, PSN, and XBL.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function OverwatchStats(ctx As CommandContext, platform As String, <RemainingText> username As String) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim owClient As New OverwatchClient
            Dim owPlayer As OverwatchPlayer = Nothing
            Dim owPlatform As OverwatchPlatform = Nothing

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

                        If owPlayer.CompetitiveSkillRating = 0 Then
                            .ThumbnailUrl = $"{OmniaConfig.ResourceUrl}/assets/overwatch/logo.png"
                        Else
                            .ThumbnailUrl = $"{OmniaConfig.ResourceUrl}/assets/overwatch/skillrating/{owPlayer.CompetitiveSkillRating}.png"
                        End If

                        .Author = New DiscordEmbedBuilder.EmbedAuthor With {
                            .Name = $"{owPlayer.Username} • Level {owPlayer.PlayerLevel} • {owPlayer.Platform.ToString}",
                            .IconUrl = owPlayer.PlayerIconUrl,
                            .Url = owPlayer.ProfileUrl
                        }

                        .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                            .Text = "Overwatch",
                            .IconUrl = $"{OmniaConfig.ResourceUrl}/assets/overwatch/logo.png"
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
#End Region

#Region "Helper Methods"
        Private Sub FormatOverwatchHeroPlaytime(client As DiscordClient, stats As List(Of OverwatchStat), ByRef strBuilder As StringBuilder)
            Dim sortedStats As List(Of OverwatchStat) = stats.FilterByName("Time Played").OrderByDescending(Function(s) s.Value).Where(Function(s) s.Hero <> "AllHeroes" AndAlso s.Value <> 0).Take(5).ToList
            If Not sortedStats.Any Then Return

            For Each heroStat In sortedStats
                Dim heroName As String = heroStat.Hero
                Dim playtime As String = TimeSpan.FromSeconds(heroStat.Value).Humanize(maxUnit:=TimeUnit.Hour)

                strBuilder.Append($"{DiscordEmoji.FromName(client, $":omnia_{heroStat.Hero.ToLower}icon:")}{heroName}: `{playtime}`{Environment.NewLine}")
            Next
        End Sub
#End Region

    End Class
End Namespace