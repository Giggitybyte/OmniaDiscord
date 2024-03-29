﻿Imports System.IO
Imports System.IO.Compression
Imports System.Net
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Utilities

Namespace Commands.Modules
    Public Class GetFilesModule
        Inherits BaseCommandModule

        <Command("getfiles"), RequireGuild>
        <Description("Searches through the last 1,000 messages in a Discord channel and retrieves all files from messages with an attachment. " + vbCrLf + " Messages from bots are ignored.")>
        <RequireBotPermissions(Permissions.EmbedLinks Or Permissions.UseExternalEmojis)>
        <Cooldown(1, 30, CooldownBucketType.Guild)>
        Public Async Function GetFiles(ctx As CommandContext, Optional channel As DiscordChannel = Nothing) As Task
            If channel Is Nothing Then channel = ctx.Channel
            If channel.Type <> ChannelType.Text Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Title = "Invalid Channel Type",
                    .Description = $"Scraping can only be done from a text channel, not a {channel.Type.ToString.ToLower} channel."
                })
                Return
            End If

            Dim embed As New DiscordEmbedBuilder
            With embed
                .Color = DiscordColor.Orange
                .Title = "Please Wait..."
                .Description = $"Messages from `{channel.Name}` are being retrieved and sorted.{Environment.NewLine}This may take a while."
            End With

            Dim waitMessage = Await ctx.RespondAsync(embed:=embed.Build)
            Await ctx.TriggerTypingAsync

            Dim messages = Await channel.GetMessagesBeforeAsync(ctx.Message.Id, 1000)
            Dim filteredMessages As New List(Of DiscordMessage)

            For Each message In messages
                If message.Attachments.Any AndAlso message.Author.IsBot = False Then filteredMessages.Add(message)
            Next

            If filteredMessages.Count = 0 Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Retrieval Failed"
                    .Description = "There were no messages found with attachments."
                End With

                Await waitMessage.DeleteAsync
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim attachmentCount As Integer
            Dim failureCount As Integer
            Dim downloadPath = $"{Environment.CurrentDirectory}/Temp/{channel.Id}"
            Dim webClient As New WebClient
            Directory.CreateDirectory(downloadPath)

            embed.Description = $"Download of files is in progress.{Environment.NewLine}This may take several minutes."
            Await waitMessage.ModifyAsync(embed:=embed.Build)
            Await ctx.TriggerTypingAsync

            For Each message In filteredMessages
                For Each attachment In message.Attachments
                    attachmentCount += 1

                    Try
                        Dim extenstion As String = attachment.FileName.Substring(attachment.FileName.LastIndexOf("."c)).ToLower
                        Await webClient.DownloadFileTaskAsync(attachment.Url, $"{downloadPath}/{GeneralUtilities.GenerateRandomChars(16)}{extenstion}")
                    Catch ex As WebException
                        failureCount += 1
                    End Try
                Next
            Next

            embed.Description = $"File compression is in progress.{Environment.NewLine}This might be a moment."
            Await waitMessage.ModifyAsync(embed:=embed.Build)
            Await ctx.TriggerTypingAsync

            Dim zipFileName = $"{channel.Id}-files.zip"
            ZipFile.CreateFromDirectory(downloadPath, zipFileName, CompressionLevel.Optimal, False)
            Directory.Delete(downloadPath, True)

            With embed
                .Color = DiscordColor.SpringGreen
                .Title = "Retrieval Completed"

                If failureCount > 0 Then
                    .Description = $"Out of {attachmentCount} files, {attachmentCount - failureCount} files were successfully retrieved."
                Else
                    .Description = $"{attachmentCount} files were successfully retrieved."
                End If
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
            Await waitMessage.DeleteAsync

            If String.IsNullOrEmpty(zipFileName) = False Then
                Dim emoji As DiscordEmoji = DiscordEmoji.FromName(ctx.Client, ":omnia_loading:")
                Dim message As DiscordMessage = Await ctx.RespondAsync($"Uploading compressed file... {emoji}")
                Dim response As Byte() = Await webClient.UploadFileTaskAsync("https://x0.at/", zipFileName)
                Dim fileUrl = Text.Encoding.ASCII.GetString(response)

                Await message.ModifyAsync($"Files from `{channel.Name}`: <{fileUrl}>")
                File.Delete(zipFileName)
            End If
        End Function
    End Class
End Namespace