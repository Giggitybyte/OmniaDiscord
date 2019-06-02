Imports System.Collections.Concurrent
Imports System.Threading

Namespace Services
    Public Class SoftbanTokenService
        Public ReadOnly Property SoftbanTokens As New ConcurrentDictionary(Of ULong, CancellationTokenSource)
    End Class
End Namespace