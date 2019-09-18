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

        Private Shared ReadOnly _numeralDictionary As New Dictionary(Of String, Integer) From {
            {"Ⅴ", 5},
            {"Ⅳ", 4},
            {"Ⅲ", 3},
            {"Ⅱ", 2},
            {"Ⅰ", 1}
        }

        Public Shared Function GetNameFromId(seasonId As Integer, rankId As Integer) As String
            If seasonId >= 15 Then Return _ranksSeason15(rankId)
            Return _ranksSeason5.ElementAt(rankId)
        End Function

        Public Shared Function GetRankIconUrl(seasonId As Integer, rankId As Integer) As String
            Dim rankName = GetNameFromId(seasonId, rankId)

            If rankName.Contains(" "c) Then
                rankName = rankName.Replace(" "c, "-"c).Replace(rankName.Last, _numeralDictionary(rankName.Last))
            End If

            Return $"{Bot.Config.ResourceUrl}/assets/siege/ranks/{rankName.ToLower}.png"
        End Function
    End Structure
End Namespace