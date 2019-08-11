Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Interactivity
Imports DSharpPlus.Lavalink
Imports Humanizer
Imports Myrmec
Imports OmniaDiscord.Entities.Media
Imports OmniaDiscord.Services

Namespace Utilities
    Public Class MediaUtilities

        ' TODO: clean this function up.
        Public Shared Async Function QueueMediaAsync(ctx As CommandContext, lavalink As LavalinkService, media As OmniaMediaInfo) As Task(Of DiscordEmbedBuilder)
            Dim embed As New DiscordEmbedBuilder

            If media Is Nothing Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Cannot Retrieve Media"
                    .Description = "The provided URL was either from an unsupported platform or is inaccessible for me."
                End With

            Else ' Build embed, join voice channel, queue and play media.
                With embed
                    .Color = DiscordColor.SpringGreen
                    .Footer = New DiscordEmbedBuilder.EmbedFooter With {
                        .Text = media.Origin
                    }
                End With

                Dim guildConnection As LavalinkGuildConnection = Await lavalink.Node.ConnectAsync(ctx.Member.VoiceState.Channel)

                If media.Type = OmniaMediaType.Track Then
                    media.Requester = ctx.Member.Id
                    lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Enqueue(media)

                    With embed
                        .Title = "Queued Track"
                        .Description = $"**[{media.Title}]({media.Url})**{Environment.NewLine}{media.Author}"
                        .ThumbnailUrl = media.ThumbnailUrl

                        If media.Duration.TotalSeconds > 0 Then .Description &= $"{Environment.NewLine}*{media.Duration.Humanize(2)}*"
                    End With

                ElseIf media.Type = OmniaMediaType.Album Or media.Type = OmniaMediaType.Playlist Then
                    For Each track As OmniaMediaInfo In media.Tracks
                        track.Requester = ctx.Member.Id
                        lavalink.GuildInfo(ctx.Guild.Id).MediaQueue.Enqueue(track)
                    Next

                    With embed
                        .Title = $"Queued {media.Tracks.Count} Tracks"

                        .Description = $"**[{media.Title}]({media.Url})**{Environment.NewLine}"
                        .Description &= $"{media.Author}{Environment.NewLine}{Environment.NewLine}"
                        .Description &= $"Total Playtime: {media.Duration.Humanize(2)}"

                        .ThumbnailUrl = media.ThumbnailUrl
                        .Footer.Text &= $" {media.Type}"
                    End With
                End If

                If guildConnection IsNot Nothing AndAlso guildConnection.IsConnected Then
                    If lavalink.GuildInfo(ctx.Guild.Id).CurrentTrack Is Nothing Then
                        ' TODO: Make this less fucky and log all tracks that fucked up.
                        Dim isSuccess As Boolean

                        Do
                            isSuccess = Await lavalink.PlayNextTrackAsync(ctx.Guild)
                        Loop Until isSuccess
                    End If
                End If
            End If

            Return embed
        End Function

        Public Shared Async Function DoQueuePaginationAsync(ctx As CommandContext, pages As List(Of String), timeout As Integer) As Task
            Dim tsc As New TaskCompletionSource(Of String)
            Dim ct As New CancellationTokenSource(timeout)
            ct.Token.Register(Sub() tsc.TrySetResult(Nothing))

            Dim pageNumber As Integer = 1
            Dim message As DiscordMessage = Await ctx.RespondAsync(Formatter.BlockCode(pages(pageNumber - 1), "markdown"))
            Dim emojis As New PaginationEmojis()

            AddPaginationEmojis(message, emojis)

            Dim handler = Async Function(e)
                              If TypeOf e Is MessageReactionAddEventArgs Or TypeOf e Is MessageReactionRemoveEventArgs Then
                                  If e.Message.Id = message.Id AndAlso e.User.Id <> ctx.Client.CurrentUser.Id AndAlso e.User.Id = ctx.Member.Id Then
                                      Dim emoji As DiscordEmoji = DirectCast(e.Emoji, DiscordEmoji)

                                      ct.Dispose()
                                      ct = New CancellationTokenSource(timeout)
                                      ct.Token.Register(Sub() tsc.TrySetResult(Nothing))


                                      Select Case emoji
                                          Case emojis.SkipLeft
                                              pageNumber = 1

                                          Case emojis.Left
                                              If pageNumber <> 1 Then pageNumber -= 1

                                          Case emojis.Stop
                                              ct.Cancel()

                                          Case emojis.Right
                                              If pageNumber <> pages.Count Then pageNumber += 1

                                          Case emojis.SkipRight
                                              pageNumber = pages.Count

                                          Case Else
                                              Return
                                      End Select

                                  Else
                                      Return
                                  End If

                              ElseIf TypeOf e Is MessageReactionsClearEventArgs Then
                                  AddPaginationEmojis(message, emojis)
                              End If

                              If ct.IsCancellationRequested = False Then
                                  Await message.ModifyAsync(Formatter.BlockCode(pages(pageNumber - 1), "markdown"))
                              End If
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

        Public Shared Function IsPlayableFileType(header As Byte()) As Boolean
            Dim fileSniffer As New Sniffer
            Dim allowedFiles As New List(Of Record) From {
                New Record("mp3", "FF FB", "MPEG-1 Layer 3 - ID3v1"),
                New Record("mp3", "49 44 33", "MPEG-1 Layer 3 - ID3v2"),
                New Record("mp4", "00 00 00 18 66 74 79 70 6D 70 34 32", "MPEG-4"),
                New Record("mp4", "00 00 00 20 66 74 79 70 69 73 6F 6D", "MPEG-4"),
                New Record("m4a", "00 00 00 20 66 74 79 70 4D 34 41", "MPEG 4 Audio"),
                New Record("flac", "66 4C 61 43", "Free Lossless Audio Codec"),
                New Record("ogg ogv oga", "4F 67 67 53", "Ogg Container Format"),
                New Record("wav", "52 49 46 46 ?? ?? ?? ?? 57 41 56 45", "Waveform Audio File Format "),
                New Record("mkv mka webm", "1A 45 DF A3", "Matroska + WebM")
            }

            fileSniffer.Populate(allowedFiles)
            Dim results = fileSniffer.Match(header, True)

            Return results.Any
        End Function

        Public Shared Async Sub AddPaginationEmojis(message As DiscordMessage, emojis As PaginationEmojis)
            Await message.CreateReactionAsync(emojis.SkipLeft)
            Await message.CreateReactionAsync(emojis.Left)
            Await message.CreateReactionAsync(emojis.Stop)
            Await message.CreateReactionAsync(emojis.Right)
            Await message.CreateReactionAsync(emojis.SkipRight)
        End Sub

    End Class
End Namespace