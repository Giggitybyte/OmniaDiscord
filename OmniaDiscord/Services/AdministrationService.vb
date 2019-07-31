Imports System.Collections.Concurrent
Imports System.Threading
Imports DSharpPlus
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs

Namespace Services
    Public Class AdministrationService
        Public ReadOnly Property SoftbanTokens As New ConcurrentDictionary(Of ULong, CancellationTokenSource)
        Private ReadOnly _db As DatabaseService

        Sub New(client As DiscordShardedClient, db As DatabaseService)
            _db = db

            AddHandler client.GuildMemberAdded, AddressOf MemberJoinHandler
            AddHandler client.GuildMemberRemoved, AddressOf MemberLeaveHandler
            AddHandler client.GuildMemberUpdated, AddressOf MemberUpdatedHandler
            AddHandler client.GuildRoleDeleted, AddressOf RoleDeletedHandler
            AddHandler client.ChannelCreated, AddressOf ChannelCreatedHandler
        End Sub

        ' Re-adds muted role to user who left the guild and rejoined.
        Private Async Function MemberJoinHandler(e As GuildMemberAddEventArgs) As Task
            If Not _db.GetGuildData(e.Guild.Id).MutedMembers.Contains(e.Member.Id) Then Return

            Dim roleId = _db.GetGuildData(e.Guild.Id).MutedRoleId
            Dim role As DiscordRole

            If Not e.Guild.Roles.ContainsKey(roleId) Then role = Await CreateGuildMutedRoleAsync(e.Guild)
            If role Is Nothing Then role = e.Guild.Roles(roleId)

            Await e.Member.GrantRoleAsync(role, "Re-muting previously muted user.")
        End Function

        ' Removes title from users that leave, are kicked, or are banned.
        Private Function MemberLeaveHandler(e As GuildMemberRemoveEventArgs) As Task
            If _db.GetGuildData(e.Guild.Id).StaffTitles.ContainsKey(e.Member.Id) Then _db.GetGuildData(e.Guild.Id).StaffTitles.Remove(e.Guild.Id)
            Return Task.CompletedTask
        End Function

        ' Re-adds muted role to user if it is manually removed from them.
        Private Async Function MemberUpdatedHandler(e As GuildMemberUpdateEventArgs) As Task
            If Not _db.GetGuildData(e.Guild.Id).MutedMembers.Contains(e.Member.Id) Then Return

            Dim roleId = _db.GetGuildData(e.Guild.Id).MutedRoleId
            If roleId = 0 Then Return

            If e.RolesBefore.Select(Function(r) r.Id).Contains(roleId) AndAlso Not e.RolesAfter.Select(Function(r) r.Id).Contains(roleId) Then
                Await e.Member.GrantRoleAsync(e.RolesBefore.FirstOrDefault(Function(r) r.Id = roleId), "Re-muting manually unmuted user.")
            End If
        End Function

        ' Re-creates muted role if it is deleted.
        Private Async Function RoleDeletedHandler(e As GuildRoleDeleteEventArgs) As Task
            If e.Role.Id = _db.GetGuildData(e.Guild.Id).MutedRoleId Then
                Dim settings = _db.GetGuildData(e.Guild.Id)
                settings.MutedRoleId = (Await CreateGuildMutedRoleAsync(e.Guild)).Id
                _db.UpdateGuildData(settings)
            End If
        End Function

        ' Adds muted role overwrite to new channels.
        Private Async Function ChannelCreatedHandler(e As ChannelCreateEventArgs) As Task
            Dim roleId = _db.GetGuildData(e.Guild.Id).MutedRoleId
            Dim role As DiscordRole

            If roleId = 0 Then Return
            If Not e.Guild.Roles.ContainsKey(roleId) Then role = Await CreateGuildMutedRoleAsync(e.Guild)
            If role Is Nothing Then role = e.Guild.Roles(roleId)
        End Function

        Public Async Function CreateGuildMutedRoleAsync(guild As DiscordGuild) As Task(Of DiscordRole)
            Dim role = Await guild.CreateRoleAsync("Muted", Permissions.None, DiscordColor.Black, False, False, "Muted role generation for Omnia.")
            Await role.ModifyPositionAsync(0, "Muted role generation for Omnia.")

            Dim settings = _db.GetGuildData(guild.Id)
            settings.MutedRoleId = role.Id
            _db.UpdateGuildData(settings)

            For Each channel In guild.Channels.Values
                Await channel.AddOverwriteAsync(role, Permissions.None, Permissions.SendMessages Or Permissions.Speak, "Muted role configuration for Omnia.")
            Next

            Return role
        End Function
    End Class
End Namespace