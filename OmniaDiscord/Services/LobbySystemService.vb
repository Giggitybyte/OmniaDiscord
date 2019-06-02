Imports System.Collections.Concurrent
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports OmniaDiscord.Entities.Database

Namespace Services
    Public Class LobbySystemService
        Private ReadOnly _db As DatabaseService
        Private ReadOnly _leaveTokens As New ConcurrentDictionary(Of ULong, Dictionary(Of ULong, CancellationTokenSource))
        Public ReadOnly _queue As New ConcurrentDictionary(Of ULong, List(Of ULong))
        Public ReadOnly _generatedChannels As ConcurrentDictionary(Of ULong, (AllowedUsers As List(Of ULong), LobbyChannel As ULong))

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            _db = db

            AddHandler client.VoiceStateUpdated, AddressOf LobbySystemManager
            AddHandler client.VoiceStateUpdated, AddressOf GameChannelManager
            AddHandler client.GuildMemberRemoved, AddressOf RemovedUserHandler
        End Sub

        Private Async Function LobbySystemManager(e As VoiceStateUpdateEventArgs) As Task
            Dim data As GuildData
            Dim lobbyChannel As DiscordChannel

            ' Check if the lobby system is enabled
            If _db.GetGuildSettings(e.Guild.Id).IsLobbySystemEnabled Then data = _db.GetGuildData(e.Guild.Id)
            If data Is Nothing Then Return
            If Not _queue.ContainsKey(e.Guild.Id) Then _queue.TryAdd(e.Guild.Id, New List(Of ULong))

            ' Add/remove user from the channel queue + more validation
            If data.LobbyChannels.Contains(e.Before.Channel?.Id) Then _queue(e.Guild.Id).Remove(e.User.Id)

            If data.LobbyChannels.Contains(e.After.Channel?.Id) Then
                _queue(e.Guild.Id).Add(e.User.Id)
                lobbyChannel = e.After.Channel
            End If

            If lobbyChannel Is Nothing OrElse lobbyChannel.UserLimit = 0 Then Return

            ' Move queued users to new channel if we're over the threshold
            If Not lobbyChannel.Users.Count >= lobbyChannel.UserLimit Then Return
            Dim users As New List(Of DiscordMember)
            Dim overwrites As New List(Of DiscordOverwriteBuilder)
            overwrites.Add(New DiscordOverwriteBuilder().For(e.Guild.EveryoneRole).Deny(Permissions.UseVoice))

            Dim userIds As List(Of ULong) = _queue(e.After.Channel.Id).TakeAndRemove(lobbyChannel.UserLimit)
            For Each userId In userIds
                If e.Guild.Members.ContainsKey(userId) Then
                    users.Add(e.Guild.Members(userId))
                    overwrites.Add(New DiscordOverwriteBuilder().For(users.Last).Allow(Permissions.UseVoice))
                End If
            Next

            Dim channelName As String = lobbyChannel.Name.Substring(0, lobbyChannel.Name.ToLower.LastIndexOf("lobby")).Trim
            Dim generatedChannel As DiscordChannel = Await e.Guild.CreateChannelAsync(channelName,
                                                                                 ChannelType.Voice,
                                                                                 userLimit:=lobbyChannel.UserLimit,
                                                                                 overwrites:=overwrites)
            Await generatedChannel.ModifyPositionAsync(lobbyChannel.Position + 1)

            _generatedChannels.TryAdd(generatedChannel.Id, (userIds, lobbyChannel.Id))

            For Each user In users
                Await user.PlaceInAsync(generatedChannel)
            Next
        End Function

#Disable Warning BC42358
        Private Async Function GameChannelManager(e As VoiceStateUpdateEventArgs) As Task
            If Not _db.GetGuildSettings(e.Guild.Id).IsLobbySystemEnabled OrElse e.Before.Channel Is Nothing Then Return

            ' Keep generated channels full.
            If _generatedChannels.ContainsKey(e.Before.Channel.Id) Then
                Dim generatedChn = e.Before.Channel
                Dim allowedUsers = _generatedChannels(e.Before.Channel.Id).AllowedUsers

                If generatedChn?.Users.Any AndAlso allowedUsers.Contains(e.User.Id) Then
                    Dim cts As New CancellationTokenSource
                    _leaveTokens.GetOrAdd(generatedChn.Id, New Dictionary(Of ULong, CancellationTokenSource)).Add(e.User.Id, cts)

                    Task.Delay(15000, cts.Token).ContinueWith(Async Sub()
                                                                  ' Delete old user overwrite.
                                                                  Dim perms = generatedChn.PermissionOverwrites
                                                                  Await perms.FirstOrDefault(Function(o) o.GetMemberAsync.GetAwaiter.GetResult.Id = e.User.Id)?.DeleteAsync

                                                                  ' Get new user.
                                                                  If Not _queue(generatedChn.Id)?.Any Then Return
                                                                  Dim nextId = _queue(generatedChn.Id).TakeAndRemove(1).First
                                                                  Dim nextUser = e.Guild.Members(nextId)

                                                                  ' Make overwrite for new user then move them.
                                                                  Dim lobbyChn = e.Guild.Channels(_generatedChannels(generatedChn.Id).LobbyChannel)
                                                                  If Not nextUser.VoiceState?.Channel.Id = lobbyChn?.Id Then Return

                                                                  Await generatedChn.AddOverwriteAsync(nextUser, Permissions.UseVoice)
                                                                  Await nextUser.PlaceInAsync(generatedChn)
                                                              End Sub, TaskContinuationOptions.NotOnCanceled)

                Else
                    Await generatedChn?.DeleteAsync()
                End If
            End If

            ' Cancel tokens for returning users.
            If _generatedChannels.ContainsKey(e.After.Channel.Id) AndAlso _leaveTokens.ContainsKey(e.After.Channel.Id) Then
                Dim allowedUsers = _generatedChannels(e.After.Channel.Id).AllowedUsers
                If Not allowedUsers.Contains(e.User.Id) Then Return

                Dim token As CancellationTokenSource
                _leaveTokens(e.After.Channel.Id).Remove(e.User.Id, token)
                token.Cancel()
            End If

        End Function
#Enable Warning BC42358

        Private Function RemovedUserHandler(e As GuildMemberRemoveEventArgs) As Task
            Dim channelId As ULong = _queue.Keys.FirstOrDefault(Function(k) e.Guild.Channels.ContainsKey(k))
            If channelId = 0 Then Return Task.CompletedTask
            If _queue(channelId).Contains(e.Member.Id) Then _queue(channelId).Remove(e.Member.Id)

            Return Task.CompletedTask
        End Function
    End Class
End Namespace