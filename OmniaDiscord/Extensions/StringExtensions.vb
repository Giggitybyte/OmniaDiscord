Imports System.Runtime.CompilerServices

Namespace Extensions
    Module StringExtensions

        <Extension()>
        Public Function SplitAtOccurence(input As String, separator As Char, occurence As Integer) As List(Of String)
            ' https://is.gd/tkxmos

            Dim parts As String() = input.Split(separator)
            Dim partlist As New List(Of String)()
            Dim result As New List(Of String)()

            For i As Integer = 0 To parts.Length - 1

                If partlist.Count = occurence Then
                    result.Add(String.Join(separator.ToString(), partlist))
                    partlist.Clear()
                End If

                partlist.Add(parts(i))
                If i = parts.Length - 1 Then result.Add(String.Join(separator.ToString(), partlist))
            Next

            Return result
        End Function

    End Module

End Namespace