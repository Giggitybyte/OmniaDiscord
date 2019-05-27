Namespace Entities.Database
    Public Structure GuildCase
        Public Property Timestamp As TimeSpan ' When this occured
        Public Property Action As GuildAction ' What happened
        Public Property ResponsibleUser As ULong ' Who initiated it
        Public Property TargetUser As ULong ' Who the target of the action is
        Public Property Reason As String ' Why the responsible user did it
        Public Property Duration As TimeSpan ' If applicable, how long the action will last
    End Structure
End Namespace