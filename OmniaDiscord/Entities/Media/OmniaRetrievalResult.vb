Namespace Entities.Media
    Public Structure OmniaRetrievalResult
        Public ReadOnly Property ValidUrls As List(Of OmniaMediaInfo)
        Public ReadOnly Property InvalidUrls As List(Of String)

        Sub New(valid As IEnumerable(Of OmniaMediaInfo), invalid As IEnumerable(Of String))
            _ValidUrls = New List(Of OmniaMediaInfo)(valid)
            _InvalidUrls = New List(Of String)(invalid)
        End Sub
    End Structure
End Namespace