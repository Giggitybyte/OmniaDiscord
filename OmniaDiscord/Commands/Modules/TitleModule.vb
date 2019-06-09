Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Entities.Database

Namespace Commands.Modules

    ' This whole thing could probably use a redo at some point.
    ' TODO: Verify and compact logic for this module.

    <Group("titles"), RequireGuild>
    <Description("Display and manage titles for this server.")>
    <RequireBotPermissions(Permissions.SendMessages Or Permissions.EmbedLinks)>
    Public Class TitleModule
        Inherits OmniaCommandBase

        <GroupCommand>
        Public Async Function DisplayTitles(ctx As CommandContext) As Task
            Await ctx.TriggerTypingAsync()

            Dim embed As New DiscordEmbedBuilder With {
                    .Title = "Staff Title List",
                    .Color = DiscordColor.CornflowerBlue
            }

            embed.AddField("Owner", ctx.Guild.Owner.Mention, True)

            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle))
                Dim users As New List(Of String)
                For Each staffMember In GuildData.StaffTitles.Where(Function(kvp) kvp.Value = title)
                    Dim user As DiscordMember = Await ctx.Guild.GetMemberAsync(staffMember.Key)
                    users.Add(user.Mention)
                Next

                embed.AddField(title.ToString, If(users.Any, String.Join(", ", users), "None."), True)
            Next

            Await ctx.RespondAsync(embed:=embed.Build())
        End Function

        <Command("assign"), Aliases("give"), RequireStaff>
        <Description("Assigns the provided title to a user.")>
        Public Async Function AssignTitle(ctx As CommandContext, titleName As String, user As DiscordMember) As Task
            Await ctx.TriggerTypingAsync()
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red}
            Dim title As GuildTitle

            If user.Id = ctx.Member.Id Then
                With embed
                    .Title = "Invalid User"
                    .Description = "You cannot assign a title to yourself!"
                End With

            ElseIf Not [Enum].TryParse(titleName, True, title) Then
                With embed
                    .Title = "Invalid Title Name"
                    .Description = $"Valid title names: {String.Join(", ", [Enum].GetNames(GetType(GuildTitle)).Select(Function(typeName) $"`{typeName}`"))}"
                End With

            ElseIf Not DoesMeetMinimumTitleRequirement(ctx, title) Then
                With embed
                    .Title = "Couldn't Assign Title"
                    .Description = $"You do not have the minimum title required to assign `{title.ToString}` to users."
                End With

            ElseIf GuildData.StaffTitles.ContainsKey(user.Id) AndAlso GuildData.StaffTitles(user.Id) = title Then
                With embed
                    .Title = "Couldn't Assign Title"
                    .Description = $"{user.Mention} already has the title of `{title.ToString}`!"
                End With

            End If

            If Not String.IsNullOrEmpty(embed.Description) Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            GuildData.StaffTitles(user.Id) = title
            UpdateGuildData()

            With embed
                .Color = DiscordColor.SpringGreen
                .Title = "Title Successfully Assigned"
                .Description = $"{user.Mention} now has the title of `{title.ToString}`"
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("remove"), RequireStaff>
        <Description("Removes the current title of a user.")>
        Public Async Function RemoveTitle(ctx As CommandContext, user As DiscordMember) As Task
            Await ctx.TriggerTypingAsync()
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Title = "Couldn't Remove Title"}
            Dim title As GuildTitle

            If Not GuildData.StaffTitles.ContainsKey(user.Id) Then
                embed.Description = $"{user.Mention} does not have a title."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            title = GuildData.StaffTitles(user.Id)

            If Not DoesMeetMinimumTitleRequirement(ctx, title) Then
                embed.Description = $"You do not have the minimum title required to remove `{title.ToString}` from users."

            Else
                GuildData.StaffTitles.Remove(user.Id)
                UpdateGuildData()

                With embed
                    .Color = DiscordColor.SpringGreen
                    .Title = "Title Successfully Removed"
                    .Description = $"{user.Mention} no longer has the title of `{title.ToString}`"
                End With

            End If

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        Private Function DoesMeetMinimumTitleRequirement(ctx As CommandContext, targetTitle As GuildTitle) As Boolean
            If ctx.Guild.Owner.Id = ctx.Member.Id Then Return True
            If Not GuildData.StaffTitles.ContainsKey(ctx.Member.Id) Then Return False

            Dim validTitles As New List(Of GuildTitle)
            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle)).Cast(Of GuildTitle)
                If title > targetTitle Then validTitles.Add(title)
            Next

            Return validTitles.Contains(GuildData.StaffTitles(ctx.Member.Id))
        End Function

    End Class

End Namespace