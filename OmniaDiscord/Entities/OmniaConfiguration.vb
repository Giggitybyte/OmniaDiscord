Imports System.IO
Imports System.Text
Imports Newtonsoft.Json

Namespace Entities
    Public Class OmniaConfiguration
        ' Discord Related Properties
        Public Property DefaultPrefix As String
        Public ReadOnly Property DiscordDevelopmentToken As String
        Public ReadOnly Property DiscordTestToken As String
        Public ReadOnly Property DiscordReleaseToken As String

        ' External Tokens and Keys
        Public ReadOnly Property DatabasePassword As String
        Public ReadOnly Property LavalinkPasscode As String
        Public ReadOnly Property SoundcloudClientId As String
        Public ReadOnly Property YoutubeApiKey As String

        Public ReadOnly Property FortniteApiKey As String
        Public ReadOnly Property RainbowSixApiPasscode As String

        ' Miscellaneous
        Public ReadOnly Property LavalinkIpAddress As String
        Public ReadOnly Property ResourceUrl As String
        Public Property RunMode As OmniaRunMode

        Sub New()
            Dim config As New Dictionary(Of String, String)

            Using fileStream As FileStream = File.OpenRead("config.json")
                Dim configJson As String = New StreamReader(fileStream, New UTF8Encoding(False)).ReadToEnd()
                config = JsonConvert.DeserializeObject(Of Dictionary(Of String, String))(configJson)
            End Using

            _DiscordDevelopmentToken = config("discorddevelopmenttoken")
            _DiscordTestToken = config("discordtesttoken")
            _DiscordReleaseToken = config("discordreleasetoken")
            _FortniteApiKey = config("ftrnapikey")
            _SoundcloudClientId = config("soundcloudclientid")
            _YoutubeApiKey = config("youtubeapikey")
            _LavalinkPasscode = config("lavalinkpasscode")
            _RainbowSixApiPasscode = config("r6apipasscode")
            _LavalinkIpAddress = config("lavalinkip")
            _ResourceUrl = config("resourceurl")
            _DatabasePassword = config("databasepassword")
        End Sub
    End Class
End Namespace