Imports System.Reflection
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq

Namespace Entities

    ' https://stackoverflow.com/a/33094930
    Public Class JsonPathConverter
        Inherits JsonConverter

        Public Overrides Function ReadJson(reader As JsonReader, objectType As Type, existingValue As Object, serializer As JsonSerializer) As Object
            Dim jo As JObject = JObject.Load(reader)
            Dim targetObj As Object = Activator.CreateInstance(objectType)

            For Each prop As PropertyInfo In objectType.GetProperties().Where(Function(p) p.CanRead AndAlso p.CanWrite)
                Dim att As JsonPropertyAttribute = prop.GetCustomAttributes(True).OfType(Of JsonPropertyAttribute)().FirstOrDefault()
                Dim jsonPath As String = (If(att IsNot Nothing, att.PropertyName, prop.Name))
                Dim token As JToken = jo.SelectToken(jsonPath)

                If token IsNot Nothing AndAlso token.Type <> JTokenType.Null Then
                    Dim value As Object = token.ToObject(prop.PropertyType, serializer)
                    prop.SetValue(targetObj, value, Nothing)
                End If
            Next

            Return targetObj
        End Function

        Public Overrides Function CanConvert(ByVal objectType As Type) As Boolean
            Return False
        End Function

        Public Overrides ReadOnly Property CanWrite As Boolean
            Get
                Return False
            End Get
        End Property

        Public Overrides Sub WriteJson(ByVal writer As JsonWriter, ByVal value As Object, ByVal serializer As JsonSerializer)
            Throw New NotImplementedException()
        End Sub
    End Class

End Namespace