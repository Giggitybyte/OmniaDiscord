Imports System.IO
Imports System.Net
Imports System.Security.Cryptography
Imports System.Text
Imports System.Threading
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Interactivity
Imports SkiaSharp
Imports SKSvg = SkiaSharp.Extended.Svg.SKSvg

Namespace Utilities

    ''' <summary>
    ''' A collection of methods that have use across multiple classes.
    ''' </summary>
    Public Class GeneralUtilities

        Public Shared Function InlineAssignHelper(Of T)(ByRef target As T, value As T) As T
            target = value
            Return value
        End Function

        ' https://stackoverflow.com/a/1344255
        Public Shared Function GenerateRandomChars(length As Integer) As String
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

        ' Downloads an SVG file from a URL, renders it, then returns it as a Stream.
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

        ' Retrieves the source code of a webpage. Intended to download JSON for deserialization.
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

        Public Shared Async Function GetUserConfirmationAsync(ctx As CommandContext) As Task(Of Boolean)
            Dim interactivity As InteractivityExtension = ctx.Client.GetInteractivity
            Dim conformationCode As String = GenerateRandomChars(8)
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.Yellow
                .Title = "Action Confirmation"
                .Description = $"Please be sure you want to go through with this action.{Environment.NewLine}Respond with the following confirmation code to complete this action.{Environment.NewLine}```{conformationCode}```"
            End With

            Dim confirmationMessage As DiscordMessage = Await ctx.RespondAsync(embed:=embed.Build)
            Dim message As InteractivityResult(Of DiscordMessage) = Await interactivity.WaitForMessageAsync(Function(m)
                                                                                                                If Not m.Author = ctx.Message.Author Then Return False
                                                                                                                Return m.Content.Trim = conformationCode
                                                                                                            End Function,
                                                                                                            TimeSpan.FromSeconds(30))
            Await confirmationMessage.DeleteAsync

            If message.Result Is Nothing Then
                With embed
                    .Color = DiscordColor.Orange
                    .Title = "Timed Out"
                    .Description = "The confirmation code was not entered in time."
                End With

                Await ctx.RespondAsync(embed:=embed.Build)
                Return False
            End If

            Return True
        End Function

        Public Shared Async Function DoEmbedPaginationAsync(ctx As CommandContext, embeds As Dictionary(Of DiscordEmoji, DiscordEmbed), Optional timeout As Integer = 30000) As Task
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
End Namespace