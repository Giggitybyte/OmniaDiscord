Namespace Entities.Database
    Public Class UserEntry
        Public Property Id As Integer ' Database ID.
        Public Property UserId As ULong ' Discord user ID associated with this entry.

        Public Property ToxicityPoints As UShort ' Think of a better name for this.

        Public Property Reminders As List(Of UserReminder) ' A collection of user made reminders.
        Public Property GameAccounts As List(Of UserGameAccount) ' A collection of all game accounts associated with this user.
        Public Property Cards As List(Of UserCard) ' A collection of all payment methods for this user.
    End Class
End Namespace