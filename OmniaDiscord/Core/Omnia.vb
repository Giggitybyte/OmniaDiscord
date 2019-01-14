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
Imports Fclp
Imports Microsoft.Extensions.DependencyInjection
Imports Newtonsoft.Json
Imports OmniaDiscord.Commands
Imports OmniaDiscord.Commands.Checks
Imports OmniaDiscord.Services
Imports OmniaDiscord.Services.Entities.Database

Namespace Core
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

            Dim discordClient As New DiscordClient(New DiscordConfiguration With {
                                                .Token = token,
                                                .TokenType = TokenType.Bot,
                                                .LogLevel = logLevel,
                                                .MessageCacheSize = 2048
                                               })

            Dim discordLavalink As LavalinkExtension = discordClient.UseLavalink()
            Dim discordInteractivity As InteractivityExtension = discordClient.UseInteractivity(New InteractivityConfiguration)


            With New ServiceCollection
                .AddSingleton(config)
                .AddSingleton(discordClient)
                .AddSingleton(discordLavalink)
                .AddSingleton(discordInteractivity)
                .AddSingleton(Of LogService)
                .AddSingleton(Of DatabaseService)

                _services = .BuildServiceProvider
            End With

            Dim discordCommands As CommandsNextExtension = discordClient.UseCommandsNext(New CommandsNextConfiguration With {
                                                                  .EnableDefaultHelp = True,
                                                                  .IgnoreExtraArguments = True,
                                                                  .Services = _services,
                                                                  .PrefixResolver = AddressOf PrefixResolver
                                                              })

            discordCommands.RegisterCommands(Assembly.GetExecutingAssembly)
            discordCommands.SetHelpFormatter(Of HelpFormatter)()

            Dim client As DiscordClient = _services.GetRequiredService(Of DiscordClient)
            Dim logger As LogService = _services.GetRequiredService(Of LogService)

            AddHandler client.DebugLogger.LogMessageReceived, Sub(sender, arg) logger.Print(arg.Level, arg.Application, arg.Message)
            AddHandler client.GuildAvailable, Function(arg) AddNewGuildToDatabase(arg)

            AddHandler client.GetCommandsNext.CommandErrored, Function(arg) CommandErroredHandler(arg)
            AddHandler client.GetCommandsNext.CommandExecuted, Function(arg) CommandExecutedHandler(arg)

            Await client.ConnectAsync
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
                    Dim discord As DiscordClient = _services.GetRequiredService(Of DiscordClient)

                    If msg.GetMentionPrefixLength(discord.CurrentUser) <> -1 Then
                        Dim botUser As DiscordUser = _services.GetRequiredService(Of DiscordClient).CurrentUser
                        Return Await Task.FromResult(msg.GetMentionPrefixLength(botUser))
                    End If
                End If
            End If

            Return Await Task.FromResult(-1)
        End Function

        Private Async Function CommandErroredHandler(arg As CommandErrorEventArgs) As Task

            If arg.Command IsNot Nothing Then
                Dim logger As LogService = _services.GetRequiredService(Of LogService)
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

        Private Async Function CommandExecutedHandler(arg As CommandExecutionEventArgs) As Task
            Dim logger As LogService = _services.GetRequiredService(Of LogService)
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

            ' Server Addresses
            Public ReadOnly Property ServerAddresses As New Dictionary(Of String, String)

            ' Miscellaneous
            Public ReadOnly Property ResourcesUrl As String
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
            End Sub
        End Class
    End Class
End Namespace