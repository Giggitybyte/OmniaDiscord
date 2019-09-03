Imports DSharpPlus
Imports LiteDB
Imports OmniaDiscord.Entities.Database

Namespace Services
    Public Class DatabaseService
        Private _db As LiteDatabase

        Sub New(logger As LogService)
            _db = New LiteDatabase($"filename=Omnia.db;password={Bot.Config.DatabasePassword}")
            _db.GetCollection(Of GuildEntry)("guilds").EnsureIndex(Function(e) e.GuildId)
            _db.GetCollection(Of UserEntry)("users").EnsureIndex(Function(e) e.UserId)

            logger.Print(LogLevel.Info, "Database Service", "Service constructed.")
        End Sub

        Public Function GetGuildEntry(guildId As ULong) As GuildEntry
            Dim entry = _db.GetCollection(Of GuildEntry)("guilds").FindOne(Function(g) g.GuildId = guildId)

            If entry Is Nothing Then
                entry = New GuildEntry With {.GuildId = guildId}
                _db.GetCollection(Of GuildEntry)("guilds").Insert(entry)
            End If

            Return entry
        End Function

        Public Sub UpdateGuildEntry(entry As GuildEntry)
            _db.GetCollection(Of GuildEntry)("guilds").Upsert(entry)
        End Sub

        Public Function GetUserEntry(userId As ULong) As UserEntry
            Dim entry = _db.GetCollection(Of UserEntry)("users").FindOne(Function(g) g.UserId = userId)
            If entry Is Nothing Then entry = New UserEntry With {.UserId = userId}
            Return entry
        End Function

        Public Sub UpdateUserEntry(entry As UserEntry)
            _db.GetCollection(Of UserEntry)("users").Upsert(entry)
        End Sub
    End Class
End Namespace