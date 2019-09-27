Imports DSharpPlus.Entities
Imports DSharpPlus.Interactivity
Imports Myrmec

Namespace Utilities
    Public Class MediaPlaybackUtilities
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