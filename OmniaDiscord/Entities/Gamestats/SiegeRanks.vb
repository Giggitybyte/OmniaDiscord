Namespace Entities.Gamestats
    Public Structure SiegeRanks
        Private Shared _ranksSeason5 As New List(Of String) From {
            "Unranked",
            "Copper Ⅳ",
            "Copper Ⅲ",
            "Copper Ⅱ",
            "Copper Ⅰ",
            "Bronze Ⅳ",
            "Bronze Ⅲ",
            "Bronze Ⅱ",
            "Bronze Ⅰ",
            "Silver Ⅳ",
            "Silver Ⅲ",
            "Silver Ⅱ",
            "Silver Ⅰ",
            "Gold Ⅳ",
            "Gold Ⅲ",
            "Gold Ⅱ",
            "Gold Ⅰ",
            "Platinum Ⅲ",
            "Platinum Ⅱ",
            "Platinum Ⅰ",
            "Diamond"
        }

        Private Shared _ranksSeason15 As New List(Of String) From {
            "Unranked",
            "Copper Ⅴ",
            "Copper Ⅳ",
            "Copper Ⅲ",
            "Copper Ⅱ",
            "Copper Ⅰ",
            "Bronze Ⅴ",
            "Bronze Ⅳ",
            "Bronze Ⅲ",
            "Bronze Ⅱ",
            "Bronze Ⅰ",
            "Silver Ⅴ",
            "Silver Ⅳ",
            "Silver Ⅲ",
            "Silver Ⅱ",
            "Silver Ⅰ",
            "Gold Ⅲ",
            "Gold Ⅱ",
            "Gold Ⅰ",
            "Platinum Ⅲ",
            "Platinum Ⅱ",
            "Platinum Ⅰ",
            "Diamond",
            "Champion"
        }

        Public Shared Function GetNameFromId(seasonId As Integer, rankId As Integer) As String
            If seasonId >= 15 Then Return _ranksSeason15(rankId)
            Return _ranksSeason5.ElementAt(rankId)
        End Function

        Public Shared Function GetRankIconUrl(rankId As Integer) As String
            Throw New NotImplementedException

            'Dim baseUrl = "https://game-rainbow6.ubi.com/"
            'Dim ranksObject = JObject.Parse(Utilities.GetJson($"{baseUrl}assets/data/ranks.e749eb368.json"))
            'Dim season = JsonConvert.DeserializeObject(Of SiegeRankedData)(ranksObject.SelectToken("$.seasons[-1:]").ToString())

            'Return $"{baseUrl}{season.Ranks(rankId).Images.HighResUrl}"
        End Function

    End Structure
End Namespace