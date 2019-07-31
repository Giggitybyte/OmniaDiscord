Imports OmniaDiscord.Entities.Database

Namespace Entities.Attributes

    <AttributeUsage(AttributeTargets.Property, AllowMultiple:=False, Inherited:=False)>
    Public Class GuildSettingAttribute
        Inherits Attribute

        Public ReadOnly Property DisplayName As String
        Public ReadOnly Property UserSetKey As String
        Public ReadOnly Property ValidSetType As Type
        Public ReadOnly Property RequiredTitle As GuildTitle

        Sub New(name As String, setKey As String, setType As Type, title As GuildTitle)
            _DisplayName = name
            _RequiredTitle = title
            _UserSetKey = setKey
            _ValidSetType = setType
        End Sub
    End Class

End Namespace