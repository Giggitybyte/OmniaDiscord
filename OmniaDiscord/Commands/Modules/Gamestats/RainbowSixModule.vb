﻿Imports System.Text
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Humanizer
Imports Humanizer.Localisation
Imports Newtonsoft.Json
Imports OmniaDiscord.Entities.Gamestats
Imports OmniaDiscord.Utilities

Namespace Commands.Modules.Gamestats
    Partial Class GameStatsModule
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

            Dim searchJson As String = GeneralUtilities.GetJson($"{baseUrl}/player-search/{Uri.EscapeUriString(username)}/{platform}")
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

            Dim statJson As String = GeneralUtilities.GetJson($"{baseUrl}/stats/{player.UbisoftId}")
            Dim statData As SiegeStatData
            Dim seasonData As SiegeSeasonData

            If Not statJson Is Nothing Then
                statData = JsonConvert.DeserializeObject(Of SiegeStatData)(statJson, jsonSettings)
                seasonData = JsonConvert.DeserializeObject(Of SiegeSeasonData)(GeneralUtilities.GetJson($"{baseUrl}/stats/{player.UbisoftId}/seasonal"), jsonSettings)
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


                strBuilder.AppendLine($"Current Rank: `{SiegeRanks.GetNameFromId(ranking.Rank)}`")
                strBuilder.AppendLine($"Current MMR: `{ranking.Mmr}`")
                strBuilder.AppendLine($"Highest Rank: `{SiegeRanks.GetNameFromId(ranking.MaxRank)}`")
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
                        strBuilder.AppendLine($"{season.Name}: `{SiegeRanks.GetNameFromId(last.MaxRank)}`")
                    End If
                Next

                .AddField("Previous Rankings", strBuilder.ToString, True)
                strBuilder.Clear()


                .Color = DiscordColor.SpringGreen
                .ThumbnailUrl = $"https://r6tab.com/images/pngranks/{ranking.Rank}.png"

                .Author = New DiscordEmbedBuilder.EmbedAuthor With {
                    .Name = $"{player.Username} • Level {statData.Progression.Level} • {platform.ToUpper}",
                    .IconUrl = $"https://ubisoft-avatars.akamaized.net/{player.UbisoftId}/default_146_146.png?appId=39baebad-39e5-4552-8c25-2c9b919064e2",
                    .Url = $"https://r6stats.com/stats/{player.UbisoftId}/"
                }

                .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                    .Text = $"Rainbow Six Siege  •  Updated {(Date.Now - ranking.CreatedForDate.ToLocalTime).Humanize(maxUnit:=TimeUnit.Hour)} ago",
                    .IconUrl = "https://i.imgur.com/F8SVRpS.png"
                }
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function
    End Class

End Namespace