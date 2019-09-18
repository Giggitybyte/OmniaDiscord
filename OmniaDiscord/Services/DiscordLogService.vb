Imports System.Threading
Imports DSharpPlus

Namespace Services

    ' Translated verbatim to VB from Kiritsu's FoxBot.
    ' https://github.com/Kiritsu/FoxBot/blob/master/src/Fox/Services/LogService.cs
    Public Class DiscordLogService
        Private ReadOnly _semaphore As SemaphoreSlim

        Public Sub New()
            _semaphore = New SemaphoreSlim(1)
        End Sub

        Public Sub Print(ByVal level As LogLevel, ByVal name As String, ByVal message As String)
            PrintAsync(level, name, message).GetAwaiter().GetResult()
        End Sub

        Public Async Function PrintAsync(ByVal level As LogLevel, ByVal name As String, ByVal message As String) As Task
            Await _semaphore.WaitAsync()

            Try
                Console.ForegroundColor = ConsoleColor.DarkGreen
                Console.Write($"[{Date.Now.ToString}] ")

                Select Case level
                    Case LogLevel.Debug
                        Console.ForegroundColor = ConsoleColor.Gray
                    Case LogLevel.Info
                        Console.ForegroundColor = ConsoleColor.White
                    Case LogLevel.Warning
                        Console.ForegroundColor = ConsoleColor.DarkYellow
                    Case LogLevel.[Error]
                        Console.ForegroundColor = ConsoleColor.DarkRed
                    Case LogLevel.Critical
                        Console.BackgroundColor = ConsoleColor.DarkRed
                        Console.ForegroundColor = ConsoleColor.Black
                    Case Else
                        Console.ForegroundColor = ConsoleColor.White
                End Select

                Console.Write($"[{level}]")
                Console.ResetColor()
                Console.WriteLine($" [{name}] {message}")
            Finally
                _semaphore.Release()
            End Try
        End Function
    End Class
End Namespace