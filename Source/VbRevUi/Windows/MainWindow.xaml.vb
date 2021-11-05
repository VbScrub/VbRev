Imports System.Net.Sockets

Imports System.Collections.ObjectModel
Imports System.Runtime.InteropServices

Public Class MainWindow

    'Must match actual tab indexes
    Public Enum Tabs As Integer
        Files = 0
        Registry = 1
        Processes = 2
        Services = 3
        CmdLine = 5
        PortForwarding = 6
        Ldap = 4
    End Enum

    Private _Listener As TcpListener
    Private _Port As Integer
    Private _RemoteMachineName As String = String.Empty
    Private _RunningAsUser As String = String.Empty
    Private _StoppingListener As Boolean

    Private _VbClient As VbRevClient

    Private _ServerInfo As ServerInfoResponseMessage

    Private Sub ExitMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub AboutMeniItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim AboutWnd As New AboutWindow
        AboutWnd.Show()
    End Sub

    Private Sub MainWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        Try
            If Not _VbClient Is Nothing Then
                _VbClient.NetClient.RaiseClosedEvent = False
                _VbClient.NetClient.Close()
            End If
        Catch ex As Exception
            MessageBox.Show("Error closing session: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        MainGrid.Visibility = Windows.Visibility.Collapsed
        InitialGrid.Visibility = Windows.Visibility.Visible
        Try
            System.Threading.Thread.CurrentThread.Name = "UI_THREAD"
            HookupPageEventHandlers(New List(Of TabPage) From {FileMainPage, ProcessMainPage, CmdMainPage, NetworkingMainPage, ServicesMainPage})
            Me.TaskbarItemInfo.ProgressValue = 1
            VersionLbl.Text = "V" & My.Application.Info.Version.Major & "." & My.Application.Info.Version.Minor & "." & My.Application.Info.Version.Build
            Try
                UserSettings.LoadSettings()
            Catch ex As Exception
                MessageBox.Show("Error loading user preferences: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Try
            Try
                If String.IsNullOrEmpty(UserSettings.FileDownloadLocation) Then
                    UserSettings.FileDownloadLocation = IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VbRev\Downloads\")
                End If
            Catch ex As Exception
                MessageBox.Show("No file download location has been specified and encountered error setting location to default of %APPDATA%\VbRev\Downloads\ :" & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
            If UserSettings.DefaultListenerPort > 0 AndAlso UserSettings.DefaultListenerPort < 65535 Then
                ListenPortBox.Text = CStr(UserSettings.DefaultListenerPort)
            End If
            If UserSettings.StartListenerOnLaunch Then
                StartListener()
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error in windows loaded event: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub HookupPageEventHandlers(Pages As List(Of TabPage))
        For Each CurrentPage As TabPage In Pages
            AddHandler CurrentPage.SendingServerRequest, AddressOf UiSendingServerRequest
            AddHandler CurrentPage.SendingServerRequestFinished, AddressOf UiSendingServerRequestFinished
        Next
        AddHandler FileMainPage.OpenCmd, AddressOf FileMainPage_OpenCmd
    End Sub

    Private Sub OptionsMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim OptionsWnd As New OptionsWindow
        If OptionsWnd.ShowDialog() Then

        End If
    End Sub

    Private Sub StartListener()
        Try
            Me.TaskbarItemInfo.ProgressState = Shell.TaskbarItemProgressState.None
            If Not Integer.TryParse(ListenPortBox.Text, _Port) Then
                MessageBox.Show("Please enter a valid TCP port number (between 1 and 65535)", "Invalid Port Specified", MessageBoxButton.OK, MessageBoxImage.Warning)
                Exit Sub
            End If
            Try
                _StoppingListener = False
                _Listener = New TcpListener(Net.IPAddress.Any, _Port)
                Log.WriteEntry("Starting listener on port " & _Port, False)
                _Listener.Start()
                Me.Title = "VbRev - Listening On " & _Port
                StatusLbl.Text = "Waiting for connection"
                StartListeningBtn.Visibility = Windows.Visibility.Collapsed
                StopListeningBtn.Visibility = Windows.Visibility.Visible
                StopListeningBtn.IsEnabled = True
                ListenerProgressLbl.Visibility = Windows.Visibility.Visible
                ListenerProgressLbl.Text = "Waiting for incoming connection on port " & _Port & "..."
                PortPanel.Visibility = Windows.Visibility.Collapsed
                _Listener.BeginAcceptTcpClient(New AsyncCallback(AddressOf ClientConnected), Nothing)
            Catch ex As Exception
                MessageBox.Show("Error starting listener: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub StartListeningBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles StartListeningBtn.Click
        StartListener()
    End Sub

    Private Sub ClientConnected(Result As IAsyncResult)
        Try
            If Not _StoppingListener Then
                Log.WriteEntry("Client connected", False)
                _VbClient = New VbRevClient(New NetworkSession(_Listener.EndAcceptTcpClient(Result), True, TimeSpan.FromSeconds(UserSettings.NetworkReadTimeoutSeconds)))
                AddHandler _VbClient.NetClient.Closed, AddressOf VbClient_Closed
                FileMainPage.Client = New FileSystemClient(_VbClient.NetClient, _Port)
                ProcessMainPage.Client = New ProcessClient(_VbClient.NetClient)
                NetworkingMainPage.Client = New NetworkAdapterClient(_VbClient.NetClient)
                ServicesMainPage.Client = New ServiceClient(_VbClient.NetClient)
                CmdMainPage.Client = _VbClient.NetClient
                CmdMainPage.Port = _Port
                Log.WriteEntry("Client = " & _VbClient.NetClient.RemoteIp, False)
                UpdateUiStatus("Received connection from " & _VbClient.NetClient.RemoteIp, False, Shell.TaskbarItemProgressState.Indeterminate)
                UpdateListenerLabel("Received connection from " & _VbClient.NetClient.RemoteIp & Environment.NewLine &
                                    "Waiting for initial data...", True)
                StopListener()
                _ServerInfo = _VbClient.GetInitialInfo
                _RunningAsUser = _ServerInfo.Username
                _RemoteMachineName = _ServerInfo.MachineName
                FileMainPage.MachineName = _RemoteMachineName
                Dim CurrentFilesDirectory As String = _ServerInfo.CurrentDirectory
                If String.IsNullOrWhiteSpace(CurrentFilesDirectory) Then
                    CurrentFilesDirectory = FileSystemClient.DrivesPath
                End If
                UpdateUiStatus("Client connected successfully", False, Shell.TaskbarItemProgressState.Normal)
                ResetTaskbar(5)
                ClientConnectionFinished(CurrentFilesDirectory)
            End If
        Catch DispEx As ObjectDisposedException
            'Ignore ObjectDisposedException as this exception gets thrown on EndAcceptTcpClient when we call TcpListener.Stop
        Catch ex As Exception
            ConnectionError(ex.Message)
        End Try
    End Sub

    Private Sub UpdateListenerLabel(Message As String, ShowProgressBar As Boolean?)
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                ListenerProgressLbl.Text = Message
                If ShowProgressBar.HasValue Then
                    If ShowProgressBar Then
                        InitialProgbar.Visibility = Windows.Visibility.Visible
                    Else
                        InitialProgbar.Visibility = Windows.Visibility.Collapsed
                    End If
                End If
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of String, Boolean?)(AddressOf UpdateListenerLabel), Message, ShowProgressBar)
        End If
    End Sub

    Private Sub ClientConnectionFinished(CurrentDirectory As String)
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                Dim IpAndMachine As String = _VbClient.NetClient.RemoteIp & If(_RemoteMachineName = String.Empty, String.Empty, " (" & _RemoteMachineName & ")")
                Me.Title = "VbRev - " & IpAndMachine
                Me.Width = 890
                Me.Height = 650
                InitialGrid.Visibility = Windows.Visibility.Collapsed
                MainGrid.Visibility = Windows.Visibility.Visible
                RenameWindowMenuItem.Visibility = Visibility.Visible
                ConnectedToLbl.Text = IpAndMachine
                RunningAsLbl.Text = _RunningAsUser
                FileMainPage.InitialLoad(CurrentDirectory)
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf ClientConnectionFinished), CurrentDirectory)
        End If
    End Sub

    Private Sub StopListeningBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        ListenerProgressLbl.Text = "Stopping listener..."
        StopListeningBtn.IsEnabled = False
        ResetInitialPanel()
        StopListener()
        StatusLbl.Text = "Ready"
    End Sub

    Private Sub ResetInitialPanel()
        Try
            Me.Title = "VbRev"
            StartListeningBtn.Visibility = Windows.Visibility.Visible
            StopListeningBtn.Visibility = Windows.Visibility.Collapsed
            ListenerProgressLbl.Visibility = Windows.Visibility.Collapsed
            InitialProgbar.Visibility = Windows.Visibility.Collapsed
            PortPanel.Visibility = Windows.Visibility.Visible
        Catch ex As Exception
            MessageBox.Show("Error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub StopListener()
        Try
            _StoppingListener = True
            If Not _Listener Is Nothing Then
                _Listener.Stop()
            End If
        Catch ex As Exception
            MessageBox.Show("Error stopping TCP listener: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub ResetTaskbar(DelaySeconds As Integer)
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                Dim TaskbarTimer As New Windows.Threading.DispatcherTimer()
                TaskbarTimer.Interval = TimeSpan.FromSeconds(DelaySeconds)
                AddHandler TaskbarTimer.Tick, AddressOf TaskbarTimer_Tick
                TaskbarTimer.Start()
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of Integer)(AddressOf ResetTaskbar), DelaySeconds)
        End If
    End Sub

    Private Sub ConnectionError(Message As String)
        Try
            If Me.Dispatcher.CheckAccess Then
                UpdateUiStatus("Error establishing connection", False, Shell.TaskbarItemProgressState.Error)
                ResetTaskbar(5)
                MessageBox.Show("Error establishing client connection: " & Environment.NewLine & Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                ResetInitialPanel()
                StopListener()
            Else
                Me.Dispatcher.Invoke(New Action(Of String)(AddressOf ConnectionError), Message)
            End If
        Catch ex As Exception
            MessageBox.Show("Error updating UI with error details: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub UpdateUiStatus(Message As String, ShowProgressBar As Boolean?, TaskbarState As Shell.TaskbarItemProgressState?)
        Try
            If Me.Dispatcher.CheckAccess Then
                If Me.IsLoaded Then
                    If Not String.IsNullOrEmpty(Message) Then
                        Me.StatusLbl.Text = Message
                    End If
                    If TaskbarState.HasValue Then
                        Me.TaskbarItemInfo.ProgressState = TaskbarState.Value
                    End If
                    If ShowProgressBar.HasValue Then
                        If ShowProgressBar Then
                            Progbar.Visibility = Windows.Visibility.Visible
                        Else
                            Progbar.Visibility = Windows.Visibility.Collapsed
                        End If
                    End If
                End If
            Else
                Me.Dispatcher.Invoke(New Action(Of String, Boolean?, Shell.TaskbarItemProgressState?)(AddressOf UpdateUiStatus), Message, ShowProgressBar, TaskbarState)
            End If
        Catch ex As Exception
            MessageBox.Show("Error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub UsageMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        UiHelper.NotInBeta()
    End Sub

    Private Sub TaskbarTimer_Tick(sender As Object, e As EventArgs)
        Me.TaskbarItemInfo.ProgressState = Shell.TaskbarItemProgressState.None
        DirectCast(sender, Windows.Threading.DispatcherTimer).Stop()
    End Sub

    Private Sub CopyToClipboard(Text As String)
        Try
            My.Computer.Clipboard.SetText(Text)
        Catch ex As Exception
            MessageBox.Show("Error copying to clipboard: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub


    Private Sub UiSendingServerRequest(StatusMessage As String)
        MainTabControl.IsEnabled = False
        Progbar.Visibility = Windows.Visibility.Visible
        StatusLbl.Text = StatusMessage
    End Sub

    Private Sub UiSendingServerRequestFinished(StatusMessage As String, SwitchToTabIndex As MainWindow.Tabs?)
        MainTabControl.IsEnabled = True
        Progbar.Visibility = Windows.Visibility.Collapsed
        StatusLbl.Text = StatusMessage
        If SwitchToTabIndex.HasValue Then
            MainTabControl.SelectedIndex = SwitchToTabIndex.Value
        End If
    End Sub

    Private Sub MachineInfoLink_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim InfoWnd As New MachineInfoWindow
        InfoWnd.MachineName = _ServerInfo.MachineName
        InfoWnd.Client = _VbClient
        InfoWnd.ShowDialog()
    End Sub

    Private Sub DisconnectBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub WebsiteMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            Process.Start("http://vbscrub.com")
        Catch ex As Exception
            MessageBox.Show("Error launching URL handler. Please manually browse to http://vbscrub.com", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub


    Private Sub Window_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        If FileExplorerTab.IsSelected Then
            FileMainPage.WindowKeyDownEvent(e)
        End If
    End Sub

    Private Sub VbClient_Closed()
        UpdateUiStatus("Server disconnected", False, Shell.TaskbarItemProgressState.Error)
        ResetTaskbar(6)
        MessageBox.Show("Connection to server has been lost", "Connection Lost", MessageBoxButton.OK, MessageBoxImage.Error)
    End Sub

    Private Sub OpenDownloadsMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            If Not IO.Directory.Exists(UserSettings.FileDownloadLocation) Then
                Try
                    IO.Directory.CreateDirectory(UserSettings.FileDownloadLocation)
                Catch ex As Exception
                    MessageBox.Show("Downloads directory did not exist (" & UserSettings.FileDownloadLocation & ") and it could not be created due to the following error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End Try
            End If
            Process.Start("explorer.exe", UserSettings.FileDownloadLocation)
        Catch ex As Exception
            MessageBox.Show("Error opening directory in Explorer: " & ex.Message, "Error Opening Directory", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub FileMainPage_OpenCmd(DirPath As String)
        CmdMainPage.StartNewCmdline(New NewProcessArgs("cmd.exe", Nothing, DirPath))
    End Sub


    Private Sub ComingSoonLinks_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            Process.Start("http://vbscrub.com")
        Catch ex As Exception
            MessageBox.Show("Error launching URL handler. Please manually browse to http://vbscrub.com", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub RenameWindowMenuItem_Click(sender As Object, e As RoutedEventArgs)
        Dim RenameWnd As New RenameWindow
        RenameWnd.WindowTitle = Me.Title
        If RenameWnd.ShowDialog Then
            Me.Title = RenameWnd.WindowTitle
        End If
    End Sub

    Private Sub UserInfoLink_Click(sender As Object, e As RoutedEventArgs)
        Dim UserInfoWnd As New UserInfoWindow
        UserInfoWnd.Username = _RunningAsUser
        UserInfoWnd.Client = _VbClient
        UserInfoWnd.Show()

    End Sub
End Class
