Imports System.IO
Imports System.Reflection
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Exceptions
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Interactivity
Imports DSharpPlus.Lavalink
Imports DSharpPlus.VoiceNext
Imports Fclp
Imports Microsoft.Extensions.DependencyInjection
Imports Newtonsoft.Json
Imports OmniaDiscord.Commands
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Services
Imports OmniaDiscord.Services.Database
Imports OmniaDiscord.Services.Database.Entities
Imports OmniaDiscord.Services.Lavalink
Imports OmniaDiscord.Services.MediaRetrieval

Public Module Omnia
    Sub Main(args As String())
        Dim bot As New Bot(args)
        bot.RunAsync.GetAwaiter.GetResult()
    End Sub
End Module

Public Class Bot
    Private _services As IServiceProvider
    Private _runMode As OmniaRunMode

    Sub New(args As String())
        Dim argParser As New FluentCommandLineParser

        argParser.Setup(Of OmniaRunMode)("r"c, "runmode").Callback(Sub(r) _runMode = r).SetDefault(OmniaRunMode.Development)
        argParser.Parse(args)
    End Sub

    Public Async Function RunAsync() As Task
        Dim config As New Configuration
        Dim token As String = String.Empty
        Dim logLevel As LogLevel = LogLevel.Debug

        config.RunMode = _runMode

        Select Case _runMode
            Case OmniaRunMode.Development
                token = config.DiscordDevelopmentToken
                config.DefaultPrefix = "|"
            Case OmniaRunMode.Testing
                token = config.DiscordTestToken
                config.DefaultPrefix = "<"
            Case OmniaRunMode.Release
                token = config.DiscordReleaseToken
                config.DefaultPrefix = ">"
                logLevel = LogLevel.Info
        End Select

        Dim clientConfig As DiscordConfiguration = New DiscordConfiguration With {
            .Token = token,
            .TokenType = TokenType.Bot,
            .LogLevel = logLevel,
            .MessageCacheSize = 2048
        }

        Dim discordClient As New DiscordShardedClient(clientConfig)

        Await discordClient.UseLavalinkAsync()
        Await discordClient.UseVoiceNextAsync(New VoiceNextConfiguration With {.EnableIncoming = True})
        Await discordClient.UseInteractivityAsync(New InteractivityConfiguration)

        With New ServiceCollection
            .AddSingleton(config)
            .AddSingleton(discordClient)
            .AddSingleton(Of LogService)
            .AddSingleton(Of DatabaseService)
            .AddSingleton(Of LavalinkService)
            .AddSingleton(Of MediaRetrievalService)

            _services = .BuildServiceProvider
        End With

        Dim commandConfig As CommandsNextConfiguration = New CommandsNextConfiguration With {
            .EnableDefaultHelp = True,
            .IgnoreExtraArguments = True,
            .Services = _services,
            .PrefixResolver = AddressOf PrefixResolver
        }

        Dim cmdExtensions As IReadOnlyDictionary(Of Integer, CommandsNextExtension) = Await discordClient.UseCommandsNextAsync(commandConfig)
        Dim client As DiscordShardedClient = _services.GetRequiredService(Of DiscordShardedClient)
        Dim logger As LogService = _services.GetRequiredService(Of LogService)

        For shard As Integer = 0 To cmdExtensions.Count - 1
            cmdExtensions(shard).RegisterCommands(Assembly.GetExecutingAssembly)
            cmdExtensions(shard).SetHelpFormatter(Of HelpFormatter)()

            AddHandler cmdExtensions(shard).CommandErrored, Function(arg) CommandErroredHandler(arg, logger)
            AddHandler cmdExtensions(shard).CommandExecuted, Function(arg) CommandExecutedHandler(arg, logger)
        Next

        AddHandler client.DebugLogger.LogMessageReceived, Sub(sender, arg) logger.Print(arg.Level, arg.Application, arg.Message)
        AddHandler client.GuildAvailable, Function(arg) AddNewGuildToDatabase(arg)
        AddHandler client.ClientErrored, Function(arg)
                                             Dim ex As Exception = arg.Exception.InnerException
                                             Return logger.PrintAsync(LogLevel.Error, arg.EventName, $"'{ex.Message}':{Environment.NewLine}{ex.StackTrace}")
                                         End Function

        Await client.StartAsync
        Await Task.Delay(-1)
    End Function

    Private Async Function PrefixResolver(msg As DiscordMessage) As Task(Of Integer)
        Dim config As Configuration = _services.GetRequiredService(Of Configuration)

        If msg.GetStringPrefixLength(config.DefaultPrefix) <> -1 Then
            Return Await Task.FromResult(msg.GetStringPrefixLength(config.DefaultPrefix))

        Else
            Dim db As DatabaseService = _services.GetRequiredService(Of DatabaseService)
            Dim settings As GuildSettings = db.GetGuildSettings(msg.Channel.GuildId)

            If settings.Prefix IsNot Nothing AndAlso msg.GetStringPrefixLength(settings.Prefix) <> -1 Then
                Return Await Task.FromResult(msg.GetStringPrefixLength(settings.Prefix))

            Else
                Dim discord As DiscordShardedClient = _services.GetRequiredService(Of DiscordShardedClient)

                If msg.GetMentionPrefixLength(discord.CurrentUser) <> -1 Then
                    Dim botUser As DiscordUser = _services.GetRequiredService(Of DiscordShardedClient).CurrentUser
                    Return Await Task.FromResult(msg.GetMentionPrefixLength(botUser))
                End If
            End If
        End If

        Return Await Task.FromResult(-1)
    End Function

    Private Async Function CommandErroredHandler(arg As CommandErrorEventArgs, logger As LogService) As Task

        If arg.Command IsNot Nothing Then
            Await logger.PrintAsync(LogLevel.Error, "Command Service", $"'{arg.Command.QualifiedName}' errored in guild {arg.Context.Guild.Id}: '{arg.Exception}' {arg.Exception.Message}")

            Dim exception As ChecksFailedException = TryCast(arg.Exception, ChecksFailedException)
            If exception IsNot Nothing Then
                Dim embed As New DiscordEmbedBuilder With {
                .Color = DiscordColor.Red,
                .Title = "Unable To Execute Command"
            }

                For Each failedCheck As CheckBaseAttribute In exception.FailedChecks
                    If TryCast(failedCheck, RequireBotPermissionsAttribute) IsNot Nothing Then
                        Dim check As RequireBotPermissionsAttribute = CType(failedCheck, RequireBotPermissionsAttribute)
                        embed.Description &= $"I don't have the right permissions to execute this command. Required permissions: {check.Permissions.ToPermissionString}.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireUserPermissionsAttribute) IsNot Nothing Then
                        Dim check As RequireUserPermissionsAttribute = CType(failedCheck, RequireUserPermissionsAttribute)
                        embed.Description &= $"You don't have the right permissions to use this command. Required permissions: {check.Permissions.ToPermissionString}.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, CooldownAttribute) IsNot Nothing Then
                        Dim check As CooldownAttribute = CType(failedCheck, CooldownAttribute)
                        Dim remainingTime As String = Utilities.FormatTimespanToString(check.GetRemainingCooldown(arg.Context))
                        Dim scope As String = String.Empty

                        Select Case check.BucketType
                            Case CooldownBucketType.User
                                scope = "for you"
                            Case CooldownBucketType.Channel
                                scope = "in this channel"
                            Case CooldownBucketType.Guild
                                scope = "for this server"
                        End Select

                        embed.Description &= $"`{arg.Command.QualifiedName}` is on cooldown {scope}. Remaining time: {remainingTime}.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireGuildAttribute) IsNot Nothing Then
                        embed.Description &= $"This command can only be used on a server.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireDirectMessageAttribute) IsNot Nothing Then
                        embed.Description &= $"This command can only be used in a direct message.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireNsfwAttribute) IsNot Nothing Then
                        embed.Description &= $"This command can only be used in channels marked as NSFW.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireOwnerAttribute) IsNot Nothing Then
                        embed.Description &= $"This command can only be used by my creator.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireGuildOwnerAttribute) IsNot Nothing Then
                        embed.Description &= $"This command can only be used by the server owner.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireStaffAttribute) IsNot Nothing Then
                        embed.Description &= $"This command can only be used by those with a staff title.{Environment.NewLine}"

                    ElseIf TryCast(failedCheck, RequireTitleAttribute) IsNot Nothing Then
                        Dim check As RequireTitleAttribute = CType(failedCheck, RequireTitleAttribute)
                        embed.Description &= $"You need the title of `{check.MinimumTitle}` or higher to use this command.{Environment.NewLine}"

                        'ElseIf TryCast(failedCheck, RequireUserDjWhitelistedAttribute) IsNot Nothing Then
                        '    embed.Description &= $"You must be whitelisted as a DJ to use this command.{Environment.NewLine}"

                        'ElseIf TryCast(failedCheck, RequireVoteToSkipDisabledAttribute) IsNot Nothing Then
                        '    embed.Description &= $"You must have *vote to skip* disabled to use this command.{Environment.NewLine}"

                        'ElseIf TryCast(failedCheck, RequireDjWhitelistEnabledAttribute) IsNot Nothing Then
                        '    embed.Description &= $"You must have *DJ whitelist* enabled to use this command.{Environment.NewLine}"

                    End If
                Next

                Await arg.Context.RespondAsync(embed:=embed.Build)
            End If
        End If

    End Function

    Private Async Function CommandExecutedHandler(arg As CommandExecutionEventArgs, logger As LogService) As Task
        Await logger.PrintAsync(LogLevel.Info, "Command Service", $"{arg.Context.User.Username} ({arg.Context.User.Id}) executed '{arg.Command.QualifiedName}' in {arg.Context.Guild.Name} ({arg.Context.Guild.Id})")
    End Function

    Private Function AddNewGuildToDatabase(arg As GuildCreateEventArgs) As Task
        Dim db As DatabaseService = _services.GetRequiredService(Of DatabaseService)
        If db.DoesContainGuild(arg.Guild.Id) = False Then db.InitializeNewGuild(arg.Guild.Id)

        Return Task.CompletedTask
    End Function

    Public Class Configuration
        ' Discord Related Properties
        Public Property DefaultPrefix As String
        Public ReadOnly Property DiscordDevelopmentToken As String
        Public ReadOnly Property DiscordTestToken As String
        Public ReadOnly Property DiscordReleaseToken As String

        ' External Tokens and Keys
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
        End Sub
    End Class

    Public Enum OmniaRunMode
        Release = 1
        Testing = 2
        Development = 3
    End Enum

End Class