Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports DSharpPlus.Entities.DiscordEmbedBuilder
Imports DSharpPlus.Interactivity
Imports OmniaDiscord.Entities.Attributes
Imports OmniaDiscord.Entities.Database

Namespace Commands.Modules
    Public Class MoveModule
        Inherits BaseCommandModule

        <Command("move"), RequireGuild>
        <Description("Moves users from the voice channel you're currently in to another voice channel." + vbCrLf + "If multiple destination voice channels are found, you'll have the option to choose from up to four of them." + vbCrLf + vbCrLf + "Partial voice channel names and channel IDs are accepted.")>
        <RequireBotPermissions(Permissions.SendMessages Or Permissions.EmbedLinks Or Permissions.AddReactions Or Permissions.MoveMembers)>
        <RequireTitle(GuildTitle.Helper)>
        <Cooldown(1, 5, CooldownBucketType.Guild)>
        Public Async Function VoiceChannelMoveCommand(ctx As CommandContext, <RemainingText> destination As String) As Task
            If destination Is Nothing Then Return

            Dim originChannel As DiscordChannel = ctx.Member.VoiceState?.Channel
            If originChannel Is Nothing Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = "You must be in a voice channel to use this command."
                })
                Return
            End If

            Dim channelId As ULong

            If Not (ULong.TryParse(destination, channelId) AndAlso ctx.Guild.Channels.ContainsKey(channelId)) Then
                Dim guildVoiceChannels = ctx.Guild.Channels.Values.Where(Function(c) c.Type = ChannelType.Voice AndAlso Not c.Id = originChannel.Id)
                Dim matchingVoiceChannels As New List(Of DiscordChannel)

                For Each voiceChannel As DiscordChannel In guildVoiceChannels
                    If voiceChannel.Name.ToLower.Contains(destination.ToLower) Then matchingVoiceChannels.Add(voiceChannel)
                Next

                Select Case matchingVoiceChannels.Count
                    Case 0
                        Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                            .Color = DiscordColor.Red,
                            .Description = "No matching voice channels could be found with the input provided."
                        })
                        Return
                    Case 1
                        channelId = matchingVoiceChannels.First.Id
                    Case > 1
                        channelId = Await VoiceChannelPromptAsync(ctx, matchingVoiceChannels)
                        If channelId = 0 Then Return
                End Select
            End If

            Dim message = Await MoveUsersAsync(originChannel, ctx.Guild.Channels(channelId))
            Await ctx.RespondAsync(embed:=message)
        End Function

        Private Async Function VoiceChannelPromptAsync(ctx As CommandContext, channels As List(Of DiscordChannel)) As Task(Of ULong)
            Dim emojis As New List(Of DiscordEmoji) From {
                DiscordEmoji.FromName(ctx.Client, ":one:"),
                DiscordEmoji.FromName(ctx.Client, ":two:"),
                DiscordEmoji.FromName(ctx.Client, ":three:"),
                DiscordEmoji.FromName(ctx.Client, ":four:")
            }

            Dim embed As New DiscordEmbedBuilder With {
                .Title = "Multiple Voice Channels Found",
                .Color = DiscordColor.Orange,
                .Footer = New EmbedFooter With {
                    .Text = "Select voice channel you'd like below",
                    .IconUrl = $"{Bot.Config.ResourceUrl}/assets/omnia/ArrowDownOrange.png"
                }
            }

            channels = channels.Take(4).ToList

            For index As Integer = 0 To channels.Count - 1
                embed.Description &= $"{emojis(index)} {channels(index).Name} ({channels(index).Id}){Environment.NewLine}"
            Next

            Dim message = Await ctx.RespondAsync(embed:=embed.Build)
            For index As Integer = 0 To channels.Count - 1
                Await message.CreateReactionAsync(emojis(index))
            Next

            Dim interactivity = ctx.Client.GetInteractivity()
            Dim reactionContext = Await interactivity.WaitForReactionAsync(Function(e)
                                                                               If e.User.Id = ctx.User.Id Then Return emojis.Contains(e.Emoji)
                                                                               Return False
                                                                           End Function,
                                                                           TimeSpan.FromSeconds(15))

            If reactionContext.Result IsNot Nothing Then
                Await message.DeleteAsync
                Return channels(emojis.IndexOf(reactionContext.Result.Emoji)).Id
            End If

            embed.Color = DiscordColor.Red
            Await message.ModifyAsync(embed:=embed.Build)

            For index As Integer = 0 To channels.Count - 1
                Await message.DeleteOwnReactionAsync(emojis(index))
            Next

            Return 0
        End Function

        Private Async Function MoveUsersAsync(originChannel As DiscordChannel, destinationChannel As DiscordChannel) As Task(Of DiscordEmbedBuilder)
            Dim totalUsers = originChannel.Users.Count + destinationChannel.Users.Count
            If destinationChannel.UserLimit > 0 AndAlso totalUsers > destinationChannel.UserLimit Then
                Return New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"Cannot move users.{Environment.NewLine}The total number of users would exceede the user limit of `{destinationChannel.Name}`"
                }
            End If

            Dim userCount = originChannel.Users.Count
            For Each user As DiscordMember In originChannel.Users
                Await user.PlaceInAsync(destinationChannel)
            Next

            Return New DiscordEmbedBuilder With {
                .Color = DiscordColor.SpringGreen,
                .Title = "Move Successful",
                .Description = $"Moved **`{userCount}`** {If(userCount = 1, "user", "users")} to **`{destinationChannel.Name}`**"
            }
        End Function
    End Class
End Namespace