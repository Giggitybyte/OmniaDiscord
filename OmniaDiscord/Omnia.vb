Imports System.Reflection
Imports System.Text
Imports DSharpPlus
Imports DSharpPlus.CommandsNext
Imports DSharpPlus.CommandsNext.Attributes
Imports DSharpPlus.CommandsNext.Exceptions
Imports DSharpPlus.Entities
Imports DSharpPlus.EventArgs
Imports DSharpPlus.Interactivity
Imports Humanizer
Imports Lavalink4NET
Imports Lavalink4NET.DSharpPlus
Imports Lavalink4NET.Logging
Imports Lavalink4NET.MemoryCache
Imports Lavalink4NET.Tracking
Imports Microsoft.Extensions.DependencyInjection
Imports OmniaDiscord.Commands
Imports OmniaDiscord.Entities
Imports OmniaDiscord.Entities.Attributes
Imports OmniaDiscord.Services
Imports CommandNotFoundException = DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException
Imports LogLevel = DSharpPlus.LogLevel

Public Module Omnia
    Sub Main(args As String())
        Dim bot As New Bot(args)
        bot.RunAsync.GetAwaiter.GetResult()
    End Sub
End Module

Public Class Bot
    Public Shared ReadOnly Property Config As New OmniaConfiguration
    Private _services As IServiceProvider
    Private _runMode As OmniaRunMode = OmniaRunMode.Development

    Sub New(args As String())
        If args.Length = 2 AndAlso args(0) = "-r" Then [Enum].TryParse(GetType(OmniaRunMode), args(1), _runMode)
    End Sub

    Public Async Function RunAsync() As Task
        Dim token As String = String.Empty
        Dim logLevel As LogLevel = logLevel.Debug

        Select Case _runMode
            Case OmniaRunMode.Development
                token = Config.DiscordDevelopmentToken
                Config.DefaultPrefix = "|"
            Case OmniaRunMode.Testing
                token = Config.DiscordTestToken
                Config.DefaultPrefix = "<"
            Case OmniaRunMode.Release
                token = Config.DiscordReleaseToken
                Config.DefaultPrefix = ">"
                logLevel = logLevel.Info
        End Select

        With New ServiceCollection
            .AddSingleton(Of DiscordClient)
            .AddSingleton(Of IDiscordClientWrapper, DiscordClientWrapper)
            .AddSingleton(Of IAudioService, LavalinkNode)
            .AddSingleton(Of ILavalinkCache, LavalinkCache)
            .AddSingleton(Of ILogger, LavalinkLogService)
            .AddSingleton(Of InactivityTrackingService)

            .AddSingleton(New DiscordConfiguration With {
                .Token = token,
                .LogLevel = logLevel,
                .MessageCacheSize = 2048
            })

            .AddSingleton(New LavalinkNodeOptions With {
                .WebSocketUri = $"ws://{Config.LavalinkIpAddress}:2333",
                .RestUri = $"http://{Config.LavalinkIpAddress}:2333",
                .Password = Config.LavalinkPasscode,
                .CacheTime = TimeSpan.FromHours(1),
                .DisconnectOnStop = False
            })

            .AddSingleton(New InactivityTrackingOptions With {
                .PollInterval = TimeSpan.FromSeconds(15),
                .DisconnectDelay = TimeSpan.FromSeconds(15)
            })

            .AddSingleton(Of DiscordLogService)
            .AddSingleton(Of AdministrationService)
            .AddSingleton(Of DatabaseService)
            .AddSingleton(Of LobbySystemService)

            _services = .BuildServiceProvider
        End With

        Dim logger As DiscordLogService = _services.GetRequiredService(Of DiscordLogService)
        Dim client = _services.GetRequiredService(Of DiscordClient)

        Await logger.PrintAsync(logLevel.Info, "Initialization", $"Omnia has been started in {_runMode} mode.")
        client.UseInteractivity(New InteractivityConfiguration)

        Dim cmdExtension = client.UseCommandsNext(New CommandsNextConfiguration With {
            .Services = _services,
            .PrefixResolver = AddressOf PrefixResolver
        })

        cmdExtension.RegisterCommands(Assembly.GetExecutingAssembly)
        cmdExtension.SetHelpFormatter(Of HelpFormatter)()

        AddHandler cmdExtension.CommandErrored, Function(arg) CommandErroredHandler(arg, logger)
        AddHandler cmdExtension.CommandExecuted, Function(arg) CommandExecutedHandler(arg, logger)

        AddHandler client.DebugLogger.LogMessageReceived, Async Sub(sender, arg) Await logger.PrintAsync(arg.Level, arg.Application, arg.Message)
        AddHandler client.GuildCreated, AddressOf GuildCreatedHandler
        AddHandler client.ClientErrored, Function(arg) ClientErroredHandler(arg, logger)
        AddHandler client.Ready, AddressOf ClientReadyHandler

        Await client.ConnectAsync()
        Await Task.Delay(-1)
    End Function

    Private Async Function PrefixResolver(msg As DiscordMessage) As Task(Of Integer)
        Dim guild = _services.GetRequiredService(Of DatabaseService).GetGuildEntry(msg.Channel.GuildId)
        If guild.Settings.Prefix IsNot Nothing AndAlso msg.GetStringPrefixLength(guild.Settings.Prefix) <> -1 Then
            Return Await Task.FromResult(msg.GetStringPrefixLength(guild.Settings.Prefix))
        End If

        If guild.Settings.Prefix Is Nothing AndAlso msg.GetStringPrefixLength(Config.DefaultPrefix) <> -1 Then
            Return Await Task.FromResult(msg.GetStringPrefixLength(Config.DefaultPrefix))
        End If

        Dim discord = _services.GetRequiredService(Of DiscordClient)
        If msg.GetMentionPrefixLength(discord.CurrentUser) <> -1 Then
            Return Await Task.FromResult(msg.GetMentionPrefixLength(discord.CurrentUser))
        End If

        Return Await Task.FromResult(-1)
    End Function

    Private Function GuildCreatedHandler(arg As GuildCreateEventArgs) As Task
        Dim db As DatabaseService = _services.GetRequiredService(Of DatabaseService)
        db.GetGuildEntry(arg.Guild.Id)
        Return Task.CompletedTask
    End Function

    Private Function ClientErroredHandler(arg As ClientErrorEventArgs, logger As DiscordLogService) As Task
        Dim ex As Exception = arg.Exception.InnerException
        Return logger.PrintAsync(LogLevel.Error, arg.EventName, $"'{ex.Message}':{Environment.NewLine}{ex.StackTrace}")
    End Function

    Private Async Function ClientReadyHandler(arg As ReadyEventArgs) As Task
        ' Calling these services to initialize them.
        _services.GetRequiredService(Of LobbySystemService)
        _services.GetRequiredService(Of AdministrationService)

        ' Setup Lavalink
        Dim lavalink As LavalinkNode = _services.GetRequiredService(Of IAudioService)
        Await lavalink.InitializeAsync

        Dim tracking = _services.GetRequiredService(Of InactivityTrackingService)
        tracking.AddTracker(DefaultInactivityTrackers.UsersInactivityTracker)
        tracking.BeginTracking()
    End Function

    Private Async Function CommandErroredHandler(arg As CommandErrorEventArgs, logger As DiscordLogService) As Task
        If arg.Command Is Nothing Or TypeOf arg.Exception Is CommandNotFoundException Or
            arg.Exception.Message.Contains("Could not find a suitable overload for the command") Then Return

        Dim builder As New StringBuilder
        Dim channelPerms = arg.Context.Channel.PermissionsFor(arg.Context.Guild.CurrentMember)
        Dim guildPerms = If(arg.Context.Guild.Permissions, Permissions.None)
        If Not (channelPerms.HasPermission(Permissions.SendMessages) Or guildPerms.HasPermission(Permissions.Administrator)) Then Return

        If TypeOf arg.Exception Is ChecksFailedException Then
            Dim exception = DirectCast(arg.Exception, ChecksFailedException)

            For Each failedCheck In exception.FailedChecks
                If TypeOf failedCheck Is RequireBotPermissionsAttribute Then
                    Dim check = DirectCast(failedCheck, RequireBotPermissionsAttribute)
                    builder.AppendLine($"Omnia needs the following permissions to execute this command: {check.Permissions.ToPermissionString}")

                ElseIf TypeOf failedCheck Is RequireUserPermissionsAttribute Then
                    Dim check = DirectCast(failedCheck, RequireUserPermissionsAttribute)
                    builder.AppendLine($"You need the following permissions to use this command: {check.Permissions.ToPermissionString}")

                ElseIf TypeOf failedCheck Is CooldownAttribute Then
                    Dim check = DirectCast(failedCheck, CooldownAttribute)
                    Dim remainingTime = check.GetRemainingCooldown(arg.Context).Humanize
                    Dim scope = String.Empty

                    Select Case check.BucketType
                        Case CooldownBucketType.User
                            scope = "for you"
                        Case CooldownBucketType.Channel
                            scope = "in this channel"
                        Case CooldownBucketType.Guild
                            scope = "for this server"
                    End Select

                    builder.AppendLine($"`{arg.Command.QualifiedName}` is on cooldown {scope}. Remaining time: {remainingTime}.")

                ElseIf TypeOf failedCheck Is RequireGuildAttribute Then
                    builder.AppendLine("This command can only be used on a server.")

                ElseIf TypeOf failedCheck Is RequireDirectMessageAttribute Then
                    builder.AppendLine("This command can only be used in a direct message.")

                ElseIf TypeOf failedCheck Is RequireNsfwAttribute Then
                    builder.AppendLine("This command can only be used in channels marked as NSFW.")

                ElseIf TypeOf failedCheck Is RequireOwnerAttribute Then
                    builder.AppendLine("This command can only be used by my creator.")

                ElseIf TypeOf failedCheck Is RequireGuildOwnerAttribute Then
                    builder.AppendLine("This command can only be used by the server owner.")

                ElseIf TypeOf failedCheck Is RequireStaffAttribute Then
                    builder.AppendLine("This command can only be used by those with a staff title.")

                ElseIf TypeOf failedCheck Is RequireTitleAttribute Then
                    Dim check = DirectCast(failedCheck, RequireTitleAttribute)
                    builder.AppendLine($"You need the title of `{check.MinimumTitle}` or higher to use this command.")
                End If
            Next
        ElseIf arg.Exception.Message.Contains("No matching subcommands were found") Then
            ' Do nothing here.
        Else
            Await logger.PrintAsync(LogLevel.Error, "Command Service", $"'{arg.Command.QualifiedName}' errored in guild {arg.Context.Guild.Id}: '{arg.Exception}'")
            builder.AppendLine($"Something went wrong while running `{arg.Command.QualifiedName}`")
            builder.AppendLine($"```{arg.Exception}```")
        End If

        If builder.Length = 0 Then Return

        If channelPerms.HasPermission(Permissions.EmbedLinks) Or guildPerms.HasPermission(Permissions.Administrator) Then
            Await arg.Context.RespondAsync(embed:=New DiscordEmbedBuilder With {
                .Color = DiscordColor.Red,
                .Title = "Unable To Execute Command",
                .Description = builder.ToString
            })
        Else
            Await arg.Context.RespondAsync(builder.ToString)
        End If
    End Function

    Private Async Function CommandExecutedHandler(arg As CommandExecutionEventArgs, logger As DiscordLogService) As Task
        Await logger.PrintAsync(LogLevel.Info, "Command Service", $"{arg.Context.User.Username} ({arg.Context.User.Id}) executed '{arg.Command.QualifiedName}' in {arg.Context.Guild.Name} ({arg.Context.Guild.Id})")
    End Function
End Class