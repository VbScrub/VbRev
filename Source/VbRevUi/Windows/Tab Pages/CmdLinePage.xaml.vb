Imports System.Net.Sockets

Public Class CmdLinePage : Inherits TabPage


    Public Property Client As NetworkSession
    Public Property Port As Integer

    Private _Listener As Net.Sockets.TcpListener
    Private _ThreadSignal As New System.Threading.AutoResetEvent(False)

    'Private _CmdLineTabs As New System.Collections.ObjectModel.ObservableCollection(Of CmdLineTabVM)

    'Private _CurrentTabId As Integer = 0

    Private Sub UserControl_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded

        'MainTabControl.ItemsSource = _CmdLineTabs
    End Sub


    Private Sub StartNewBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles StartNewBtn.Click
        Dim Args As New NewProcessArgs
        If CmdTypeBox.SelectedIndex = 1 Then
            Args.File = "powershell.exe"
        Else
            Args.File = "cmd.exe"
        End If
        StartNewCmdline(Args)
    End Sub

    Public Sub StartNewCmdline(Args As NewProcessArgs)
        RaiseSendingServerRequestEvent("Requesting new command line process...")
        Dim BgThread As New System.Threading.Thread(AddressOf ServerStartNewProcess)
        BgThread.Name = "NEW_CMD_THREAD"
        BgThread.IsBackground = True
        BgThread.Start(Args)
    End Sub

    Private Sub ServerStartNewProcess(Args As Object)
        Dim ProcArgs As NewProcessArgs = DirectCast(Args, NewProcessArgs)
        Try
            If _Listener Is Nothing Then
                _Listener = New TcpListener(Net.IPAddress.Any, Me.Port)
            End If
            _Listener.Start()
            Client.Send(New StartProcessRequestMessage(NetworkMessage.MessageType.StartCmdLineRequest, ProcArgs))
            _Listener.BeginAcceptTcpClient(AddressOf Listener_ConnectionAccepted, ProcArgs)
            'Timeout in case server never connect back to us
            Dim TimedOut As Boolean = Not _ThreadSignal.WaitOne(TimeSpan.FromSeconds(UserSettings.NetworkReadTimeoutSeconds))
            If TimedOut Then
                Throw New ApplicationException("No connection received from remote machine on port " & Port & " within the specified timeout limit")
            End If
        Catch ex As Exception
            _ThreadSignal.Set()
            _Listener.Stop()
            ProcessStartFinished(ex.Message)
        End Try
    End Sub

    Private Sub Listener_ConnectionAccepted(Result As IAsyncResult)
        Dim ErrorMsg As String = Nothing
        Try
            _ThreadSignal.Set()
            Dim NetSession As New NetworkSession(_Listener.EndAcceptTcpClient(Result), False, TimeSpan.Zero)
            _Listener.Stop()
            Dim ProcArgs As NewProcessArgs = DirectCast(Result.AsyncState, NewProcessArgs)
            Dim CmdSession As New CmdLineClient(NetSession, ProcArgs.File, ProcArgs.Arguments, ProcArgs.WorkingDirectory)
            Dim UiTabItem As New CmdLineTabVM(CmdSession, IO.Path.GetFileName(ProcArgs.File))
            Me.Dispatcher.Invoke(New Action(Of CmdLineTabVM)(AddressOf AddTab), UiTabItem)
        Catch ex As Exception
            _Listener.Stop()
            ErrorMsg = ex.Message
        Finally
            ProcessStartFinished(ErrorMsg)
        End Try
    End Sub

    Private Sub AddTab(Vm As CmdLineTabVM)
        Dim NewTab As New TabItem With {.DataContext = Vm}
        Dim Header As New CmdTabItemHeader
        AddHandler Header.TabCloseRequested, AddressOf TabHeader_TabCloseRequested
        NewTab.Header = Header
        NewTab.Content = New CmdLineTabItem
        'Can't use data binding here because then TabControl reuses the same controls for every tab and only changes their datacontext.
        'We could work around that by data binding absolutely every property but would be awkward for some things and probably not a great user experience (i.e. not remembering scroll bar positions etc)
        MainTabControl.Items.Add(NewTab)
        MainTabControl.SelectedIndex = MainTabControl.Items.Count - 1
    End Sub

    Private Sub ProcessStartFinished(ErrorMsg As String)
        If Me.Dispatcher.CheckAccess Then
            Try
                If String.IsNullOrEmpty(ErrorMsg) Then
                    RaiseSendingServerRequestFinishedEvent("New command line process started", MainWindow.Tabs.CmdLine)
                Else
                    RaiseSendingServerRequestFinishedEvent("Error starting command line process")
                    MessageBox.Show("Error setting up new command line session: " & ErrorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            Catch ex As Exception
                MessageBox.Show("Unexpected error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End Try
        Else
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf ProcessStartFinished), ErrorMsg)
        End If
    End Sub

    Private Sub TabHeader_TabCloseRequested(TabId As Guid)
        Try
            For i As Integer = MainTabControl.Items.Count - 1 To 0 Step -1
                If DirectCast(DirectCast(MainTabControl.Items(i), TabItem).DataContext, CmdLineTabVM).Id = TabId Then
                    MainTabControl.Items.RemoveAt(i)
                    Exit Sub
                End If
            Next
        Catch ex As Exception
            MessageBox.Show("Error removing tab: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub


End Class
