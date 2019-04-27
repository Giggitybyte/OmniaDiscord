Namespace Entites.Database
    Public Structure GuildCase
        Public Property Timestamp As TimeSpan
        Public Property Action As GuildAction
        Public Property ResponsibleUser As ULong
        Public Property TargetUser As ULong
        Public Property Reason As String
        Public Property Duration As TimeSpan
    End Structure
End Namespace