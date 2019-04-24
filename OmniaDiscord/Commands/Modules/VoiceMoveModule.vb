Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports DSharpPlus.Entities.DiscordEmbedBuilder
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Interactivity
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Entites.Database

Namespace Commands.Modules

    Public Class VoiceMoveModule
        Inherits BaseCommandModule

        <Command("movevoice"), Aliases("move", "vcm"), RequireGuild>
        <Description("Moves users from the voice channel you're currently in to another voice channel. If multiple destination voice channels are found, you'll have the option to choose from up to four of them. Partial voice channel names and channel IDs are accepted.")>
        <RequireBotPermissions(Permissions.SendMessages Or Permissions.EmbedLinks Or Permissions.AddReactions Or Permissions.MoveMembers)>
        <RequireTitle(GuildTitle.Helper)>
        <Cooldown(1, 5, CooldownBucketType.Guild)>
        Public Async Function MoveToVoiceChannel(ctx As CommandContext, <RemainingText> destination As String) As Task
            Dim embed As New DiscordEmbedBuilder

            If String.IsNullOrEmpty(destination) Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Destination"
                    .Description = "Destination cannot be nothing."
                End With

            Else
                Dim originChannel As DiscordChannel = ctx.Member.VoiceState?.Channel

                If originChannel Is Nothing Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Not In Voice Channel"
                        .Description = "You must be in a voice channel to use this command"
                    End With
                Else
                    Dim channelId As ULong
                    Dim matchingVoiceChannels As New List(Of DiscordChannel)

                    If destination.Length = 18 AndAlso ULong.TryParse(destination, channelId) Then
                        Dim voiceChannel As DiscordChannel = ctx.Guild.GetChannel(channelId)
                        If voiceChannel IsNot Nothing Then matchingVoiceChannels.Add(voiceChannel)
                    Else
                        For Each voiceChannel As DiscordChannel In (Await ctx.Guild.GetChannelsAsync).Where(Function(c) c.Type = ChannelType.Voice)
                            If voiceChannel.Name.ToLower.Contains(destination.ToLower) Then matchingVoiceChannels.Add(voiceChannel)
                        Next
                    End If

                    ' Remove origin channel if there are multiple matching channels. Fuck VB.
                    If matchingVoiceChannels.Count = 2 AndAlso matchingVoiceChannels.Contains(originChannel) Then matchingVoiceChannels.Remove(originChannel)

                    If matchingVoiceChannels.Count = 0 Then
                        With embed
                            .Title = "Destination Not Found"
                            .Description = "No matching voice channel could be found"
                            .Color = DiscordColor.Red
                        End With

                    ElseIf matchingVoiceChannels.Count = 1 Then
                        If originChannel.Id = matchingVoiceChannels.First.Id Then
                            With embed
                                .Color = DiscordColor.Red
                                .Title = "Invalid Destination"
                                .Description = "Destination channel cannot be the channel you're currently in"
                            End With
                        Else
                            Dim resultEmbed As DiscordEmbedBuilder = Await MoveUsersAsync(originChannel, matchingVoiceChannels.First)
                            embed = resultEmbed
                        End If

                    ElseIf matchingVoiceChannels.Count > 1 Then
                        Dim emojis As New List(Of DiscordEmoji) From {
                            DiscordEmoji.FromName(ctx.Client, ":one:"),
                            DiscordEmoji.FromName(ctx.Client, ":two:"),
                            DiscordEmoji.FromName(ctx.Client, ":three:"),
                            DiscordEmoji.FromName(ctx.Client, ":four:")
                        }

                        Dim vcListEmbed As New DiscordEmbedBuilder With {
                            .Title = "Multiple Voice Channels Found",
                            .Color = DiscordColor.Orange,
                            .Footer = New EmbedFooter With {
                                .Text = "Select voice channel you'd like below",
                                .IconUrl = "https://i.imgur.com/ZHrFe49.png"
                            }
                        }
                        If matchingVoiceChannels.Contains(originChannel) Then matchingVoiceChannels.Remove(originChannel)
                        If matchingVoiceChannels.Count > 4 Then matchingVoiceChannels = matchingVoiceChannels.OrderByDescending(Function(x) x.Name).ToList.GetRange(0, 4)

                        For index As Integer = 0 To matchingVoiceChannels.Count - 1
                            vcListEmbed.Description &= $"**`{index + 1}`** {matchingVoiceChannels(index).Name} ({matchingVoiceChannels(index).Id}){Environment.NewLine}"
                        Next

                        Dim message As DiscordMessage = Await ctx.RespondAsync(embed:=vcListEmbed.Build)

                        For index As Integer = 0 To matchingVoiceChannels.Count - 1
                            Await message.CreateReactionAsync(emojis(index))
                        Next

                        Dim interactivity As InteractivityExtension = ctx.Client.GetInteractivity()
                        Dim reactionContext As InteractivityResult(Of MessageReactionAddEventArgs) = Await interactivity.WaitForReactionAsync(Function(e)
                                                                                                                                                  If e.User = ctx.User Then
                                                                                                                                                      Return emojis.Contains(e.Emoji)
                                                                                                                                                  End If

                                                                                                                                                  Return False
                                                                                                                                              End Function)
                        If reactionContext.Result IsNot Nothing Then
                            Dim resultEmbed As DiscordEmbedBuilder = Await MoveUsersAsync(originChannel, matchingVoiceChannels(emojis.IndexOf(reactionContext.Result.Emoji)))
                            embed = resultEmbed
                        Else
                            With embed
                                .Color = DiscordColor.Red
                                .Title = "Voice Channel Move Cancelled"
                                .Description = "There was no response within the time limit"
                            End With
                        End If

                        Await message.DeleteAsync()
                    End If
                End If
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        Private Async Function MoveUsersAsync(originChannel As DiscordChannel, destinationChannel As DiscordChannel) As Task(Of DiscordEmbedBuilder)
            Dim embed As New DiscordEmbedBuilder

            Try
                If destinationChannel.UserLimit > 0 AndAlso (originChannel.Users.Count + destinationChannel.Users.Count) > destinationChannel.UserLimit Then
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Cannot Move Users"
                        .Description = $"The total number of users would exceede the user limit of `{destinationChannel.Name}`"
                    End With
                Else
                    Dim userCount As Integer

                    For Each user As DiscordMember In originChannel.Users
                        Await user.PlaceInAsync(destinationChannel)
                        userCount += 1
                    Next

                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Title = "Voice Channel Move Successful"
                        .Description = $"Moved **`{userCount}`** {If(userCount = 1, "user", "users")} to **`{destinationChannel.Name}`**"
                    End With
                End If
            Catch ex As Exception
                With embed
                    .Title = "Voice Channel Move Unsuccessful"
                    .Description = $"I ran into a problem while moving users to **`{destinationChannel.Name}`**"
                End With
            End Try

            Return embed
        End Function

    End Class

End Namespace