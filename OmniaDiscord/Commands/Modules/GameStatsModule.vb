Imports System.IO
Imports System.Net
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports OmniaDiscord.Entities
Imports Overstarch
Imports Overstarch.Entities
Imports Overstarch.Enums
Imports Overstarch.Extensions
Namespace Commands.Modules

    <Group("gamestats"), Aliases("gs")>
    <Description("Command group for the retrival of player stats for several popular multiplayer games.")>
    <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.UseExternalEmojis)>
    Public Class GameStatsModule
        Inherits OmniaBaseModule

#Region "Commands"
        <Command("siege"), Aliases("r6s", "r6")>
        <Description("Retrieves player stats for Siege. Valid platforms are PC, PSN, and XBL. Valid regions are NA, EU, and AS.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function RainbowSixSiege(ctx As CommandContext, platform As String, region As String, <RemainingText> username As String) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim validRegions As New Dictionary(Of String, String) From {
                {"NA", "ncsa"},
                {"EU", "emea"},
                {"AS", "apac"}
            }
            Dim validPlatforms As New Dictionary(Of String, String) From {
                {"PC", "uplay"},
                {"XBL", "xbl"},
                {"PSN", "psn"}
            }

            Await ctx.Channel.TriggerTypingAsync

            If validPlatforms.ContainsKey(platform.ToUpper) = False Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Platform"
                    .Description = $"The platform you provided was not a valid platform.{Environment.NewLine}Valid platforms: {String.Join(", ", validPlatforms.Keys)}"
                End With

            ElseIf validRegions.ContainsKey(region.ToUpper) = False Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Region"
                    .Description = $"The region you provided was not a valid region.{Environment.NewLine}Valid regions: {String.Join(", ", validRegions.Keys)}"
                End With
            Else
                Dim userJson As JObject = JObject.Parse(GetJson($"{OmniaConfig.ResourceUrl}/api/r6/getUser.php?name={username}&platform={validPlatforms(platform.ToUpper)}&region={validRegions(region.ToUpper)}&appcode={OmniaConfig.RainbowSixApiPasscode}"))
                Dim player As SiegePlayer = JsonConvert.DeserializeObject(Of SiegePlayer)(userJson.First.First.First.First.ToString)

                If player.UbisoftId Is Nothing Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "User Not Found"
                        .Description = $"No user could be found with the input you provided.{Environment.NewLine}Make sure you have the correct username, region, and platform."
                    End With
                Else
                    Dim statsjson As JObject = JObject.Parse(GetJson($"{OmniaConfig.ResourceUrl}/api/r6/getStats.php?name={username}&platform={validPlatforms(platform.ToUpper)}&appcode={OmniaConfig.RainbowSixApiPasscode}"))

                    player.RankedCurrent = JsonConvert.DeserializeObject(Of RankedCurrent)(userJson.First.First.First.First.ToString)
                    player.RankedOverall = JsonConvert.DeserializeObject(Of RankedOverall)(statsjson.First.First.First.First.ToString)
                    player.CasualOverall = JsonConvert.DeserializeObject(Of CasualOverall)(statsjson.First.First.First.First.ToString)
                    player.GeneralStats = JsonConvert.DeserializeObject(Of GeneralStats)(statsjson.First.First.First.First.ToString)

                    With embed
                        Dim strBuilder As New StringBuilder

                        .Color = DiscordColor.SpringGreen
                        .ThumbnailUrl = player.RankedCurrent.Resources.Image

                        .Author = New DiscordEmbedBuilder.EmbedAuthor With {
                            .Name = $"{player.Username} - Level {player.ClearanceLevel} - {platform.ToUpper}",
                            .IconUrl = $"https://ubisoft-avatars.akamaized.net/{player.UbisoftId}/default_146_146.png?appId=39baebad-39e5-4552-8c25-2c9b919064e2",
                            .Url = $"https://game-rainbow6.ubi.com/en-us/uplay/player-statistics/{player.UbisoftId}/multiplayer"
                        }

                        .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                            .Text = $"Rainbow Six Siege",
                            .IconUrl = "https://i.imgur.com/F8SVRpS.png"
                        }

                        strBuilder.Append($"K/D Ratio: `{(player.RankedOverall.Kills / player.RankedOverall.Deaths).ToString("N2")}`{Environment.NewLine}")
                        strBuilder.Append($"Kills: `{player.RankedOverall.Kills.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Deaths: `{player.RankedOverall.Deaths.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"W/L Ratio: `{(player.RankedOverall.Wins / player.RankedOverall.Losses).ToString("N2")}`{Environment.NewLine}")
                        strBuilder.Append($"Wins: `{player.RankedOverall.Wins.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Losses: `{player.RankedOverall.Losses.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Time Played: `{Utilities.FormatTimespanToString(TimeSpan.FromSeconds(player.RankedOverall.Playtime))}`{Environment.NewLine}")

                        .AddField("Ranked Overall", strBuilder.ToString, True)
                        strBuilder.Clear()

                        strBuilder.Append($"Ranking: `{player.RankedCurrent.Resources.RankName}`{Environment.NewLine}")
                        strBuilder.Append($"Current MMR: `{CInt(Math.Round(player.RankedCurrent.CurrentMmr))}`{Environment.NewLine}")
                        strBuilder.Append($"Highest MMR: `{CInt(Math.Round(player.RankedCurrent.HighestMmr))}`{Environment.NewLine}")
                        strBuilder.Append($"Wins: `{player.RankedCurrent.Wins}`{Environment.NewLine}")
                        strBuilder.Append($"Losses: `{player.RankedCurrent.Losses}`{Environment.NewLine}")
                        strBuilder.Append($"Abandons: `{player.RankedCurrent.Abandons}`{Environment.NewLine}")
                        If player.RankedCurrent.Resources.RankName <> "Unranked" Then strBuilder.Append($"Next rank at: `{player.RankedCurrent.NextRankMmr} MMR`{Environment.NewLine}")

                        .AddField($"Current Ranked Season", strBuilder.ToString, True)
                        strBuilder.Clear()

                        strBuilder.Append($"K/D Ratio: `{(player.CasualOverall.Kills / player.CasualOverall.Deaths).ToString("N2")}`{Environment.NewLine}")
                        strBuilder.Append($"Kills: `{player.CasualOverall.Kills.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Deaths: `{player.CasualOverall.Deaths.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"W/L Ratio: `{(player.CasualOverall.Wins / player.CasualOverall.Losses).ToString("N2")}`{Environment.NewLine}")
                        strBuilder.Append($"Wins: `{player.CasualOverall.Wins.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Losses: `{player.CasualOverall.Losses.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Time Played: `{Utilities.FormatTimespanToString(TimeSpan.FromSeconds(player.CasualOverall.Playtime))}`{Environment.NewLine}")

                        .AddField("Casual Overall", strBuilder.ToString, True)
                        strBuilder.Clear()

                        strBuilder.Append($"Overall Playtime: `{Utilities.FormatTimespanToString(TimeSpan.FromSeconds(player.CasualOverall.Playtime + player.RankedOverall.Playtime))}`{Environment.NewLine}")
                        strBuilder.Append($"Headshots: `{player.GeneralStats.Headshots.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Assists: `{player.GeneralStats.KillAssists.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Melee Kills: `{player.GeneralStats.MeleeKills.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Penetration Kills: `{player.GeneralStats.PenetrationKills.ToString("N0")}`{Environment.NewLine}")
                        strBuilder.Append($"Bullet Accuracy: `{((player.GeneralStats.ShotsHit / player.GeneralStats.ShotsFired) * 100).ToString("N1")}%`{Environment.NewLine}")
                        strBuilder.Append($"Alpha Pack Chance: `{CInt(Math.Round(player.LootboxChance / 100))}%`")

                        .AddField("General Stats", strBuilder.ToString, True)
                        strBuilder.Clear()
                    End With
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("overwatch"), Aliases("ow")>
        <Description("Retrieves player stats for Overwatch. Valid platforms are PC, PSN, and XBL.")>
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
                            .Name = $"{owPlayer.Username} - Level {owPlayer.PlayerLevel} - {owPlayer.Platform.ToString}",
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

                            strBuilder.Append($"Time Played: `{Utilities.FormatTimespanToString(TimeSpan.FromSeconds(qpTimePlayed))}`{Environment.NewLine}")
                            strBuilder.Append($"Games Won: `{qpGamesWon.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"K/D Ratio: `{(qpElims / qpDeaths).ToString("N2")}`{Environment.NewLine}")
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

                            strBuilder.Append($"Time Played: `{Utilities.FormatTimespanToString(TimeSpan.FromSeconds(compTimePlayed))}`{Environment.NewLine}")
                            strBuilder.Append($"Games Played: `{compGamesPlayed.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"Games Won: `{compGamesWon.ToString("N0")}`{Environment.NewLine}")
                            strBuilder.Append($"K/D Ratio: `{(compElims / compDeaths).ToString("N2")}`{Environment.NewLine}")
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

            If sortedStats.Count > 0 Then
                For Each heroStat In sortedStats
                    Dim heroName As String = heroStat.Hero
                    Dim playtime As String = Utilities.FormatTimespanToString(TimeSpan.FromSeconds(heroStat.Value))

                    strBuilder.Append($"{DiscordEmoji.FromName(client, $":omnia_{heroStat.Hero.ToLower}icon:")}{heroName}: `{playtime}`{Environment.NewLine}")
                Next
            End If
        End Sub

        Private Function GetJson(url As String) As String
            Dim request As HttpWebRequest
            Dim response As HttpWebResponse = Nothing
            Dim reader As StreamReader
            Dim rawjson As String = Nothing

            request = CType(WebRequest.Create(url), HttpWebRequest)
            request.UserAgent = "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/29.0.1547.2 Safari/537.36"
            request.Timeout = 12500

            Try
                response = CType(request.GetResponse(), HttpWebResponse)
                reader = New StreamReader(response.GetResponseStream())
                rawjson = reader.ReadToEnd()
                reader.Close()
            Catch webEx As WebException
                Return Nothing
            End Try

            Return rawjson
        End Function
#End Region

    End Class
End Namespace