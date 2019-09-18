Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Commands.Bases
Imports OmniaDiscord.Entities.Attributes
Imports OmniaDiscord.Entities.Database

Namespace Commands.Modules
    <Group("titles"), RequireGuild>
    <Description("Displays titles for this server." + vbCrLf + "Child commands allow for management of titles.")>
    <RequireBotPermissions(Permissions.SendMessages Or Permissions.EmbedLinks)>
    Public Class TitleModule
        Inherits OmniaDbCommandBase

        <GroupCommand>
        Public Async Function DisplayTitles(ctx As CommandContext) As Task
            Dim embed As New DiscordEmbedBuilder With {
                    .Title = "Staff Title List",
                    .Color = DiscordColor.CornflowerBlue
            }

            embed.AddField("Owner", ctx.Guild.Owner.Mention, True)

            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle))
                Dim users As New List(Of String)
                For Each staffMember In DbGuild.Data.TitleHolders.Where(Function(kvp) kvp.Value = title)
                    Dim user As DiscordMember = Await ctx.Guild.GetMemberAsync(staffMember.Key)
                    users.Add(user.Mention)
                Next

                If users.Any Then embed.AddField(title.ToString, String.Join(", ", users), True)
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
                embed.Title = "Invalid User"
                embed.Description = "You cannot assign a title to yourself."
            ElseIf Not [Enum].TryParse(titleName, True, title) Then
                embed.Title = "Invalid Title Name"
                embed.Description = $"Valid title names: {String.Join(", ", [Enum].GetNames(GetType(GuildTitle)).Select(Function(typeName) $"`{typeName}`"))}"
            ElseIf Not DoesMeetMinimumTitleRequirement(ctx, title) Then
                embed.Title = "Cannot Assign Title"
                embed.Description = $"You do not have the minimum title required to assign `{title.ToString}` to users."
            ElseIf DbGuild.Data.TitleHolders.ContainsKey(user.Id) AndAlso DbGuild.Data.TitleHolders(user.Id) = title Then
                embed.Title = "Cannot Assign Title"
                embed.Description = $"{user.Mention} already has the title of `{title.ToString}`."
            End If

            If embed.Description Is Nothing Then
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            DbGuild.Data.TitleHolders(user.Id) = title

            With embed
                .Color = DiscordColor.SpringGreen
                .Title = "Title Successfully Assigned"
                .Description = $"{user.Mention} now has the title of `{title.ToString}`"
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("remove"), Aliases("strip"), RequireStaff>
        <Description("Removes the title that a user has, if any.")>
        Public Async Function RemoveTitle(ctx As CommandContext, user As DiscordMember) As Task
            Await ctx.TriggerTypingAsync()
            Dim embed As New DiscordEmbedBuilder With {.Color = DiscordColor.Red, .Title = "Cannot Remove Title"}

            If Not DbGuild.Data.TitleHolders.ContainsKey(user.Id) Then
                embed.Description = $"{user.Mention} does not have a title."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            Dim title = DbGuild.Data.TitleHolders(user.Id)
            If Not DoesMeetMinimumTitleRequirement(ctx, title) Then
                embed.Description = $"You do not have the minimum title required to remove `{title.ToString}` from users."
                Await ctx.RespondAsync(embed:=embed.Build)
                Return
            End If

            DbGuild.Data.TitleHolders.Remove(user.Id)

            With embed
                .Color = DiscordColor.SpringGreen
                .Title = "Title Successfully Removed"
                .Description = $"{user.Mention} no longer has the title of `{title.ToString}`"
            End With

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        Private Function DoesMeetMinimumTitleRequirement(ctx As CommandContext, targetTitle As GuildTitle) As Boolean
            If ctx.Guild.Owner.Id = ctx.Member.Id Then Return True
            If Not DbGuild.Data.TitleHolders.ContainsKey(ctx.Member.Id) Then Return False

            Dim validTitles As New List(Of GuildTitle)
            For Each title As GuildTitle In [Enum].GetValues(GetType(GuildTitle)).Cast(Of GuildTitle)
                If title > targetTitle Then validTitles.Add(title)
            Next

            Return validTitles.Contains(DbGuild.Data.TitleHolders(ctx.Member.Id))
        End Function

    End Class

End Namespace