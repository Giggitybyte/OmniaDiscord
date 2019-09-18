Imports Lavalink4NET.Logging

Namespace Services
    Public Class LavalinkLogService
        Implements ILogger

        Private _dLogger As DiscordLogService

        Sub New(dLogger As DiscordLogService)
            _dLogger = dLogger
        End Sub

        Public Async Sub Log(source As Object, message As String, Optional level As LogLevel = LogLevel.Information, Optional exception As Exception = Nothing) Implements ILogger.Log
            Dim discordLogLevel As DSharpPlus.LogLevel

            Select Case level
                Case LogLevel.Debug, LogLevel.Trace
                    discordLogLevel = DSharpPlus.LogLevel.Debug
                Case LogLevel.Error
                    discordLogLevel = DSharpPlus.LogLevel.Error
                Case LogLevel.Information
                    discordLogLevel = DSharpPlus.LogLevel.Info
                Case LogLevel.Warning
                    discordLogLevel = DSharpPlus.LogLevel.Warning
            End Select

            Await _dLogger.PrintAsync(discordLogLevel, "Lavalink", message)
        End Sub
    End Class
End Namespace