Imports System.Collections.Concurrent
Imports System.Threading

Namespace Services
    Public Class SoftbanService

        ' The best service.
        Public Property BanCancellationTokens As ConcurrentDictionary(Of ULong, CancellationTokenSource)

        Sub New()
            BanCancellationTokens = New ConcurrentDictionary(Of ULong, CancellationTokenSource)
        End Sub

    End Class
End Namespace