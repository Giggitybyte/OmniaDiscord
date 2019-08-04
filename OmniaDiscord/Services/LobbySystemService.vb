Imports System.Collections.Concurrent
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs

Namespace Services
    Public Class LobbySystemService
        Private ReadOnly _db As DatabaseService
        Private ReadOnly _queue As New ConcurrentDictionary(Of ULong, List(Of ULong)) ' Lobby channel, ordered list of queued users.
        Private ReadOnly _leaveTokens As New ConcurrentDictionary(Of ULong, Dictionary(Of ULong, CancellationTokenSource)) ' Lobby channel, dict of user cancel tokens.
        Private ReadOnly _generatedChannels As New ConcurrentDictionary(Of ULong, ULong) ' generated channel, lobby channel.
        Private ReadOnly _allowedUsers As New ConcurrentDictionary(Of ULong, List(Of ULong)) ' generated channel, allowed users.

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            _db = db

            AddHandler client.VoiceStateUpdated, AddressOf LobbySystemManager
            AddHandler client.VoiceStateUpdated, AddressOf GeneratedChannelManager
            AddHandler client.GuildMemberRemoved, AddressOf RemovedUserHandler
            AddHandler client.ChannelDeleted, AddressOf RemovedChannelHandler
        End Sub

        Private Async Function LobbySystemManager(e As VoiceStateUpdateEventArgs) As Task
            ' Return if user is a bot or the lobby system is not enabled on this guild.
            If e.User.IsBot OrElse Not _db.GetGuildSettings(e.Guild.Id).IsLobbySystemEnabled Then Return

            ' If leave channel is a lobby channel, remove user from the queue for that channel.
            Dim data = _db.GetGuildData(e.Guild.Id)

            If e.Before?.Channel IsNot Nothing AndAlso
                data.LobbyChannels.Contains(e.Before.Channel.Id) AndAlso
                _queue.ContainsKey(e.Before.Channel.Id) AndAlso
                _queue(e.Before.Channel.Id).Contains(e.User.Id) Then
                _queue(e.Before.Channel.Id).Remove(e.User.Id)
            End If

            ' If join channel is a lobby channel, add user to the queue for that channel.
            If e.After?.Channel IsNot Nothing AndAlso data.LobbyChannels.Contains(e.After.Channel.Id) Then
                If Not _queue.ContainsKey(e.After.Channel.Id) Then _queue.TryAdd(e.After.Channel.Id, New List(Of ULong))
                If Not _queue(e.After.Channel.Id).Contains(e.User.Id) Then _queue(e.After.Channel.Id).Add(e.User.Id)

                ' Generate channel and move users if needed.
                Dim lobbyChannel = e.After.Channel

                If Not (lobbyChannel.UserLimit > 0 AndAlso lobbyChannel.Users.Count >= lobbyChannel.UserLimit) Then Return
                If _queue(lobbyChannel.Id).Count < lobbyChannel.UserLimit Then
                    Dim tempUsers = lobbyChannel.Users.Where(Function(m) Not _queue(lobbyChannel.Id).Contains(m.Id))
                    _queue(lobbyChannel.Id).AddRange(tempUsers.Select(Function(u) u.Id))
                End If

                Dim users As New List(Of DiscordMember)
                Dim userIds As List(Of ULong) = _queue(e.After.Channel.Id).TakeAndRemove(lobbyChannel.UserLimit).ToList
                Dim overwrites As New List(Of DiscordOverwriteBuilder) From {
                    New DiscordOverwriteBuilder().For(e.Guild.EveryoneRole).Deny(Permissions.UseVoice)
                }

                For Each userId In userIds
                    If e.Guild.Members.ContainsKey(userId) Then
                        users.Add(e.Guild.Members(userId))
                        overwrites.Add(New DiscordOverwriteBuilder().For(users.Last).Allow(Permissions.UseVoice))
                    End If
                Next

                Dim generatedChan = Await e.Guild.CreateVoiceChannelAsync($"{New String(lobbyChannel.Name.Take(97).ToArray)} - {Utilities.GenerateRandomChars(4).ToUpper}",
                                                                         If(lobbyChannel.Parent, Nothing),
                                                                         user_limit:=lobbyChannel.UserLimit,
                                                                         overwrites:=overwrites)

                Await generatedChan.ModifyAsync(Sub(c) c.Position = lobbyChannel.Position)
                _generatedChannels(generatedChan.Id) = lobbyChannel.Id
                _allowedUsers(generatedChan.Id) = userIds

                For Each user In users
                    Await user.PlaceInAsync(generatedChan)
                Next
            End If
        End Function

