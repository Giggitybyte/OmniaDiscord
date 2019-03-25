Imports System.Security.Cryptography
Imports System.Text
Imports DSharpPlus.Entities

Public Class Utilities

    Public Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

    Public Shared Function GenerateRandomChars(maxSize As Integer) As String
        ' https://stackoverflow.com/a/1344255

        Dim chars As Char() = New Char(61) {}
        chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray()
        Dim data As Byte() = New Byte(0) {}

        Using crypto As RNGCryptoServiceProvider = New RNGCryptoServiceProvider()
            crypto.GetNonZeroBytes(data)
            data = New Byte(maxSize - 1) {}
            crypto.GetNonZeroBytes(data)
        End Using

        Dim result As StringBuilder = New StringBuilder(maxSize)

        For Each b As Byte In data
            result.Append(chars(b Mod (chars.Length)))
        Next

        Return result.ToString()
    End Function

    ' TODO: actually make this useful.
    Public Shared Function TagToTextConverter(user As DiscordMember, message As String, Optional embedSafe As Boolean = False) As String
        Dim tagList As New Dictionary(Of String, String) From {
            {"%user%", user.DisplayName},
            {"%usermention%", If(embedSafe, $"@{user.DisplayName}", user.Mention)},
            {"%server%", user.Guild.Name}
        }

        ' TODO: escape stuff that could break an embed or codeblock.
        For Each tag As KeyValuePair(Of String, String) In tagList
            message = message.Replace(tag.Key, tag.Value, StringComparison.CurrentCultureIgnoreCase)
        Next

        Return message
    End Function

    Public Shared Function FixedWidthText(text As String, targetLength As Integer) As String
        If text Is Nothing Then
            Return String.Empty

        ElseIf text.Length < targetLength Then
            Return text.PadRight(targetLength, " "c)

        ElseIf text.Length > targetLength Then
            Return text.Substring(0, targetLength)

        Else
            Return text
        End If
    End Function

    Public Shared Function FormatTimespanToString(time As TimeSpan, Optional notRestrictedToHours As Boolean = False) As String
        If notRestrictedToHours Then
            If (Math.Floor(time.Days / 365) <> 0) Then

                Return $"{(time.Days / 365).ToString("N0")}y {((time.Days Mod 365) / 30).ToString("N0")}m"
            End If

            If (Math.Floor(time.Days / 30) <> 0) Then
                Return $"{(time.Days / 30).ToString("N1")}m"
            End If

            If time.Days > 0 Then
                If time.Hours > 0 Then
                    Return time.ToString("d\d\ h\h")
                Else
                    Return time.ToString("d\d")
                End If
            End If
        Else
            If time.Days > 0 Then Return $"{time.TotalHours.ToString("N0")}h {time.Minutes.ToString}m"
        End If

        If time.Hours > 0 Then
            If time.Minutes > 0 Then
                Return time.ToString("h\h\ m\m")
            Else
                Return time.ToString("h\h")
            End If
        End If

        If time.Minutes > 0 Then
            If time.Seconds > 0 Then
                Return time.ToString("m\m\ s\s")
            Else
                Return time.ToString("m\m")
            End If
        Else
            Return time.ToString("s\.f\s")
        End If
    End Function

    Public Shared Function CombineTimespans(timespans As IEnumerable(Of TimeSpan)) As String
        Dim total As New TimeSpan
        total = timespans.Aggregate(total, Function(current, timespan) current.Add(timespan))

        Return FormatTimespanToString(total)
    End Function

End Class