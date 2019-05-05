Imports System.Runtime.CompilerServices

Public Module DoubleExtenstions

    <Extension()>
    Public Function ToStringNoRounding(value As Double, Optional formatting As String = "N2") As String
        ' https://is.gd/mc9VMx

        Dim truncatedValue As Double = Math.Truncate(value * 100) / 100
        Dim format As String = $"{{0:{formatting}}}"

        Return String.Format("{0:N2}", truncatedValue)
    End Function

End Module
