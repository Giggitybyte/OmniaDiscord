Imports System.Runtime.CompilerServices

Public Module DoubleExtenstions
    <Extension()>
    Public Function ToStringNoRounding(value As Double, Optional formatting As String = "N2") As String
        Dim truncatedValue As Double = Math.Truncate(value * 100) / 100
        Dim format As String = $"{{0:{formatting}}}"

        If truncatedValue.Equals(Double.NaN) Then Return "0.00"
        Return String.Format(format, truncatedValue)
    End Function
End Module
