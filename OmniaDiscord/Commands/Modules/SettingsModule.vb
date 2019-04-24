Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Converters
Imports DSharpPlus.Entities
Imports DSharpPlus.Interactivity
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Entites.Database

Namespace Commands.Modules

    <Group("settings"), RequireGuild>
    <Description("Allows for the viewing and the modifcation of server settings.")>
    <RequireBotPermissions(Permissions.EmbedLinks)>
    Public Class SettingsModule
        Inherits OmniaCommandBase

        <GroupCommand>
        Public Async Function DisplaySettings(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim logChannel As DiscordChannel = ctx.Guild.GetChannel(GuildSettings.LogChannelId)

            With embed
                .Color = DiscordColor.CornflowerBlue
                .AddField("Custom Prefix", $"`{If(GuildSettings.Prefix, "Not Set")}`", True)
                .AddField("Log Channel", $"{If(logChannel?.Mention, Formatter.InlineCode("Not Set"))}", True)

            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Group("set")>
        <Description("This command allows those with the title of moderator on up to set various server settings.")>
        Public Class SetModule
            Inherits OmniaCommandBase

            <Command("logchannel")>
            <Description("This command sets the channel where logs will be output to.")>
            <RequireTitle(GuildTitle.Moderator)>
            Public Async Function SetLogChannel(ctx As CommandContext, channel As String) As Task
                Dim targetChannel As [Optional](Of DiscordChannel) = Await New DiscordChannelConverter().ConvertAsync(channel, ctx)
                Dim embed As New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Title = "Invalid Channel",
                    .Description = "The channel you specified "
                }

                If Not targetChannel.HasValue Then
                    embed.Description &= "doesn't seem to exist."

                ElseIf Not targetChannel.Value.Type = ChannelType.Text Then
                    embed.Description &= "is not a text channel."

                Else
                    GuildSettings.LogChannelId = targetChannel.Value.Id
                    UpdateGuildSettings()

                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Title = "Log Channel Set"
                        .Description = $"All logs will now be output to {targetChannel.Value.Mention}"
                    End With
                End If

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("prefix")>
            <Description("This command sets the custom prefix for this server.")>
            <RequireTitle(GuildTitle.Admin)>
            Public Async Function SetPrefix(ctx As CommandContext, <RemainingText> newPrefix As String) As Task
                Dim oldPrefix As String = GuildSettings.Prefix
                Dim embed As New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Title = "Invalid Prefix",
                    .Description = "Prefix cannot be "
                }

                If String.IsNullOrEmpty(newPrefix) Then
                    embed.Description &= "nothing."

                ElseIf newPrefix.Count > 4 Then
                    embed.Description &= "more than four characters long."

                Else
                    GuildSettings.Prefix = newPrefix.Trim
                    UpdateGuildSettings()

                    With embed
                        .Color = DiscordColor.Green
                        .Title = "Custom Prefix Set"
                        .Description = $"Server prefix was changed from `{If(oldPrefix, OmniaConfig.DefaultPrefix)}` to `{newPrefix}`"
                    End With
                End If

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function
        End Class

        <Group("reset")>
        <Description("This command allows those with the title of admin to reset various server settings back to their defaults.")>
        <RequireTitle(GuildTitle.Admin)>
        Public Class ResetModule
            Inherits OmniaCommandBase

            <Command("prefix")>
            <Description("Removes the custom prefix for this server.")>
            <RequireTitle(GuildTitle.Admin)>
            Public Async Function ResetPrefix(ctx As CommandContext) As Task
                GuildSettings.Prefix = Nothing
                UpdateGuildSettings()

                Dim embed As New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Green,
                    .Title = "Prefix Reset",
                    .Description = "Your server no longer has a custom prefix."
                }

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            <Command("logchannel")>
            <Description("Resets the log channel for this server, effectively disabling log messages.")>
            Public Async Function ResetLogChannel(ctx As CommandContext) As Task
                GuildSettings.LogChannelId = Nothing
                UpdateGuildSettings()

                Dim embed As New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Green,
                    .Title = "Log Channel Reset",
                    .Description = "Your server no longer has a channel for logs."
                }

                Await ctx.RespondAsync(embed:=embed.Build)
            End Function

            Private Async Function GetUserConfirmationAsync(ctx As CommandContext) As Task(Of Boolean)
                Dim interactivity As InteractivityExtension = ctx.Client.GetInteractivity
                Dim conformationCode As String = Utilities.GenerateRandomChars(8)
                Dim embed As New DiscordEmbedBuilder

                With embed
                    .Color = DiscordColor.Yellow
                    .Title = "Action Confirmation"

                    .Description = $"Please be sure you want to go through with this action.{Environment.NewLine}"
                    .Description &= $"Respond with the following confirmation code to complete this action.{Environment.NewLine}```{conformationCode}```"
                End With

                Dim confirmationMessage As DiscordMessage = Await ctx.RespondAsync(embed:=embed.Build)
                Dim message As InteractivityResult(Of DiscordMessage) = Await interactivity.WaitForMessageAsync(Function(m)
                                                                                                                    If m.Author = ctx.Message.Author Then
                                                                                                                        Return m.Content.Trim = conformationCode
                                                                                                                    End If

                                                                                                                    Return False
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
        End Class

    End Class
End Namespace