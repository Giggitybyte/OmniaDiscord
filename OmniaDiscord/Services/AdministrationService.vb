Imports System.Collections.Concurrent
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs

Namespace Services
    Public Class AdministrationService
        Public ReadOnly Property SoftbanTokens As New ConcurrentDictionary(Of ULong, CancellationTokenSource)
        Private ReadOnly _db As DatabaseService

        Sub New(client As DiscordClient, db As DatabaseService)
            _db = db

            AddHandler client.GuildMemberAdded, AddressOf MemberJoinHandler
            AddHandler client.GuildMemberRemoved, AddressOf MemberLeaveHandler
            AddHandler client.GuildMemberUpdated, AddressOf MemberUpdatedHandler
            AddHandler client.GuildRoleDeleted, AddressOf RoleDeletedHandler
            AddHandler client.ChannelCreated, AddressOf ChannelCreatedHandler
        End Sub

        ' Re-adds muted role to user who left the guild and rejoined.
        Private Async Function MemberJoinHandler(e As GuildMemberAddEventArgs) As Task
            If Not _db.GetGuildEntry(e.Guild.Id).Data.MutedMembers.Contains(e.Member.Id) Then Return

            Dim roleId = _db.GetGuildEntry(e.Guild.Id).Data.MutedRoleId
            Dim role As DiscordRole

            If Not e.Guild.Roles.ContainsKey(roleId) Then role = Await CreateGuildMutedRoleAsync(e.Guild)
            If role Is Nothing Then role = e.Guild.Roles(roleId)

            Await e.Member.GrantRoleAsync(role, "Re-muting previously muted user.")
        End Function

        ' Removes title from users that leave, are kicked, or are banned.
        Private Function MemberLeaveHandler(e As GuildMemberRemoveEventArgs) As Task
            If _db.GetGuildEntry(e.Guild.Id).Data.TitleHolders.ContainsKey(e.Member.Id) Then _db.GetGuildEntry(e.Guild.Id).Data.TitleHolders.Remove(e.Guild.Id)
            Return Task.CompletedTask
        End Function

        ' Re-adds muted role to user if it is manually removed from them.
        Private Async Function MemberUpdatedHandler(e As GuildMemberUpdateEventArgs) As Task
            If Not _db.GetGuildEntry(e.Guild.Id).Data.MutedMembers.Contains(e.Member.Id) Then Return

            Dim roleId = _db.GetGuildEntry(e.Guild.Id).Data.MutedRoleId
            If roleId = 0 Then Return

            If e.RolesBefore.Select(Function(r) r.Id).Contains(roleId) AndAlso Not e.RolesAfter.Select(Function(r) r.Id).Contains(roleId) Then
                Await e.Member.GrantRoleAsync(e.RolesBefore.FirstOrDefault(Function(r) r.Id = roleId), "Re-muting manually unmuted user.")
            End If
        End Function

        ' Re-creates muted role if it is deleted.
        Private Async Function RoleDeletedHandler(e As GuildRoleDeleteEventArgs) As Task
            If e.Role.Id = _db.GetGuildEntry(e.Guild.Id).Data.MutedRoleId Then
                Dim guild = _db.GetGuildEntry(e.Guild.Id)
                guild.Data.MutedRoleId = (Await CreateGuildMutedRoleAsync(e.Guild)).Id
                _db.UpdateGuildEntry(guild)
            End If
        End Function

        ' Adds muted role overwrite to new channels.
        Private Async Function ChannelCreatedHandler(e As ChannelCreateEventArgs) As Task
            Dim roleId = _db.GetGuildEntry(e.Guild.Id).Data.MutedRoleId
            Dim role As DiscordRole

            If roleId = 0 Then Return
            If Not e.Guild.Roles.ContainsKey(roleId) Then role = Await CreateGuildMutedRoleAsync(e.Guild)
            If role Is Nothing Then role = e.Guild.Roles(roleId)
        End Function

        Public Async Function CreateGuildMutedRoleAsync(guild As DiscordGuild) As Task(Of DiscordRole)
            Dim role = Await guild.CreateRoleAsync("Muted", Permissions.None, DiscordColor.Black, False, False, "Muted role generation for Omnia.")
            Await role.ModifyPositionAsync(0, "Muted role generation for Omnia.")

            Dim dbGuild = _db.GetGuildEntry(guild.Id)
            dbGuild.Data.MutedRoleId = role.Id
            _db.UpdateGuildEntry(dbGuild)

            For Each channel In guild.Channels.Values
                Await channel.AddOverwriteAsync(role, Permissions.None, Permissions.SendMessages Or Permissions.Speak, "Muted role configuration for Omnia.")
            Next

            Return role
        End Function
    End Class
End Namespace