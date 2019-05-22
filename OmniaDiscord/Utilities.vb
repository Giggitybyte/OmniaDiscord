Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports SkiaSharp
Imports SKSvg = SkiaSharp.Extended.Svg.SKSvg

Public Class Utilities

    Public Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
        target = value
        Return value
    End Function

    Public Shared Function GenerateRandomChars(length As Integer) As String
        ' https://stackoverflow.com/a/1344255

        Dim chars As Char() = New Char(61) {}
        chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890".ToCharArray()
        Dim data As Byte() = New Byte(0) {}

        Using crypto As RNGCryptoServiceProvider = New RNGCryptoServiceProvider()
            crypto.GetNonZeroBytes(data)
            data = New Byte(length - 1) {}
            crypto.GetNonZeroBytes(data)
        End Using

        Dim result As StringBuilder = New StringBuilder(length)

        For Each b As Byte In data
            result.Append(chars(b Mod (chars.Length)))
        Next

        Return result.ToString()
    End Function

    ''' <summary>
    ''' Downloads an SVG file from a URL, renders it, then returns it as a <see cref="Stream"/><para/>
    ''' Intended for usage with Discord embeds.
    ''' </summary>
    Public Shared Async Function SvgToStreamAsync(svgUrl As String, Optional width As Integer = 512, Optional height As Integer = 512) As Task(Of Stream)
        Dim svg As SKSvg = New SKSvg
        Dim bitmap As New SKBitmap(width, height)
        Dim canvas As New SKCanvas(bitmap)
        Dim svgStream As Stream = New MemoryStream
        Dim imageStream As Stream

        Using wclient As New WebClient
            svgStream = Await wclient.OpenReadTaskAsync(svgUrl)
        End Using

        svg.Load(svgStream)
        svgStream.Dispose()

        Dim svgMaxSize As Single = MathF.Max(svg.Picture.CullRect.Width, svg.Picture.CullRect.Height)
        Dim canvasMinSize As Single = MathF.Max(width, height)
        Dim scaleSize As Single = canvasMinSize / svgMaxSize

        canvas.DrawPicture(svg.Picture, SKMatrix.MakeScale(scaleSize, scaleSize))

        imageStream = SKImage.FromBitmap(bitmap).Encode.AsStream
        imageStream.Position = 0

        bitmap.Dispose()
        canvas.Dispose()

        Return imageStream
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

    ''' <summary>
    ''' Takes a <see cref="TimeSpan"/> and converts it into human readable text.
    ''' </summary>
    Public Shared Function FormatTimespanToString(time As TimeSpan, Optional notRestrictedToHours As Boolean = False) As String
        If notRestrictedToHours Then
            If (Math.Floor(time.Days / 365) <> 0) Then
                Return $"{(time.Days / 365).ToString("N0")}y {((time.Days Mod 365) / 30).ToString("N0")}m"
            End If

            If (Math.Floor(time.Days / 30) <> 0) Then
                Return $"{(time.Days / 30).ToString("N1")}m"
            End If

            If time.Days > 0 Then
                If time.Hours > 0 Then Return time.ToString("d\d\ h\h")
                Return time.ToString("d\d")
            End If
        Else
            If time.Days > 0 Then Return $"{time.TotalHours.ToString("N0")}h {time.Minutes.ToString}m"
        End If

        If time.Hours > 0 Then
            If time.Minutes > 0 Then Return time.ToString("h\h\ m\m")
            Return time.ToString("h\h")
        End If

        If time.Minutes > 0 Then
            If time.Seconds > 0 Then Return time.ToString("m\m\ s\s")
            Return time.ToString("m\m")
        Else
            If time.Milliseconds > 0 Then Return time.ToString("s\.f\s")
            Return time.ToString("s\s")
        End If
    End Function

    ''' <summary>
    ''' Adds multiple timespans together.
    ''' </summary>
    Public Shared Function CombineTimespans(timespans As IEnumerable(Of TimeSpan)) As String
        Dim total As New TimeSpan
        total = timespans.Aggregate(total, Function(current, timespan) current.Add(timespan))

        Return FormatTimespanToString(total)
    End Function

    ''' <summary>
    ''' Retrieves the source code of a webpage. Intended to download JSON for deserialization.
    ''' </summary>
    Public Shared Function GetJson(url As String) As String
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

    Private Async Function DoEmbedPaginationAsync(ctx As CommandContext, embeds As Dictionary(Of DiscordEmoji, DiscordEmbed), Optional timeout As Integer = 30000) As Task
        Dim tsc As New TaskCompletionSource(Of String)
        Dim ct As New CancellationTokenSource(timeout)
        ct.Token.Register(Sub() tsc.TrySetResult(Nothing))

        Dim currentEmoji As DiscordEmoji = embeds.First.Key
        Dim emojis As New List(Of DiscordEmoji)
        Dim message As DiscordMessage = Await ctx.RespondAsync(embed:=embeds(currentEmoji))

        For Each embed In embeds
            emojis.Add(embed.Key)
            Await ctx.Message.CreateReactionAsync(embed.Key)
        Next

        Dim handler = Async Function(e)
                          If TypeOf e Is MessageReactionAddEventArgs Or TypeOf e Is MessageReactionRemoveEventArgs Then
                              If e.Message.Id = message.Id AndAlso e.User.Id <> ctx.Client.CurrentUser.Id AndAlso e.User.Id = ctx.Member.Id Then
                                  Dim emoji As DiscordEmoji = DirectCast(e.Emoji, DiscordEmoji)

                                  ct.Dispose()
                                  ct = New CancellationTokenSource(timeout)
                                  ct.Token.Register(Sub() tsc.TrySetResult(Nothing))

                                  If emojis.Contains(emoji) Then currentEmoji = emoji

                              Else
                                  Return
                              End If

                          ElseIf TypeOf e Is MessageReactionsClearEventArgs Then
                              For Each emoji In emojis
                                  Await ctx.Message.CreateReactionAsync(emoji)
                              Next
                          End If

                          If Not ct.IsCancellationRequested Then Await message.ModifyAsync(embed:=embeds(currentEmoji))
                      End Function

        Try
            AddHandler ctx.Client.MessageReactionAdded, handler
            AddHandler ctx.Client.MessageReactionRemoved, handler
            AddHandler ctx.Client.MessageReactionsCleared, handler

            Await tsc.Task.ConfigureAwait(False)
        Catch ex As Exception
            Throw

        Finally
            RemoveHandler ctx.Client.MessageReactionAdded, handler
            RemoveHandler ctx.Client.MessageReactionRemoved, handler
            RemoveHandler ctx.Client.MessageReactionsCleared, handler

        End Try

        ct.Dispose()
        Await message.DeleteAllReactionsAsync
    End Function

End Class