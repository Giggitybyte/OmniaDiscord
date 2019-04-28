﻿Imports System.IO
Imports System.Net
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
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
        <Description("Retrieves player stats for Siege. Valid platforms are PC, PSN, and XBL. Valid regions are NA, EU, and AS.")>
        <Cooldown(1, 5, CooldownBucketType.User)>
        Public Async Function RainbowSixSiege(ctx As CommandContext, platform As String, region As String, <RemainingText> username As String) As Task
            Dim baseUrl As String = "http://r6stats.com/api/stats/"
            Dim jsonSettings As New JsonSerializerSettings With {
                .NullValueHandling = NullValueHandling.Ignore,
                .MissingMemberHandling = MissingMemberHandling.Ignore,
            }

            Dim stats As SiegeStats = JsonConvert.DeserializeObject(Of SiegeStats)(GetJson($"{baseUrl}/06458532-d978-4739-8d95-870ea4b7c4d6"), jsonSettings)
            Dim seaons As SiegeSeasons = JsonConvert.DeserializeObject(Of SiegeSeasons)(GetJson($"{baseUrl}/06458532-d978-4739-8d95-870ea4b7c4d6/seasonal"), jsonSettings)

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