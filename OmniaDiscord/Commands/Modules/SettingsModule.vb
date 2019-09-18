Imports System.Reflection
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.Entities
Imports OmniaDiscord.Entities.Attributes
Imports OmniaDiscord.Entities.Database

Namespace Commands.Modules

    <Group("settings"), RequireGuild>
    <Description("Displays settings for this server. " + vbCrLf + " Child commands allow for modification of settings.")>
    <RequireBotPermissions(Permissions.EmbedLinks)>
    Public Class SettingsModule
        Inherits OmniaCommandBase

        <GroupCommand>
        Public Async Function DisplaySettings(ctx As CommandContext) As Task
            Dim properites = GetType(GuildSettings).GetProperties()
            Array.Sort(properites, Function(p1, p2) p1.Name.CompareTo(p2.Name))

            Dim embed As New DiscordEmbedBuilder With {
                .Color = DiscordColor.CornflowerBlue,
                .Author = New DiscordEmbedBuilder.EmbedAuthor With {
                    .IconUrl = ctx.Guild.IconUrl,
                    .Name = $"{ctx.Guild.Name} Settings"
                }
            }

            For Each prop In properites
                Dim attr = prop.GetCustomAttributes(GetType(GuildSettingAttribute), True).FirstOrDefault
                If attr Is Nothing Then Continue For
                Dim value As String = prop.GetValue(DbGuild.Settings)?.ToString
                If value Is Nothing OrElse value = "0" Then value = "Not Set"

                embed.AddField(attr.DisplayName, $"`{value}`", True)
            Next

            Await ctx.RespondAsync(embed:=embed.Build)
        End Function

        <Command("set")>
        <Description("Sets the value of a setting. A value of 'null' will reset the specified setting to its default.")>
        Public Async Function SetSettingCommand(ctx As CommandContext, settingKey As String, value As String) As Task
            Dim setting As PropertyInfo
            Dim settingInfo As GuildSettingAttribute

            For Each prop In GetType(GuildSettings).GetProperties()
                Dim attr = prop.GetCustomAttributes(GetType(GuildSettingAttribute), True).FirstOrDefault

                If attr?.UserSetKey = settingKey Then
                    setting = prop
                    settingInfo = attr
                    Exit For
                End If
            Next

            If setting Is Nothing Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"`{settingKey}` is not a valid key.{Environment.NewLine}You can view all valid keys by running `{ctx.Prefix}settings keys`."
                })
                Return
            End If

            Dim userTitle As GuildTitle = 0
            DbGuild.Data.TitleHolders.TryGetValue(ctx.Member.Id, userTitle)
            If Not userTitle >= settingInfo.RequiredTitle AndAlso Not ctx.Member.Id = ctx.Guild.Owner.Id Then
                Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                    .Color = DiscordColor.Red,
                    .Description = $"This settings requires a title of `{settingInfo.RequiredTitle.ToString}` or higher to modify."
                })
                Return
            End If

            If settingInfo.ValidSetType.Equals(GetType(String)) Then
                setting.SetValue(DbGuild.Settings, If(value.ToLower = "null", String.Empty, value))
                Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
                Return
            End If

            Dim newValue = Activator.CreateInstance(settingInfo.ValidSetType)
            If Not value.ToLower = "null" Then
                Try
                    newValue = settingInfo.ValidSetType.InvokeMember("Parse", BindingFlags.InvokeMethod, Type.DefaultBinder, newValue, {value}) ' Fuck Visual Basic
                Catch ex As Exception
                    ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                        .Color = DiscordColor.Red,
                        .Description = $"Invalid value for `{settingInfo.UserSetKey}`.{Environment.NewLine}Expected a `{settingInfo.ValidSetType.Name}` value."
                    }).GetAwaiter.GetResult()
                    Return
                End Try
            End If

            setting.SetValue(DbGuild.Settings, newValue)
            Await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":ok_hand:"))
        End Function

        <Command("keys")>
        <Description("Displays all valid setting keys.")>
        Public Async Function SettingKeysCommand(ctx As CommandContext) As Task
            Dim keys As New List(Of String)

            For Each prop In GetType(GuildSettings).GetProperties()
                Dim attr = prop.GetCustomAttributes(GetType(GuildSettingAttribute), True).FirstOrDefault
                If attr IsNot Nothing Then keys.Add(attr.UserSetKey)
            Next

            Await ctx.RespondAsync(embed:=New DiscordEmbedBuilder With {
                .Color = DiscordColor.CornflowerBlue,
                .Title = "Valid Setting Keys",
                .Description = String.Join(", ", keys.Select(Function(s) $"`{s}`"))
            })
        End Function
    End Class
End Namespace