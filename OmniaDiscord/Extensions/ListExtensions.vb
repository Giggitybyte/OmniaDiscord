﻿Imports System.Runtime.CompilerServices

Public Module ListExtensions

    ''' <summary>
    ''' Removes a specified number of elements from a list and returns them.
    ''' </summary>
    <Extension()>
    Function TakeAndRemove(Of T)(ByRef collection As IList(Of T), count As Integer) As IList(Of T)
        SyncLock collection
            Dim value As IList(Of T) = collection.Take(count).ToList
            collection = collection.Skip(count).ToList
            Return value.ToList
        End SyncLock
    End Function
End Module
