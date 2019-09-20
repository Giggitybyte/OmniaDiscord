Imports System.Net
Imports System.Text
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

        <Group("siege"), Aliases("r6s", "r6")>
        <Description("Displays an overview of various player stats for Ubisoft's Rainbow Six Siege. Child commands display more specific stats.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Class SiegeModule
            Inherits BaseCommandModule

            Private Const R6SErrorMessage = "was returned by R6Stats while retrieving stats." + vbCrLf + "R6Stats may be having issues right now; please try again in a moment."
            Private Const R6SPlayerNotFoundMessage = "A player profile could not be found with the input provided." + vbCrLf + "Make sure you have the correct username and platform."
            Private Const R6TErrorMessage = "was returned by R6Tab while retrieving seasonal stats." + vbCrLf + "R6Tab may be having issues right now; please try again in a moment."

            Private ReadOnly _validRegions As New Dictionary(Of String, String) From {
                {"NA", "ncsa"},
                {"EU", "emea"},
                {"AS", "apac"}
            }
            Private ReadOnly _regionNames As New Dictionary(Of String, String) From {
                {"ncsa", "North America"},
                {"emea", "Europe"},
                {"apac", "Asia"}
            }
            Private ReadOnly _platforms As New List(Of String) From {
                "PC",
                "XBL",
                "PSN"
            }

            <GroupCommand>
            Public Async Function SiegeOverviewCommand(ctx As CommandContext, platform As String, <RemainingText> username As String) As Task
                Dim player = Await GetR6StatsPlayer(ctx, username, platform)
                If player Is Nothing Then Return

                Dim statReq = GeneralUtilities.GetJson($"https://r6stats.com/api/stats/{player.UbisoftId}")
                Dim seasonalReq = GeneralUtilities.GetJson($"https://r6stats.com/api/stats/{player.UbisoftId}/seasonal")

                If statReq.json Is Nothing Or seasonalReq.json Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Orange,
                        .Title = "Unable To Retrieve Stats",
                        .Description = $"`{statReq.status}` and `{seasonalReq.status}` {R6SErrorMessage}"
                    })
                    Return
                End If

                Dim strBuilder As New StringBuilder
                Dim jsonSettings As New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore}

                Dim statData = JsonConvert.DeserializeObject(Of R6StatsStatData)(statReq.json, jsonSettings)
                Dim stats = statData.Stats.OrderByDescending(Function(s) s.Timestamps.LastUpdated).FirstOrDefault
                Dim currentSeason = JsonConvert.DeserializeObject(Of R6StatsSeasonData)(seasonalReq.json, jsonSettings).Seasons.First.Value
                Dim embed = InitEmbedBuilder(player, platform, statData.LastUpdated.ToLocalTime)

                strBuilder.AppendLine($"K/D Ratio: `{(stats.Playlist.Ranked.Kills / stats.Playlist.Ranked.Deaths).ToStringNoRounding}`")
                strBuilder.AppendLine($"Kills: `{stats.Playlist.Ranked.Kills.ToString("N0")}`")
                strBuilder.AppendLine($"Deaths: `{stats.Playlist.Ranked.Deaths.ToString("N0")}`")
                strBuilder.AppendLine($"W/L Ratio: `{(stats.Playlist.Ranked.Wins / stats.Playlist.Ranked.Losses).ToStringNoRounding}`")
                strBuilder.AppendLine($"Wins: `{stats.Playlist.Ranked.Wins.ToString("N0")}`")
                strBuilder.AppendLine($"Losses: `{stats.Playlist.Ranked.Losses.ToString("N0")}`")
                strBuilder.AppendLine($"Total Matches: `{stats.Playlist.Ranked.GamesPlayed.ToString("N0")}`")
                strBuilder.AppendLine($"K/M Average: `{(stats.Playlist.Ranked.Kills / stats.Playlist.Ranked.GamesPlayed).ToStringNoRounding}`")
                strBuilder.AppendLine($"Time Played: `{TimeSpan.FromSeconds(stats.Playlist.Ranked.Playtime).Humanize(maxUnit:=TimeUnit.Hour)}`")

                embed.AddField("Ranked Overall", strBuilder.ToString, True)
                strBuilder.Clear()


                strBuilder.AppendLine($"K/D Ratio: `{(stats.Playlist.Casual.Kills / stats.Playlist.Casual.Deaths).ToStringNoRounding}`")
                strBuilder.AppendLine($"Kills: `{stats.Playlist.Casual.Kills.ToString("N0")}`")
                strBuilder.AppendLine($"Deaths: `{stats.Playlist.Casual.Deaths.ToString("N0")}`")
                strBuilder.AppendLine($"W/L Ratio: `{(stats.Playlist.Casual.Wins / stats.Playlist.Casual.Losses).ToStringNoRounding}`")
                strBuilder.AppendLine($"Wins: `{stats.Playlist.Casual.Wins.ToString("N0")}`")
                strBuilder.AppendLine($"Losses: `{stats.Playlist.Casual.Losses.ToString("N0")}`")
                strBuilder.AppendLine($"Total Matches: `{stats.Playlist.Casual.GamesPlayed.ToString("N0")}`")
                strBuilder.AppendLine($"K/M Average: `{(stats.Playlist.Casual.Kills / stats.Playlist.Casual.GamesPlayed).ToStringNoRounding()}`")
                strBuilder.AppendLine($"Time Played: `{TimeSpan.FromSeconds(stats.Playlist.Casual.Playtime).Humanize(maxUnit:=TimeUnit.Hour)}`")

                embed.AddField("Casual Overall", strBuilder.ToString, True)
                strBuilder.Clear()


                Dim regionRankings As New List(Of Integer)
                For Each region In currentSeason.Regions
                    Dim rankedData = region.Value.OrderByDescending(Function(r) r.CreatedForDate).FirstOrDefault
                    strBuilder.Append($"{_regionNames(region.Key)}: `{SiegeRanks.GetNameFromId(currentSeason.Id, rankedData.Rank)}` ")
                    regionRankings.Add(rankedData.Rank)
                Next
                embed.AddField("Current Rankings", strBuilder.ToString)
                embed.ThumbnailUrl = SiegeRanks.GetRankIconUrl(currentSeason.Id, regionRankings.OrderByDescending(Function(i) i).First)
                strBuilder.Clear()

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("ranked")>
            <Description("Displays detailed stats for ranked in the specified region.")>
            Public Async Function SiegeRankedCommand(ctx As CommandContext, region As String, platform As String, <RemainingText> username As String) As Task
                Dim player = Await GetR6StatsPlayer(ctx, username, platform)
                If player Is Nothing Then Return

                region = region.ToUpper

                If Not _validRegions.ContainsKey(region) Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Title = "Invalid Region",
                        .Description = $"The region you provided was not a valid region.{Environment.NewLine}Valid regions: {String.Join(", ", _validRegions.Keys)}"
                    })
                    Return
                End If

                Dim rankedReq, seasonalReq As (json As String, status As HttpStatusCode)
                Dim tasks As New List(Of Task) From {
                    Task.Run(Sub()
                                 GeneralUtilities.GetJson($"https://r6tab.com/mainpage.php?page={player.UbisoftId}&updatenow=true")
                                 seasonalReq = GeneralUtilities.GetJson($"https://r6tab.com/api/player.php?p_id={player.UbisoftId}")
                             End Sub),
                    Task.Run(Sub() rankedReq = GeneralUtilities.GetJson($"https://r6stats.com/api/stats/{player.UbisoftId}/seasonal"))
                }
                Task.WaitAll(tasks.ToArray)

                If rankedReq.json Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Orange,
                        .Title = "Unable To Retrieve Stats",
                        .Description = $"`{rankedReq.status}` {R6SErrorMessage}"
                    })
                    Return
                End If

                If seasonalReq.json Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Orange,
                        .Title = "Unable To Retrieve Stats",
                        .Description = $"`{seasonalReq.status}` {R6TErrorMessage}"
                    })
                    Return
                End If

                Dim jsonSettings As New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore}
                Dim rankedData = JsonConvert.DeserializeObject(Of R6StatsSeasonData)(rankedReq.json, jsonSettings)
                Dim seasonalData = JsonConvert.DeserializeObject(Of R6TabSeasonalData)(seasonalReq.json, jsonSettings)
                Dim strBuilder As New StringBuilder
                Dim embed = InitEmbedBuilder(player, platform, rankedData.LastUpdated.ToLocalTime)

                Dim currentSeason = rankedData.Seasons.First.Value
                Dim stats = currentSeason.Regions(_validRegions(region)).First
                strBuilder.AppendLine($"Current MMR: `{stats.Mmr}` `({SiegeRanks.GetNameFromId(currentSeason.Id, stats.Rank)})`")
                strBuilder.AppendLine($"Highest MMR: `{stats.MaxMmr}` `({SiegeRanks.GetNameFromId(currentSeason.Id, stats.MaxRank)})`")
                strBuilder.AppendLine($"K/D Ratio: `{GeneralUtilities.CalcKdr(seasonalData.Kills, seasonalData.Deaths)}`")
                strBuilder.AppendLine($"Kills: `{seasonalData.Kills.ToString("N0")}`")
                strBuilder.AppendLine($"Deaths: `{seasonalData.Deaths.ToString("N0")}`")
                strBuilder.AppendLine($"Wins: `{seasonalData.Wins.ToString("N0")}`")
                strBuilder.AppendLine($"Losses: `{seasonalData.Losses.ToString("N0")}`")
                strBuilder.AppendLine($"Abandons: `{stats.Abandons.ToString("N0")}`")
                strBuilder.AppendLine($"Win Rate `{(seasonalData.Wins / (seasonalData.Wins + seasonalData.Losses) * 100).ToStringNoRounding}%`")

                If Not stats.Rank = 0 AndAlso Not stats.NextRankMmr = 0 Then strBuilder.AppendLine($"Next Rank In: `{(stats.NextRankMmr - stats.Mmr).ToStringNoRounding} MMR`")

                embed.AddField($"Current Season: {currentSeason.Name}", strBuilder.ToString)
                strBuilder.Clear()

                For Each season In rankedData.Seasons.Skip(1).Take(9).Select(Function(s) s.Value)
                    strBuilder.AppendLine($"{season.Name}: `{SiegeRanks.GetNameFromId(season.Id, season.Regions(_validRegions(region)).First.MaxRank)}`")
                Next
                embed.AddField($"Past Seasons Top Ranks", strBuilder.ToString)
                strBuilder.Clear()

                embed.ThumbnailUrl = SiegeRanks.GetRankIconUrl(currentSeason.Id, stats.Rank)

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("operators"), Aliases("ops")>
            <Description("Displays basic operators stats for the specifed player.")>
            Public Async Function SiegeOperatorsCommand(ctx As CommandContext, platform As String, <RemainingText> username As String) As Task
                Dim player = Await GetR6StatsPlayer(ctx, username, platform)
                If player Is Nothing Then Return

                Dim statReq = GeneralUtilities.GetJson($"https://r6stats.com/api/stats/{player.UbisoftId}")
                If statReq.json Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Orange,
                        .Title = "Unable To Retrieve Stats",
                        .Description = $"`{statReq.status}` {R6SErrorMessage}"
                    })
                    Return
                End If

                Dim jsonSettings As New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore}
                Dim stats = JsonConvert.DeserializeObject(Of R6StatsStatData)(statReq.json, jsonSettings)

                Dim topTimeAtk = stats.Operators.Where(Function(o) o.Operator.Role = "attacker").OrderByDescending(Function(o) o.Playtime).Take(5)
                Dim topKdAtk = stats.Operators.Where(Function(o) o.Operator.Role = "attacker").OrderByDescending(Function(o) o.Kills / o.Deaths).Take(5)
                Dim topTimeDef = stats.Operators.Where(Function(o) o.Operator.Role = "defender").OrderByDescending(Function(o) o.Playtime).Take(5)
                Dim topKdDef = stats.Operators.Where(Function(o) o.Operator.Role = "defender").OrderByDescending(Function(o) o.Kills / o.Deaths).Take(5)

                Dim embed = InitEmbedBuilder(player, platform, stats.LastUpdated.ToLocalTime)
                Dim strBuilder As New StringBuilder
                embed.ThumbnailUrl = stats.Operators.OrderByDescending(Function(o) o.Playtime).First.Operator.Images.Badge

                For Each attacker In topTimeAtk
                    strBuilder.AppendLine($"{attacker.Operator.Name}: `{TimeSpan.FromSeconds(attacker.Playtime).Humanize(maxUnit:=TimeUnit.Hour)}`")
                Next
                embed.AddField("Most Played Attackers", strBuilder.ToString, True)
                strBuilder.Clear()

                For Each defender In topTimeDef
                    strBuilder.AppendLine($"{defender.Operator.Name}: `{TimeSpan.FromSeconds(defender.Playtime).Humanize(maxUnit:=TimeUnit.Hour)}`")
                Next
                embed.AddField("Most Played Defenders", strBuilder.ToString, True)
                strBuilder.Clear()

                For Each attacker In topKdAtk
                    strBuilder.AppendLine($"{attacker.Operator.Name}: `{GeneralUtilities.CalcKdr(attacker.Kills, attacker.Deaths)}`")
                Next
                embed.AddField("Highest Attacker KDR", strBuilder.ToString, True)
                strBuilder.Clear()

                For Each defender In topKdDef
                    strBuilder.AppendLine($"{defender.Operator.Name}: `{GeneralUtilities.CalcKdr(defender.Kills, defender.Deaths)}`")
                Next
                embed.AddField("Highest Defender KDR", strBuilder.ToString, True)
                strBuilder.Clear()

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("weapons"), Aliases("weap", "w")>
            <Description("Displays basic weapon stats for the specified player.")>
            Public Async Function SiegeWeaponsCommand(ctx As CommandContext, platform As String, <RemainingText> username As String) As Task
                Dim player = Await GetR6StatsPlayer(ctx, username, platform)
                If player Is Nothing Then Return

                Dim weaponReq = GeneralUtilities.GetJson($"https://r6stats.com/api/stats/{player.UbisoftId}/weapons")
                If weaponReq.json Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Orange,
                        .Title = "Unable To Retrieve Stats",
                        .Description = $"`{weaponReq.status}` {R6SErrorMessage}"
                    })
                    Return
                End If

                Dim jsonSettings As New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore}
                Dim weaponStats = JsonConvert.DeserializeObject(Of R6StatsWeaponData)(weaponReq.json, jsonSettings)

                Dim filteredWeps = weaponStats.Weapons.Where(Function(w) Not w.Weapon.Category.Name = "Handgun")
                Dim favWeapons = filteredWeps.OrderByDescending(Function(w) w.TimesChosen).Take(5)
                Dim kdrWeapons = filteredWeps.OrderByDescending(Function(w) w.Kd).Take(5)

                Dim embed = InitEmbedBuilder(player, platform, weaponStats.LastUpdated.ToLocalTime)
                Dim strBuilder As New StringBuilder

                For Each weaponStat In favWeapons
                    strBuilder.AppendLine($"{weaponStat.Weapon.Name}: `{weaponStat.TimesChosen.ToString("N0")} Times`")
                Next
                embed.AddField("Most Chosen Weapons", strBuilder.ToString)
                strBuilder.Clear()

                For Each weaponStat In kdrWeapons
                    strBuilder.AppendLine($"{weaponStat.Weapon.Name}: `{GeneralUtilities.CalcKdr(weaponStat.Kills, weaponStat.Deaths)} KDR`")
                Next
                embed.AddField("Top KDR Weapons", strBuilder.ToString)
                strBuilder.Clear()

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            Private Function InitEmbedBuilder(player As R6StatsLookupResult, platform As String, updatedTime As Date) As DiscordEmbedBuilder
                Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.SpringGreen}
                embed.WithAuthor($"{player.Username} • Level {player.Level} • {platform.ToUpper}",
                                 $"https://r6stats.com/stats/{player.UbisoftId}/",
                                 $"https://ubisoft-avatars.akamaized.net/{player.UbisoftId}/default_146_146.png")
                embed.WithFooter($"Rainbow Six Siege  •  Updated {(Date.Now - updatedTime).Humanize(2)} ago",
                                 "https://i.imgur.com/F8SVRpS.png")
                Return embed
            End Function

            Private Async Function GetR6StatsPlayer(ctx As CommandContext, username As String, platform As String) As Task(Of R6StatsLookupResult)
                If Not _platforms.Contains(platform.ToUpper) Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                         .Color = DiscordColor.Red,
                         .Title = "Invalid Platform",
                         .Description = $"The platform you provided was not a valid platform.{Environment.NewLine}Valid platforms: {String.Join(", ", _platforms)}"
                    })
                    Return Nothing
                End If

                Await ctx.TriggerTypingAsync

                Dim lookupReq = GeneralUtilities.GetJson($"https://r6stats.com/api/player-search/{Uri.EscapeUriString(username)}/{platform}")
                If lookupReq.json Is Nothing Then
                    If lookupReq.status = HttpStatusCode.NotFound Then
                        Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                             .Color = DiscordColor.Red,
                             .Title = "Player Not Found",
                             .Description = R6SPlayerNotFoundMessage
                        })
                        Return Nothing
                    End If

                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Orange,
                        .Title = "Unable To Retrieve Stats",
                        .Description = $"`{lookupReq.status}` {R6SErrorMessage}"
                    })
                    Return Nothing
                End If

                Dim jsonSettings As New JsonSerializerSettings With {.NullValueHandling = NullValueHandling.Ignore}
                Dim players = JsonConvert.DeserializeObject(Of List(Of R6StatsLookupResult))(lookupReq.json, jsonSettings)
                Dim player = players.FirstOrDefault(Function(r) r.Username.ToLower = username.ToLower)

                If player Is Nothing Then
                    Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Title = "Player Not Found",
                        .Description = R6SPlayerNotFoundMessage
                    })
                    Return Nothing
                End If

                Return player
            End Function
        End Class
    End Class
End Namespace