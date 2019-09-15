Imports System.Collections.Concurrent
Imports System.Net
Imports System.Text.RegularExpressions
Imports Newtonsoft.Json
Imports Newtonsoft.Json.Linq
Imports OmniaDiscord.Entities.Media
Imports OmniaDiscord.Entities.Media.Bandcamp
Imports OmniaDiscord.Entities.Media.Instagram
Imports OmniaDiscord.Entities.Media.Soundcloud
Imports YoutubeExplode
Imports YoutubeExplode.Models

Namespace Utilities
    Public Class MediaRetrievalUtilities
        Private Shared _webClient As New WebClient
        Private Shared _bandcampRegex As New Regex("https?:\/\/[a-z0-9\\-]+?\.bandcamp\.com\/album|track\/[a-z0-9\-]+?\/?")
        Private Shared _clypRegex As New Regex("https?:\/\/clyp.it\/\w{8}")
        Private Shared _instagramRegex As New Regex("(https?:\/\/(www\.)?)?instagram\.com(\/p\/\w+\/?)")
        Private Shared _soundcloudRegex As New Regex("(https?:\/\/)?(www.)?(m\.)?soundcloud\.com\/[\w\-\.]+(\/)+[\w\-\.]+\/?")
        Private Shared _spotifyRegex As New Regex("(https?:\/\/open.spotify.com\/(track|user|artist|album)\/[a-zA-Z0-9]+(\/playlist\/[a-zA-Z0-9]+|)|spotify:(track|user|artist|album):[a-zA-Z0-9]+(:playlist:[a-zA-Z0-9]+|))")
        Private Shared _vimeoRegex As New Regex("(http:\/\/|https:\/\/)vimeo\.com\/([\w\/]+)(\?id=.*)?")
        Private Shared _youtubeRegex As New Regex("((?:https?:)?\/\/)?((?:www|m)\.)?((?:youtube\.com|youtu.be))(\/(?:[\w\-]+\?v=|embed\/|v\/)?)([\w\-]+)(\S+)?")

        Public Shared Async Function GetMultipleMediaAsync(urls As IEnumerable(Of String), Optional doParsePlaylists As Boolean = False) As Task(Of OmniaRetrievalResult)
            Dim goodUrls As New ConcurrentQueue(Of OmniaMediaInfo)
            Dim badUrls As New ConcurrentQueue(Of String)
            Dim trackTasks As New List(Of Task)

            For Each url In urls
                trackTasks.Add(Task.Run(Async Function()
                                            Dim media = Await GetMediaAsync(url)

                                            If media Is Nothing Then
                                                badUrls.Enqueue(url)
                                                Return
                                            End If

                                            If media.Type = OmniaMediaType.Track Or Not doParsePlaylists Then
                                                goodUrls.Enqueue(media)
                                            Else
                                                media.Tracks.ForEach(Sub(t) goodUrls.Enqueue(t))
                                            End If
                                        End Function))
            Next

            Await Task.WhenAll(trackTasks)
            Return New OmniaRetrievalResult(goodUrls, badUrls)
        End Function

        Public Shared Async Function GetMediaAsync(url As String) As Task(Of OmniaMediaInfo)
            Dim mediaInfo As OmniaMediaInfo = Nothing
            url = url.Trim

            If _youtubeRegex.IsMatch(url) Then
                mediaInfo = Await ResolveYouTubeAsync(url)
            ElseIf _soundcloudRegex.IsMatch(url) Then
                mediaInfo = Await ResolveSoundcloudAsync(url)
            ElseIf _spotifyRegex.IsMatch(url) Then
                mediaInfo = ResolveSpotify(url)
            ElseIf _bandcampRegex.IsMatch(url) Then
                mediaInfo = Await ResolveBandcampAsync(url)
            ElseIf _instagramRegex.IsMatch(url) Then
                mediaInfo = Await ResolveInstagramAsync(url)
            ElseIf _vimeoRegex.IsMatch(url) Then
                mediaInfo = ResolveVimeo(url)
            ElseIf _clypRegex.IsMatch(url) Then
                mediaInfo = ResolveClyp(url)
            End If

            If mediaInfo Is Nothing OrElse mediaInfo.Equals(New OmniaMediaInfo) Then Return Nothing
            If String.IsNullOrEmpty(mediaInfo.ThumbnailUrl) Then mediaInfo.ThumbnailUrl = $"{Bot.Config.ResourceUrl}/assets/omnia/{mediaInfo.Origin}.png"
            Return mediaInfo
        End Function

        Private Shared Async Function ResolveSoundcloudAsync(url As String) As Task(Of OmniaMediaInfo)
            Dim rawJson As String = Await _webClient.DownloadStringTaskAsync($"http://api.soundcloud.com/resolve?url={url}&representation=full&client_id={Bot.Config.SoundcloudClientId}")
            Dim jsonObject As JObject = JObject.Parse(rawJson)
            Dim mediaKind As String = jsonObject.Value(Of String)("kind")
            Dim mediaInfo As New OmniaMediaInfo

            If mediaKind = "track" Then
                Dim scTrack As SoundcloudTrack = jsonObject.ToObject(Of SoundcloudTrack)
                mediaInfo = SoundcloudTrackToMediaInfo(scTrack)

            ElseIf mediaKind = "playlist" Then
                Dim scPlaylist As SoundcloudPlaylist = jsonObject.ToObject(Of SoundcloudPlaylist)

                With mediaInfo
                    .Author = scPlaylist.Creator.Username
                    .Duration = TimeSpan.FromMilliseconds(scPlaylist.Duration)
                    .Origin = "Soundcloud"
                    .ThumbnailUrl = scPlaylist.ArtworkUrl
                    .Title = scPlaylist.Title
                    .Url = scPlaylist.Url

                    For Each track In scPlaylist.Tracks
                        mediaInfo.Tracks.Add(SoundcloudTrackToMediaInfo(track))
                    Next

                    If scPlaylist.Type = "album" Then
                        .Type = OmniaMediaType.Album
                    ElseIf scPlaylist.Type = "compilation" Or String.IsNullOrEmpty(scPlaylist.Type) Then
                        .Type = OmniaMediaType.Playlist
                    End If
                End With
            End If

            Return mediaInfo
        End Function

        Private Shared Async Function ResolveYouTubeAsync(url As String) As Task(Of OmniaMediaInfo)
            Dim mediaId As String = String.Empty
            Dim mediaInfo As OmniaMediaInfo
            Dim ytClient As New YoutubeClient

            If YoutubeClient.TryParseVideoId(url, mediaId) Then
                Try
                    Dim ytVideo As Video = Await ytClient.GetVideoAsync(mediaId)
                    mediaInfo = YoutubeVideoToMediaInfo(ytVideo)

                Catch ex As Exception
                    ' Video inaccessible.
                End Try

            ElseIf YoutubeClient.TryParsePlaylistId(url, mediaId) Then
                Dim ytPlaylist As Playlist = Await ytClient.GetPlaylistAsync(mediaId)
                Dim totalDuration As TimeSpan
                mediaInfo = New OmniaMediaInfo

                With mediaInfo
                    .Author = ytPlaylist.Author
                    .Origin = "YouTube"
                    .Title = ytPlaylist.Title
                    .Type = OmniaMediaType.Playlist
                    .Url = ytPlaylist.GetUrl

                End With

                For Each video In ytPlaylist.Videos
                    Dim tempMediaInfo As New OmniaMediaInfo

                    tempMediaInfo = YoutubeVideoToMediaInfo(video)
                    totalDuration = totalDuration.Add(tempMediaInfo.Duration)

                    mediaInfo.Tracks.Add(tempMediaInfo)
                Next

                mediaInfo.Duration = totalDuration

            ElseIf YoutubeClient.TryParseChannelId(url, mediaId) Then
                Return Nothing ' TODO
            End If

            Return mediaInfo
        End Function

        Private Shared Async Function ResolveBandcampAsync(url As String) As Task(Of OmniaMediaInfo)
            Dim rawJson As String = Await GetJsonFromWebPageSourceAsync(url, "var TralbumData = {", "};")
            Dim mediaInfo As New OmniaMediaInfo

            If Not String.IsNullOrWhiteSpace(rawJson) Then
                rawJson = New Regex("(?<root>url: "".+)"" \+ ""(?<album>.+"",)").Replace(rawJson, "${root}${album}")

                Dim settings = New JsonSerializerSettings With {
                    .NullValueHandling = NullValueHandling.Ignore,
                    .MissingMemberHandling = MissingMemberHandling.Ignore
                }
                Dim bcMedia As BandcampMediaInfo = JsonConvert.DeserializeObject(Of BandcampMediaInfo)(rawJson, settings)

                If bcMedia.MediaType = "track" Then
                    mediaInfo = BandcampTrackToMediaInfo(bcMedia.Tracks.First, bcMedia)

                ElseIf bcMedia.MediaType = "album" Then
                    Dim albumUri As New Uri(url)

                    With mediaInfo
                        .Author = bcMedia.Artist
                        .Origin = "Bandcamp"
                        .ThumbnailUrl = $"https://f4.bcbits.com/img/a{bcMedia.ArtId.ToString.PadLeft(10, "0"c)}_10.jpg"
                        .Title = bcMedia.AlbumInfo.Title
                        .Type = OmniaMediaType.Album
                        .Url = bcMedia.Url
                    End With

                    For Each bcTrack As BandcampMediaInfo.TrackInfo In bcMedia.Tracks.Where(Function(x) x.Mp3 IsNot Nothing).ToList
                        Dim trackInfo As OmniaMediaInfo = BandcampTrackToMediaInfo(bcTrack, bcMedia)

                        With mediaInfo
                            .Duration = .Duration.Add(trackInfo.Duration)
                            .Tracks.Add(trackInfo)
                        End With
                    Next

                End If
            End If

            Return mediaInfo
        End Function

        Private Shared Async Function ResolveInstagramAsync(url As String) As Task(Of OmniaMediaInfo)
            Dim rawjson As String = Await GetJsonFromWebPageSourceAsync(url, "window._sharedData = {", "};")
            Dim mediaInfo As New OmniaMediaInfo

            If Not String.IsNullOrWhiteSpace(rawjson) Then
                Dim instaInfo As InstagramMediaInfo = InstagramMediaInfo.CreateNew(rawjson)

                If instaInfo.IsVideo Then
                    With mediaInfo
                        .Author = instaInfo.Uploader
                        .DirectUrl = instaInfo.VideoUrl
                        .Duration = TimeSpan.FromSeconds(0)
                        .Origin = "Instagram"
                        .ThumbnailUrl = instaInfo.ThumbnailUrl
                        .Title = instaInfo.Title
                        .Type = OmniaMediaType.Track
                        .Url = url
                    End With
                End If
            End If

            Return mediaInfo
        End Function

        Private Shared Function ResolveSpotify(url As String) As OmniaMediaInfo
            Throw New NotImplementedException()
        End Function

        Private Shared Function ResolveVimeo(url As String) As OmniaMediaInfo
            Throw New NotImplementedException()
        End Function

        Private Shared Function ResolveClyp(url As String) As OmniaMediaInfo
            Throw New NotImplementedException()
        End Function

        Private Shared Function YoutubeVideoToMediaInfo(ytVideo As Video) As OmniaMediaInfo
            YoutubeVideoToMediaInfo = New OmniaMediaInfo With {
                .Author = ytVideo.Author,
                .DirectUrl = ytVideo.GetUrl,
                .Duration = ytVideo.Duration,
                .Origin = "YouTube",
                .ThumbnailUrl = ytVideo.Thumbnails.HighResUrl,
                .Title = ytVideo.Title,
                .Type = OmniaMediaType.Track,
                .Url = ytVideo.GetUrl
            }

            Return YoutubeVideoToMediaInfo
        End Function

        Private Shared Function BandcampTrackToMediaInfo(bcTrack As BandcampMediaInfo.TrackInfo, bcAlbum As BandcampMediaInfo) As OmniaMediaInfo
            BandcampTrackToMediaInfo = New OmniaMediaInfo With {
                .Author = bcAlbum.Artist,
                .DirectUrl = bcTrack.Mp3.DirectLink,
                .Duration = TimeSpan.FromSeconds(Math.Floor(bcTrack.Duration)),
                .Origin = "Bandcamp",
                .ThumbnailUrl = $"https://f4.bcbits.com/img/a{bcAlbum.ArtId.ToString.PadLeft(10, "0"c)}_10.jpg",
                .Title = bcTrack.Title,
                .Type = OmniaMediaType.Track,
                .Url = bcAlbum.Url
            }

            Return BandcampTrackToMediaInfo
        End Function

        Private Shared Function SoundcloudTrackToMediaInfo(scTrack As SoundcloudTrack) As OmniaMediaInfo
            SoundcloudTrackToMediaInfo = New OmniaMediaInfo With {
                .Author = scTrack.Uploader.Username,
                .DirectUrl = scTrack.GetDownloadUrl(),
                .Duration = TimeSpan.FromMilliseconds(scTrack.Duration),
                .Origin = "Soundcloud",
                .ThumbnailUrl = If(scTrack.ArtworkUrl, $"{Bot.Config.ResourceUrl}/assets/omnia/{ .Origin}.png"),
                .Title = scTrack.Title,
                .Type = OmniaMediaType.Track,
                .Url = scTrack.Url
            }

            Return SoundcloudTrackToMediaInfo
        End Function

        Private Shared Async Function GetJsonFromWebPageSourceAsync(url As String, startString As String, endString As String) As Task(Of String)
            Dim htmlCode As String

            Try
                htmlCode = Await _webClient.DownloadStringTaskAsync(url)
            Catch ex As Exception
                Return Nothing
            End Try

            Dim jsonTemp As String = htmlCode.Substring(htmlCode.IndexOf(startString) + startString.Length - 1)
            Dim json As String = jsonTemp.Substring(0, jsonTemp.IndexOf(endString) + 1)

            Return json
        End Function

    End Class
End Namespace