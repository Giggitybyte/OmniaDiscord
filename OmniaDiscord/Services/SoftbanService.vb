Imports System.Collections.Concurrent
Imports System.Threading

Namespace Services
    Public Class SoftbanService

        ' I could have just passed in New ConcurrentDictionary(Of ..., ...)
        ' into the service collection, but that's fucking lame.
        Public Property BanCancellationTokens As ConcurrentDictionary(Of ULong, CancellationTokenSource)

        Sub New()
            BanCancellationTokens = New ConcurrentDictionary(Of ULong, CancellationTokenSource)
        End Sub

    End Class
End Namespace