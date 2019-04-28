Imports Newtonsoft.Json

Namespace Entities.Media.Instagram

    Public Class InstagramMediaInfo

        ''' <summary>
        ''' The title of this media.
        ''' </summary>
        Public ReadOnly Property Title As String

        ''' <summary>
        ''' The uploader of this media.
        ''' </summary>
        Public ReadOnly Property Uploader As String

        ''' <summary>
        ''' The video URL. If this isn't a video, this will be empty.
        ''' </summary>
        Public ReadOnly Property VideoUrl As String

        ''' <summary>
        ''' The thumbnail image URL for this media.
        ''' </summary>
        Public ReadOnly Property ThumbnailUrl As String

        ''' <summary>
        ''' Whether or not this media is a video.
        ''' </summary>
        Public ReadOnly Property IsVideo As Boolean

        Private Sub New(title As String, username As String, vUrl As String, tUrl As String, isVid As Boolean)
            _Title = title
            _Uploader = username
            _VideoUrl = vUrl
            _ThumbnailUrl = tUrl
            _IsVideo = isVid
        End Sub

        Public Shared Function CreateNew(rawJson As String) As InstagramMediaInfo
            Dim instaObject As JsonObject = JsonConvert.DeserializeObject(Of JsonObject)(rawJson)
            Dim title As String

            If instaObject.MediaInfo.CaptionData.Edges.Count > 0 Then
                title = instaObject.MediaInfo.CaptionData.Edges.First.Node.Caption.Trim
                title = title.Replace(vbCr, " "c).Replace(vbLf, " "c).Replace(vbCrLf, " "c)
            Else
                title = "Instagram Video"
            End If

            Return New InstagramMediaInfo(title,
                                          instaObject.MediaInfo.MediaOwnership.UploaderUsername,
                                          instaObject.MediaInfo.VideoUrl,
                                          instaObject.MediaInfo.MediaThumbnail,
                                          instaObject.MediaInfo.IsVideo)
        End Function

        ' Automatically generated object. Slightly modified.
        Private Class JsonObject
            <JsonProperty("entry_data")>
            Private _entryData As Data

            <JsonIgnore>
            Public ReadOnly Property MediaInfo As InstagramMedia
                Get
                    Return _entryData.PostPage.First.Graphql.InstagramMedia
                End Get
            End Property

            Public Structure InstagramMedia

                <JsonProperty("id")>
                Public Property MediaId As String

                <JsonProperty("display_url")>
                Public Property MediaThumbnail As String

                <JsonProperty("is_video")>
                Public Property IsVideo As Boolean

                <JsonProperty("edge_media_to_caption")>
                Public Property CaptionData As EdgeMediaToCaption

                <JsonProperty("video_url")>
                Public Property VideoUrl As String

                <JsonProperty("owner")>
                Public Property MediaOwnership As Owner

            End Structure

            Public Structure Owner

                <JsonProperty("id")>
                Public Property UploaderId As String

                <JsonProperty("profile_pic_url")>
                Public Property UploaderProfilePicUrl As String

                <JsonProperty("username")>
                Public Property UploaderUsername As String

                <JsonProperty("full_name")>
                Public Property UploaderName As String

            End Structure

            Public Structure EdgeMediaToCaption

                <JsonProperty("edges")>
                Public Property Edges As Edge()

            End Structure

            Public Structure Edge

                <JsonProperty("node")>
                Public Property Node As Text

            End Structure

            Public Structure Text

                <JsonProperty("text")>
                Public Property Caption As String

            End Structure

            Private Structure Data

                <JsonProperty("PostPage")>
                Public Property PostPage As PostPage()

            End Structure

            Private Structure PostPage

                <JsonProperty("graphql")>
                Public Property Graphql As Graphql

            End Structure

            Private Structure Graphql

                <JsonProperty("shortcode_media")>
                Public Property InstagramMedia As InstagramMedia

            End Structure
        End Class

    End Class

End Namespace