#Disable Warning BC42358
        Private Async Function GeneratedChannelManager(e As VoiceStateUpdateEventArgs) As Task
            If Not _db.GetGuildSettings(e.Guild.Id).IsLobbySystemEnabled Then Return

            ' Keep generated channels full.
            If e.Before?.Channel IsNot Nothing AndAlso _generatedChannels.ContainsKey(e.Before.Channel.Id) Then
                Dim generatedChan = e.Before.Channel

                If generatedChan?.Users.Any Then
                    If Not _allowedUsers(generatedChan.Id).Contains(e.User.Id) Then Return

                    Dim cts As New CancellationTokenSource
                    If Not _leaveTokens.ContainsKey(generatedChan.Id) Then _leaveTokens.TryAdd(generatedChan.Id, New Dictionary(Of ULong, CancellationTokenSource))
                    _leaveTokens(generatedChan.Id).TryAdd(e.User.Id, cts)

                    Task.Delay(15000, cts.Token).ContinueWith(Async Sub()
                                                                  ' Remove token.
                                                                  _leaveTokens.TryRemove(generatedChan.Id, Nothing)

                                                                  ' Delete old user overwrite.
                                                                  Dim perms = generatedChan.PermissionOverwrites
                                                                  Await perms.FirstOrDefault(Function(o)
                                                                                                 If o.Type = OverwriteType.Role Then Return False
                                                                                                 Return o.GetMemberAsync.GetAwaiter.GetResult.Id = e.User.Id
                                                                                             End Function)?.DeleteAsync

                                                                  ' Get lobby channel.
                                                                  Dim lobbyChanId = _generatedChannels(generatedChan.Id)
                                                                  If Not e.Guild.Channels.ContainsKey(lobbyChanId) Then Return
                                                                  Dim lobbyChan As DiscordChannel = e.Guild.Channels(lobbyChanId)

                                                                  ' Get new user.
                                                                  If Not _queue.ContainsKey(lobbyChanId) OrElse Not _queue(lobbyChanId)?.Any Then Return
                                                                  Dim nextId = _queue(lobbyChanId).TakeAndRemove(1).First
                                                                  Dim nextUser = e.Guild.Members(nextId)

                                                                  ' Make overwrite for new user then move them.
                                                                  If Not nextUser.VoiceState?.Channel.Id = lobbyChan?.Id Then Return
                                                                  Await generatedChan.AddOverwriteAsync(nextUser, Permissions.UseVoice)
                                                                  Await nextUser.PlaceInAsync(generatedChan)
                                                              End Sub, TaskContinuationOptions.NotOnCanceled)

                Else
                    Await generatedChan?.DeleteAsync()
                End If
            End If

            If e.After?.Channel IsNot Nothing Then

                ' Cancel tokens for returning users.
                If _generatedChannels.ContainsKey(e.After.Channel.Id) AndAlso _leaveTokens.ContainsKey(e.After.Channel.Id) Then
                    If Not _allowedUsers(e.After.Channel.Id).Contains(e.User.Id) Then Return

                    Dim token As CancellationTokenSource
                    _leaveTokens(e.After.Channel.Id).Remove(e.User.Id, token)
                    token.Cancel()
                End If

                ' Top off generated channels with users.
                If _db.GetGuildData(e.Guild.Id).LobbyChannels.Contains(e.After.Channel.Id) Then
                    Dim lobbyChan = e.After.Channel
                    Dim user = e.Guild.Members(e.User.Id)
                    Dim genChans As New List(Of DiscordChannel)

                    For Each chanId In _generatedChannels.Where(Function(kvp) kvp.Value = e.After.Channel.Id).Select(Function(kvp) kvp.Key)
                        If e.Guild.Channels.ContainsKey(chanId) Then genChans.Add(e.Guild.Channels(chanId))
                    Next

                    If Not genChans.Any Then Return
                    Dim targetChan = genChans.FirstOrDefault(Function(c)
                                                                 Dim tokenCount As Integer = 0
                                                                 If _leaveTokens.ContainsKey(c.Id) Then tokenCount = _leaveTokens(c.Id).Count
                                                                 Return (c.UserLimit - c.Users.Count) - tokenCount > 0
                                                             End Function)
                    If targetChan IsNot Nothing Then
                        Await targetChan.AddOverwriteAsync(user, Permissions.UseVoice)
                        targetChan.PlaceMemberAsync(user)
                    End If
                End If

            End If
        End Function
#Enable Warning BC42358

        Private Function RemovedUserHandler(e As GuildMemberRemoveEventArgs) As Task
            Dim channelId As ULong = _queue.Keys.FirstOrDefault(Function(k) e.Guild.Channels.ContainsKey(k))
            If channelId = 0 Then Return Task.CompletedTask

            If _queue(channelId).Contains(e.Member.Id) Then _queue(channelId).Remove(e.Member.Id)
            If _leaveTokens.ContainsKey(channelId) AndAlso _leaveTokens(channelId).ContainsKey(e.Member.Id) Then _leaveTokens(channelId)(e.Member.Id).Cancel()

            Return Task.CompletedTask
        End Function

        Private Function RemovedChannelHandler(e As ChannelDeleteEventArgs) As Task
            If Not e.Channel.Type = ChannelType.Voice Then Return Task.CompletedTask
            If _generatedChannels.ContainsKey(e.Channel.Id) Then _generatedChannels.TryRemove(e.Channel.Id, Nothing)
            If _allowedUsers.ContainsKey(e.Channel.Id) Then _allowedUsers.TryRemove(e.Channel.Id, Nothing)
            If _leaveTokens.ContainsKey(e.Channel.Id) Then
                For Each token In _leaveTokens(e.Channel.Id).Values
                    token.Cancel()
                Next

                _leaveTokens.TryRemove(e.Channel.Id, Nothing)
            End If

            Dim data = _db.GetGuildData(e.Guild.Id)
            If Not data.LobbyChannels.Contains(e.Channel.Id) Then Return Task.CompletedTask
            data.LobbyChannels.Remove(e.Channel.Id)
            _db.UpdateGuildData(data)

            Return Task.CompletedTask
        End Function
    End Class
End Namespace