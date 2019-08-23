Imports System.IO
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Utilities

Namespace Commands.Modules
    Public Class EmojiModule
        Inherits BaseCommandModule

        <Command("emoji")>
        <Description("Displays a larger version of an emoji.")>
        Public Async Function GetEmojiCommand(ctx As CommandContext, emoji As DiscordEmoji) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.CornflowerBlue}
            Await ctx.TriggerTypingAsync

            If emoji.Id = 0 Then ' Unicode emoji.
                Dim emojiUtf32Hex As String = Char.ConvertToUtf32(emoji.Name, 0).ToString("X4")
                Dim fileName As String = GeneralUtilities.GenerateRandomChars(16)
                Dim image As Stream = Await GeneralUtilities.SvgToStreamAsync($"https://twemoji.maxcdn.com/2/svg/{emojiUtf32Hex.ToLower}.svg")

                embed.ImageUrl = $"attachment://{fileName}.png"
                Await ctx.RespondWithFileAsync($"{fileName}.png", image, embed:=embed.Build)

                image.Dispose()

            Else ' Guild emote.
                embed.ImageUrl = emoji.Url
                Await ctx.RespondAsync(embed:=embed.Build)
            End If
        End Function

    End Class
End Namespace