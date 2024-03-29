﻿Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Utilities

Namespace Commands.Modules
    Public Class EmojiModule
        Inherits BaseCommandModule

        <Command("emoji"), RequireGuild>
        <Description("Displays a larger version of an emoji. " + vbCrLf + " Both unicode emojis and guild emotes are supported.")>
        <RequireBotPermissions(Permissions.EmbedLinks)>
        <Cooldown(1, 5, CooldownBucketType.Channel)>
        Public Async Function GetEmojiCommand(ctx As CommandContext, emoji As DiscordEmoji) As Task
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.CornflowerBlue}

            If Not emoji.Id = 0 Then
                embed.ImageUrl = emoji.Url
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Await ctx.TriggerTypingAsync
            Dim emojiUtf32Hex = Char.ConvertToUtf32(emoji.Name, 0).ToString("X4")
            Dim fileName = GeneralUtilities.GenerateRandomChars(16)
            Dim image = Await GeneralUtilities.SvgToStreamAsync($"https://twemoji.maxcdn.com/2/svg/{emojiUtf32Hex.ToLower}.svg")

            embed.ImageUrl = $"attachment://{fileName}.png"
            Await ctx.RespondWithFileAsync($"{fileName}.png", image, embed:=embed.Build)

            image.Dispose()
        End Function
    End Class
End Namespace