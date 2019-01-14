Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports DSharpPlus.Interactivity
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Services
Imports OmniaDiscord.Services.Entities.Database

Namespace Commands.Modules

    <Group("prefix"), Aliases("p"), RequireGuild>
    <Description("Displays prefix information and allows admins to change the custom prefix for this server.")>
    <RequireBotPermissions(Permissions.EmbedLinks)>
    Public Class PrefixModule
        Inherits OmniaBaseModule

        <GroupCommand>
        Public Async Function DisplayPrefix(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder

            With embed
                .Color = DiscordColor.CornflowerBlue
                .AddField("Custom Prefix", $"`{If(GuildSettings.Prefix, "Not Set")}`", True)
                .AddField("Default Prefix", $"`{OmniaConfig.DefaultPrefix}`", True)

            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("set")>
        <Description("Changes the custom prefix for this server.")>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function SetPrefix(ctx As CommandContext, <RemainingText> newPrefix As String) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim oldPrefix As String = GuildSettings.Prefix

            If String.IsNullOrEmpty(newPrefix) Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Prefix"
                    .Description = "Prefix cannot be nothing."
                End With
            ElseIf newPrefix.Count > 4 Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Prefix"
                    .Description = "Prefix cannot be more than four characters long."
                End With
            Else
                GuildSettings.Prefix = newPrefix.Trim
                UpdateGuildSettings()

                With embed
                    .Color = DiscordColor.Green
                    .Title = "Changed Prefix"
                    .Description = $"Server prefix was changed from `{If(oldPrefix, OmniaConfig.DefaultPrefix)}` to `{newPrefix}`"
                End With
            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("reset")>
        <Description("Removes the custom prefix for this server.")>
        <RequireTitle(GuildTitle.Admin)>
        Public Async Function ResetPrefix(ctx As CommandContext) As Task
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
            Dim result As MessageContext = Await interactivity.WaitForMessageAsync(Function(m)
                                                                                       If m.Author = ctx.Message.Author Then
                                                                                           Return m.Content.Trim = conformationCode
                                                                                       End If

                                                                                       Return False
                                                                                   End Function, TimeSpan.FromSeconds(30))

            If result.Message IsNot Nothing Then
                GuildSettings.Prefix = Nothing
                UpdateGuildSettings()

                With embed
                    .Color = DiscordColor.Green
                    .Title = "Prefix Reset"
                    .Description = "Your server no longer has a custom prefix."
                End With
            Else
                With embed
                    .Color = DiscordColor.Orange
                    .Title = "Timed Out"
                    .Description = "The confirmation code was not."
                End With
            End If

            Await confirmationMessage.DeleteAsync
            Await ctx.RespondAsync(embed:=embed.Build)

        End Function


    End Class
End Namespace