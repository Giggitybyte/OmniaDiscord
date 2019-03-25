Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Entites

Namespace Commands.Modules

    ' This whole thing could probably use a redo at some point.
    ' TODO: Verify and compact logic for this module.

    <Group("titles"), RequireGuild>
    <Description("Display and manage titles for this server.")>
    <RequireBotPermissions(Permissions.SendMessages Or Permissions.EmbedLinks)>
    Public Class TitleModule
        Inherits OmniaBaseModule

#Region "Command Methods"

        <GroupCommand>
        Public Async Function DisplayTitles(ctx As CommandContext) As Task
            Await ctx.TriggerTypingAsync()

            Dim embed As New DiscordEmbedBuilder With {
                    .Title = "Staff Title List",
                    .Color = DiscordColor.CornflowerBlue
            }

            embed.AddField("Owner", ctx.Guild.Owner.Mention, True)

            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle))
                If GuildData.StaffTitles(title).Count > 0 Then
                    Dim users As New List(Of String)
                    For Each userId As ULong In GuildData.StaffTitles(title)
                        Dim user As DiscordMember = Await ctx.Guild.GetMemberAsync(userId)
                        users.Add(user.Mention)
                    Next

                    embed.AddField(title.ToString, String.Join(", ", users), True)
                End If
            Next

            Await ctx.RespondAsync(embed:=embed.Build())
        End Function

        <Command("assign"), Aliases("give"), RequireStaff>
        <Description("Assigns the provided title to a user.")>
        Public Async Function AssignTitle(ctx As CommandContext, titleName As String, user As DiscordMember) As Task
            Await ctx.TriggerTypingAsync()
            Dim embed As New DiscordEmbedBuilder
            Dim title As GuildTitle

            If user.Id = ctx.Member.Id Then
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid User"
                    .Description = "You cannot assign a title to yourself!"
                End With

            ElseIf [Enum].TryParse(titleName, True, title) Then
                If DoesMeetMinimumTitleRequirement(ctx, title) Then
                    If GuildData.StaffTitles(title).Contains(user.Id) Then
                        With embed
                            .Color = DiscordColor.Red
                            .Title = "Couldn't Assign Title"
                            .Description = $"{user.Mention} already has the title of `{title.ToString}`!"
                        End With
                    Else
                        For Each staffTitle As GuildTitle In [Enum].GetValues(GetType(GuildTitle))
                            If GuildData.StaffTitles(staffTitle).Contains(user.Id) Then GuildData.StaffTitles(staffTitle).Remove(user.Id)
                        Next

                        GuildData.StaffTitles(title).Add(user.Id)
                        UpdateGuildData()

                        With embed
                            .Color = DiscordColor.SpringGreen
                            .Title = "Title Successfully Assigned"
                            .Description = $"{user.Mention} now has the title of `{title.ToString}`"
                        End With
                    End If
                Else
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Couldn't Assign Title"
                        .Description = $"You do not have the minimum title required to assign `{title.ToString}` to users."
                    End With
                End If
            Else
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Invalid Title Name"
                    .Description = $"Valid title names: {String.Join(", ", [Enum].GetNames(GetType(GuildTitle)).Select(Function(typeName) $"`{typeName}`"))}"
                End With
            End If

            Await ctx.RespondAsync(embed:=embed.Build())
        End Function

        <Command("remove"), RequireStaff>
        <Description("Removes the current title of a user.")>
        Public Async Function RemoveTitle(ctx As CommandContext, user As DiscordMember) As Task
            Dim embed As New DiscordEmbedBuilder
            Dim title As GuildTitle = 0

            Await ctx.TriggerTypingAsync()

            For Each staffTitle As GuildTitle In [Enum].GetValues(GetType(GuildTitle))
                If GuildData.StaffTitles(staffTitle).Contains(user.Id) Then title = staffTitle
            Next

            If title > 0 Then
                If DoesMeetMinimumTitleRequirement(ctx, title) Then
                    GuildData.StaffTitles(title).Remove(user.Id)
                    UpdateGuildData()

                    With embed
                        .Color = DiscordColor.SpringGreen
                        .Title = "Title Successfully Removed"
                        .Description = $"{user.Mention} no longer has the title of `{title.ToString}`"
                    End With
                Else
                    With embed
                        .Color = DiscordColor.Red
                        .Title = "Couldn't Remove Title"
                        .Description = $"You do not have the minimum title required to remove `{title.ToString}` from users."
                    End With
                End If
            Else
                With embed
                    .Color = DiscordColor.Red
                    .Title = "Couldn't Remove Title"
                    .Description = $"{user.Mention} does not have a title."
                End With
            End If

            Await ctx.RespondAsync(embed:=embed.Build())
        End Function

#End Region

#Region "Helper Methods"

        Private Function DoesMeetMinimumTitleRequirement(ctx As CommandContext, title As GuildTitle) As Boolean
            If title = GuildTitle.Admin Then
                If ctx.Guild.Owner.Id = ctx.Member.Id Then Return True

            ElseIf title = GuildTitle.Moderator Then
                If DoesHaveRequiredTitle(ctx, GuildTitle.Admin) Then Return True

            ElseIf title = GuildTitle.Helper Then
                If DoesHaveRequiredTitle(ctx, GuildTitle.Moderator) Then Return True

            End If

            Return False
        End Function

        Private Function DoesHaveRequiredTitle(context As CommandContext, minimumTitle As GuildTitle) As Boolean
            If context.Guild.Owner.Id = context.Member.Id Then
                Return True
            Else
                Dim validTitles As List(Of GuildTitle) = (From title In [Enum].GetValues(GetType(GuildTitle)).Cast(Of GuildTitle) Where title >= minimumTitle).ToList()

                For Each title As GuildTitle In validTitles
                    For Each userId As ULong In GuildData.StaffTitles(title)
                        If GuildData.StaffTitles(title).Contains(userId) Then Return True
                    Next
                Next

                Return False
            End If
        End Function

#End Region

    End Class

End Namespace