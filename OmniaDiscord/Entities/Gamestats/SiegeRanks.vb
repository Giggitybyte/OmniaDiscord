Namespace Entities.Gamestats
    Public Structure SiegeRanks
        Private Shared _ranks As New List(Of (String, String)) From {
            ("Unranked", "https://i.imgur.com/jNJ1BBl.png"),
            ("Copper Ⅳ", "https://i.imgur.com/deTjm7V.png"),
            ("Copper Ⅲ", "https://i.imgur.com/zx5KbBO.png"),
            ("Copper Ⅱ", "https://i.imgur.com/RTCvQDV.png"),
            ("Copper Ⅰ", "https://i.imgur.com/SN55IoP.png"),
            ("Bronze Ⅳ", "https://i.imgur.com/DmfZeRP.png"),
            ("Bronze Ⅲ", "https://i.imgur.com/QOuIDW4.png"),
            ("Bronze Ⅱ", "https://i.imgur.com/ry1KwLe.png"),
            ("Bronze Ⅰ", "https://i.imgur.com/64eQSbG.png"),
            ("Silver Ⅳ", "https://i.imgur.com/fOmokW9.png"),
            ("Silver Ⅲ", "https://i.imgur.com/e84XmHl.png"),
            ("Silver Ⅱ", "https://i.imgur.com/f68iB99.png"),
            ("Silver Ⅰ", "https://i.imgur.com/iQGr0yz.png"),
            ("Gold Ⅳ", "https://i.imgur.com/DelhMBP.png"),
            ("Gold Ⅲ", "https://i.imgur.com/5fYa6cM.png"),
            ("Gold Ⅱ", "https://i.imgur.com/7c4dBTz.png"),
            ("Gold Ⅰ", "https://i.imgur.com/cOFgDW5.png"),
            ("Platinum Ⅲ", "https://i.imgur.com/to1cRGC.png"),
            ("Platinum Ⅱ", "https://i.imgur.com/vcIEaEz.png"),
            ("Platinum Ⅰ", "https://i.imgur.com/HAU5DLj.png"),
            ("Diamond Ⅰ", "https://i.imgur.com/Rt6c2om.png")
        }

        Public Shared Function GetRankFromId(rankId As Integer) As (rankName As String, url As String)
            Return _ranks.ElementAt(rankId)
        End Function

        Public Shared Function GetRankFromMmr(mmr As Integer) As (rankName As String, url As String)
            Select Case mmr
                Case <= 1399
                    Return _ranks.ElementAt(1)

                Case 1400 To 1499
                    Return _ranks.ElementAt(2)

                Case 1500 To 1599
                    Return _ranks.ElementAt(3)

                Case 1600 To 1699
                    Return _ranks.ElementAt(4)

                Case 1700 To 1799
                    Return _ranks.ElementAt(5)

                Case 1800 To 1899
                    Return _ranks.ElementAt(6)

                Case 1900 To 1999
                    Return _ranks.ElementAt(7)

                Case 2000 To 2099
                    Return _ranks.ElementAt(8)

                Case 2100 To 2199
                    Return _ranks.ElementAt(9)

                Case 2200 To 2299
                    Return _ranks.ElementAt(10)

                Case 2300 To 2399
                    Return _ranks.ElementAt(11)

                Case 2400 To 2499
                    Return _ranks.ElementAt(12)

                Case 2500 To 2699
                    Return _ranks.ElementAt(13)

                Case 2700 To 2899
                    Return _ranks.ElementAt(14)

                Case 2900 To 3099
                    Return _ranks.ElementAt(15)

                Case 3100 To 3299
                    Return _ranks.ElementAt(16)

                Case 3300 To 3699
                    Return _ranks.ElementAt(17)

                Case 3700 To 4099
                    Return _ranks.ElementAt(18)

                Case 4100 To 4499
                    Return _ranks.ElementAt(19)

                Case >= 4500
                    Return _ranks.ElementAt(20)

                Case Else
                    Return _ranks.ElementAt(0)
            End Select

        End Function

    End Structure

End Namespace