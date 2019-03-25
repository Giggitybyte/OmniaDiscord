Imports LiteDB
Imports OmniaDiscord.Entites

Namespace Services

    Public Class DatabaseService
        Private ReadOnly _db As LiteDatabase
        Private ReadOnly _logger As LogService

        Sub New(logger As LogService)
            _db = New LiteDatabase("Omnia.db")
            _logger = logger

            Dim guildData As LiteCollection(Of GuildData) = _db.GetCollection(Of GuildData)("guildData")
            Dim guildSettings As LiteCollection(Of GuildSettings) = _db.GetCollection(Of GuildSettings)("guildSettings")

            guildData.EnsureIndex(Function(d) d.GuildId)
            guildSettings.EnsureIndex(Function(s) s.GuildId)

            _logger.Print(DSharpPlus.LogLevel.Info, "Database Service", "Service initalized.")
        End Sub

        Public Sub InitializeNewGuild(guildId As ULong)
            Dim guildData As LiteCollection(Of GuildData) = _db.GetCollection(Of GuildData)("guildData")
            Dim guildSettings As LiteCollection(Of GuildSettings) = _db.GetCollection(Of GuildSettings)("guildSettings")

            Dim data As New GuildData With {.GuildId = guildId}
            Dim settings As New GuildSettings With {.GuildId = guildId}

            guildData.Insert(data)
            guildSettings.Insert(settings)

            _logger.Print(DSharpPlus.LogLevel.Info, "Database Service", $"Created new database entries for guild {guildId}.")
        End Sub

        Public Function GetGuildData(guildId As ULong) As GuildData
            Dim guildData As LiteCollection(Of GuildData) = _db.GetCollection(Of GuildData)("guildData")
            Dim data As GuildData = guildData.FindOne(Function(d) d.GuildId = guildId)

            Return data
        End Function

        Public Sub UpdateGuildData(updatedData As GuildData)
            Dim guildData As LiteCollection(Of GuildData) = _db.GetCollection(Of GuildData)("guildData")
            Dim success As Boolean = guildData.Update(updatedData)

            If success Then
                _logger.Print(DSharpPlus.LogLevel.Debug, "Database Service", $"Updated data for guild {updatedData.GuildId}.")
            Else
                _logger.Print(DSharpPlus.LogLevel.Warning, "Database Service", $"Couldn't update guild data for guild {updatedData.GuildId}.")
            End If
        End Sub

        Public Function GetGuildSettings(guildId As ULong) As GuildSettings
            Dim guildSettings As LiteCollection(Of GuildSettings) = _db.GetCollection(Of GuildSettings)("guildSettings")
            Dim settings As GuildSettings = guildSettings.FindOne(Function(s) s.GuildId = guildId)

            Return settings
        End Function

        Public Sub UpdateGuildSettings(updatedSettings As GuildSettings)
            Dim guildSettings As LiteCollection(Of GuildSettings) = _db.GetCollection(Of GuildSettings)("guildSettings")
            Dim success As Boolean = guildSettings.Update(updatedSettings)

            If success Then
                _logger.Print(DSharpPlus.LogLevel.Debug, "Database Service", $"Updated settings for guild {updatedSettings.GuildId}.")
            Else
                _logger.Print(DSharpPlus.LogLevel.Warning, "Database Service", $"Couldn't update settings for guild {updatedSettings.GuildId}.")
            End If
        End Sub

        Public Function DoesContainGuild(guildId As ULong) As Boolean
            Dim guildData As LiteCollection(Of GuildData) = _db.GetCollection(Of GuildData)("guildData")
            Dim guildSettings As LiteCollection(Of GuildSettings) = _db.GetCollection(Of GuildSettings)("guildSettings")

            If guildData.Exists(Function(d) d.GuildId = guildId) And
               guildSettings.Exists(Function(s) s.GuildId = guildId) Then

                Return True
            End If

            Return False
        End Function

    End Class

End Namespace