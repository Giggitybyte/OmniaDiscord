Namespace Entities.Gamestats
    Public Structure SiegeRanks
        Private Shared _ranks As New List(Of String) From {
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
            "Diamond",
            "Top Player"
        }

        Public Shared Function GetNameFromId(rankId As Integer) As String
            Return _ranks.ElementAt(rankId)
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