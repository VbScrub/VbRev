Public Class CmdLineTabItem

    Private _ClientVM As CmdLineTabVM
    Private _PreviousCommands As New List(Of String)
    Private _PreviousCommandIndex As Integer
    Private _Loaded As Boolean

    Public Property UniqueId As Guid = Guid.NewGuid

    Private Sub UserControl_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Try
            If Not _Loaded Then
                _Loaded = True
                _ClientVM = DirectCast(Me.DataContext, CmdLineTabVM)
                AddHandler _ClientVM.DataReceived, AddressOf Client_DataReceived
                AddHandler _ClientVM.Closed, AddressOf Client_Closed
                Dim BgThread As New System.Threading.Thread(AddressOf _ClientVM.Data.ReceiveData)
                BgThread.IsBackground = True
                BgThread.Name = "CMD_RECEIVE_THREAD"
                BgThread.Start()
                If String.Compare(IO.Path.GetFileName(Me._ClientVM.Data.File), "powershell.exe", True) = 0 Then
                    CmdOutputBox.Background = DirectCast(New BrushConverter().ConvertFrom("#FF012456"), Brush)
                    CmdBox.AcceptsReturn = True
                End If
            End If
            CmdBox.Focus()
        Catch ex As Exception
            MessageBox.Show("Unexpected error setting up tab: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ExecuteCmdBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ExecuteCmdBtn.Click
        SubmitCmd()
    End Sub

    Private Sub SubmitCmd()
        If Not _ClientVM.Data.IsRunning Then
            MessageBox.Show("Unable to send input as the remote process is no longer running or we lost connection to it", "Cannot Send Input", MessageBoxButton.OK, MessageBoxImage.Warning)
            Exit Sub
        End If
        Dim CmdToSend As String = String.Empty
        If Not String.IsNullOrWhiteSpace(CmdBox.Text) Then
            CmdToSend = CmdBox.Text
            _PreviousCommands.Add(CmdBox.Text)
            _PreviousCommandIndex = _PreviousCommands.Count
            CmdBox.Clear()
        End If
        UiSendingServerRequest("Sending command to server...")
        Dim BgThread As New System.Threading.Thread(AddressOf ServerSendInput)
        BgThread.IsBackground = False
        BgThread.Name = "CMD_SEND_THREAD"
        BgThread.Start(CmdToSend)

    End Sub

    Private Sub ServerSendInput(Msg As Object)
        Dim ErrorMsg As String = String.Empty
        Try
            _ClientVM.Data.SendInput(CStr(Msg))
        Catch ex As Exception
            ErrorMsg = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf ServerSendInputFinished), ErrorMsg)
        End Try
    End Sub

    Private Sub ServerSendInputFinished(ErrorMsg As String)
        If String.IsNullOrEmpty(ErrorMsg) Then
            UiSendingServerFinished("Running")
        Else
            UiSendingServerFinished(ErrorMsg)
        End If
    End Sub

    Private Sub UiSendingServerRequest(StatusMsg As String)
        StatusLbl.Text = StatusMsg
        ProgBar.Visibility = Windows.Visibility.Visible
        SendCommandPanel.IsEnabled = False
        'EndProcessBtn.IsEnabled = False
    End Sub

    Private Sub UiSendingServerFinished(StatusMsg As String)
        If Not String.IsNullOrEmpty(StatusMsg) Then
            StatusLbl.Text = StatusMsg
        End If
        ProgBar.Visibility = Windows.Visibility.Collapsed
        SendCommandPanel.IsEnabled = True
        If _ClientVM.Data.IsRunning Then
            'EndProcessBtn.IsEnabled = True
        Else
            ExecuteCmdBtn.IsEnabled = False
        End If
    End Sub

    Public Sub RequestProcessClose()
        Try
            _ClientVM.Data.EndProcess()
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf UiSendingServerFinished), "Waiting for server to end process")
        Catch ex As Exception
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf UiSendingServerFinished), ex.Message)
        End Try
    End Sub


    Private Sub CmdPanelKeyDown(KeyPressArgs As KeyEventArgs)
        Select Case KeyPressArgs.Key
            Case Key.Up
                KeyPressArgs.Handled = True
                If _PreviousCommands.Count > 0 Then
                    _PreviousCommandIndex -= 1
                    If _PreviousCommandIndex < 0 Then
                        _PreviousCommandIndex = 0
                    End If
                    CmdBox.Text = _PreviousCommands(_PreviousCommandIndex)
                    CmdBox.CaretIndex = CmdBox.Text.Length
                End If
            Case Key.Down
                KeyPressArgs.Handled = True
                If _PreviousCommands.Count > 0 Then
                    _PreviousCommandIndex += 1
                    If _PreviousCommandIndex > _PreviousCommands.Count - 1 Then
                        _PreviousCommandIndex = _PreviousCommands.Count - 1
                    End If
                    CmdBox.Text = _PreviousCommands(_PreviousCommandIndex)
                    CmdBox.CaretIndex = CmdBox.Text.Length
                End If
            Case Key.Enter
                SubmitCmd()
        End Select
    End Sub

    Private Sub Client_DataReceived(Data As String)
        CmdOutputBox.AppendText(Data)
        CmdOutputBox.ScrollToEnd()
    End Sub

    Private Sub Client_Closed(Status As String)
        StatusLbl.Text = Status
        StatusLbl.Foreground = Brushes.DarkRed
        StatusLbl.FontWeight = FontWeights.Bold
        ExecuteCmdBtn.IsEnabled = False
        CmdOutputBox.Background = DirectCast(New BrushConverter().ConvertFrom("#FF2B2B2B"), Brush)

        'EndProcessBtn.Visibility = Windows.Visibility.Collapsed
    End Sub

    Private Sub CmdBox_PreviewKeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        CmdPanelKeyDown(e)
    End Sub
End Class
