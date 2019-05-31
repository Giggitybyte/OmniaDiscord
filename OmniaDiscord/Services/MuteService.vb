Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports OmniaDiscord.Entities.Database

Namespace Services
    Public Class MuteService
        Private _db As DatabaseService

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            AddHandler client.TypingStarted, AddressOf MutedUserTypingHandlerAsync
            AddHandler client.MessageCreated, AddressOf MutedUserTextHandlerAsync
            AddHandler client.VoiceStateUpdated, AddressOf MutedUserVoiceHandlerAsync
            _db = db
        End Sub

        Private Async Function MutedUserVoiceHandlerAsync(arg As VoiceStateUpdateEventArgs) As Task
            If Not _db.DoesContainGuild(arg.Guild.Id) Then Return

            Dim guild As GuildData = _db.GetGuildData(arg.Guild.Id)
            If guild.MutedMembers.Contains(arg.User.Id) AndAlso Not arg.After.IsServerMuted Then Await DirectCast(arg.User, DiscordMember).SetMuteAsync(True)
        End Function

        Private Async Function MutedUserTextHandlerAsync(arg As MessageCreateEventArgs) As Task
            If Not _db.DoesContainGuild(arg.Guild.Id) Then Return

            Dim guild As GuildData = _db.GetGuildData(arg.Channel.GuildId)
            If guild.MutedMembers.Contains(arg.Author.Id) Then Await arg.Message.DeleteAsync()
        End Function

        Private Async Function MutedUserTypingHandlerAsync(arg As TypingStartEventArgs) As Task
            If Not _db.DoesContainGuild(arg.Channel.GuildId) Then Return

            Dim guild As GuildData = _db.GetGuildData(arg.Channel.GuildId)
            If guild.MutedMembers.Contains(arg.User.Id) Then
                Dim chanOverwrites As List(Of DiscordOverwrite) = arg.Channel.PermissionOverwrites.ToList
                Dim userOverwrite As DiscordOverwrite = chanOverwrites.FirstOrDefault(Function(o) o.GetMemberAsync.Id = arg.User.Id)
                Dim perms As Permissions = Permissions.AddReactions Or Permissions.SendMessages

                Await arg.Channel.AddOverwriteAsync(arg.User,
                                                    If(userOverwrite?.Allowed, Permissions.None) And perms,
                                                    If(userOverwrite?.Denied, Permissions.None) Or perms)

            End If
        End Function

    End Class
End Namespace