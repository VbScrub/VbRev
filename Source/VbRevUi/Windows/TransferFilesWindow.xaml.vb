Imports System.Windows.Controls.Primitives
Imports System.Net.Sockets

Public Class TransferFilesWindow


    Public Property ManagerSession As NetworkSession 'For sending initial request to server that will cause server to create new TCP connection to us for file transfer
    Public Property IsUpload As Boolean
    Public Property SingleFile As Boolean
    Public Property FileTransferList As List(Of FileTransferItem)
    Public Property DownloadDirectory As String
    Public Property Port As Integer
    Public Property DirectoryRefreshRequired As Boolean

    Private _Cancelled As Boolean
    Private _FatalErrorEncountered As Boolean
    Private _ItemLock As New Object
    Private _ThreadSignal As New System.Threading.AutoResetEvent(False)
    Private _TransferModeName As String = "Downloads"
    Private Const ProgressColumnWidth As Integer = 160
    Private Listener As TcpListener
    Private _CurrentIndex As Integer = 0
    Private _MaxIndex As Integer = 0
    Private _TransferVmList As New List(Of FileTransferItemVM)
    Private _AllowClose As Boolean = True


    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Try
            'Create UI items for each file transfer instance (file transfer instances raise progress/status events that UI items receive and update UI via INotifyPropertyChanged)
            For i As Integer = 0 To Me.FileTransferList.Count - 1
                Dim Vm As New FileTransferItemVM(FileTransferList(i))
                If i = 0 Then
                    Vm.ProgressDetails = "Starting transfer..."
                End If
                _TransferVmList.Add(Vm)
            Next
            _MaxIndex = _TransferVmList.Count - 1
        Catch ex As Exception
            MessageBox.Show("Unexpected error constructing file list: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Me.Close()
            Exit Sub
        End Try

        'Configure window based on whether we have multiple items to download or just one, and whether we're uploading or downloading files
        If SingleFile Then
            OpenBtn.IsEnabled = False
            SingleTransferPanel.Visibility = Windows.Visibility.Visible
            SingleTransferPanel.DataContext = _TransferVmList(0)
            OpenAutoChk.IsChecked = UserSettings.OpenFilesAfterDownload
            ProgressBox.Visibility = Windows.Visibility.Collapsed
        Else
            OpenAutoChk.Visibility = Windows.Visibility.Collapsed
            ProgressBox.Visibility = Windows.Visibility.Visible
            ProgressBox.ItemsSource = _TransferVmList
            ProgressBox.AddHandler(Thumb.DragDeltaEvent, New DragDeltaEventHandler(AddressOf ColumnResizeHandler), True)
        End If
        If Me.IsUpload Then
            _TransferModeName = "Uploads"
            Me.Title = "Uploading Files..."
            TitleLbl.Text = "Uploading Files"
            DownloadLocationGrid.Visibility = Windows.Visibility.Collapsed
            OpenAutoChk.Visibility = Windows.Visibility.Collapsed
            OpenBtn.Visibility = Windows.Visibility.Collapsed
        Else
            OpenDownloadsDirLink.Inlines.Add(Me.DownloadDirectory)
        End If
        'Create TCP listener that will listen for incoming connections from remote machine
        '(file transfers are done on separate TCP connection so they are easy to cancel by closing connection and don't interfere with other network messages)
        Try
            Listener = New TcpListener(Net.IPAddress.Any, Me.Port)
        Catch ex As Exception
            MessageBox.Show("Error setting up TCP listener on port " & Me.Port & " : " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Me.Close()
            Exit Sub
        End Try
        'Start a new background thread that will run transfers
        BeginTransferAsync()
    End Sub

    'Cancel remaining items if user tries to exit window before we're done
    Private Sub TransferFilesWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        If Not _AllowClose Then
            e.Cancel = True
            If Not _Cancelled Then
                If MessageBox.Show("Cancel file transfers?", "Cancel?", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                    CancelTransfers()
                End If
            End If
        End If
    End Sub

    'Only called once on window load
    Private Sub BeginTransferAsync()
        _AllowClose = False
        Dim BgThread As New System.Threading.Thread(AddressOf BeginTransfer)
        BgThread.IsBackground = True
        BgThread.Name = "FILE_TRANSFER_INIT_THREAD"
        BgThread.Start()
    End Sub

    'This method gets called once for each file transfer, after previous transfer has finished
    '(can't process them all in a normal loop because we want to have a timeout for AcceptTcpClient, so have to use BeginAcceptTcpClient and split things up)
    Private Sub BeginTransfer()
        Try
            If _Cancelled Then
                CancelUiItem(_TransferVmList(_CurrentIndex))
                IncrementCounterAndStartNext()
            Else
                Listener.Start()
                Try
                    'Use _CurrentIndex to get the current item we're processing
                    Dim CurrentItem As FileTransferItem = _TransferVmList(_CurrentIndex).Data
                    'Send initial request to tell server to connect back to us
                    If IsUpload Then
                        ManagerSession.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.UploadFileRequest, CurrentItem.OutputPath))
                    Else
                        ManagerSession.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.DownloadFileRequest, CurrentItem.SourcePath))
                    End If
                Catch ex As Exception
                    Throw New ApplicationException("Error sending file transfer request to server: " & ex.Message)
                End Try
                'Wait for server to connect back to us
                Listener.BeginAcceptTcpClient(AddressOf Listener_ConnectionAccepted, _TransferVmList(_CurrentIndex).Data)
                'Timeout in case server never connect back to us
                Dim TimedOut As Boolean = Not _ThreadSignal.WaitOne(TimeSpan.FromSeconds(UserSettings.NetworkReadTimeoutSeconds))
                If TimedOut Then
                    _Cancelled = True
                    Throw New ApplicationException("No connection received from remote machine on port " & Port & " within the specified timeout limit")
                End If
            End If
        Catch ex As Exception
            'An exception here is usually going to mean network connection is broken, so cancel all remaining items 
            _Cancelled = True
            Listener.Stop()
            _ThreadSignal.Set()
            _TransferVmList(_CurrentIndex).Data.UpdateProgress(FileTransferItem.ProgressState.Failed, 0, ex.Message)
            IncrementCounterAndStartNext()
            FatalError(ex.Message)
        End Try
    End Sub

    Private Sub CancelUiItem(Item As FileTransferItemVM)
        Item.Data.RequestCancellation()
        Item.Data.CurrentState = FileTransferItem.ProgressState.Cancelled
        Item.Data_ProgressUpdated(FileTransferItem.ProgressState.Cancelled, 0, "Transfer cancelled")
    End Sub

    'poor man's For loop
    Private Sub IncrementCounterAndStartNext()
        If _CurrentIndex < _MaxIndex Then
            _CurrentIndex += 1
            BeginTransfer()
        Else
            TransfersFinished()
        End If
    End Sub

    'Called when the server has connected back to us
    Private Sub Listener_ConnectionAccepted(Result As IAsyncResult)
        Dim TransferItem As FileTransferItem = DirectCast(Result.AsyncState, FileTransferItem)
        _ThreadSignal.Set() 'stop timeout error from appearing as we only get here if we received a connection from server
        Try

            Dim Client As New NetworkSession(Listener.EndAcceptTcpClient(Result), False, TimeSpan.FromSeconds(UserSettings.NetworkReadTimeoutSeconds))
            Listener.Stop()
            If _Cancelled Then
                TransferItem.RequestCancellation()
            Else
                If IsUpload Then
                    Threading.Thread.CurrentThread.Name = "CLIENT_UPLOAD_FILE"
                    TransferItem.SendFile(Client)
                Else
                    Threading.Thread.CurrentThread.Name = "CLIENT_DOWNLOAD_FILE"
                    TransferItem.ReceiveFile(Client, True)
                End If
            End If
        Catch ex As Exception
            TransferItem.UpdateProgress(FileTransferItem.ProgressState.Failed, 0, "Error handling TCP connection: " & ex.Message)
        Finally
            IncrementCounterAndStartNext()
        End Try
    End Sub

    Private Sub FatalError(Message As String)
        _FatalErrorEncountered = True
        _AllowClose = True
        Listener.Stop()
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                SetFailedUi()
                MessageBox.Show(Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf FatalError), Message)
        End If
    End Sub

    'Sets "Cancelled" boolean in each transfer item (that they check for cancellation periodically and update UI themselves when cancelled)
    Private Sub CancelTransfers()
        _Cancelled = True
        _AllowClose = True
        For i As Integer = 0 To _TransferVmList.Count - 1
            _TransferVmList(i).Data.RequestCancellation()
        Next
    End Sub

    Private Sub TransfersFinished()
        _AllowClose = True
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                CancelBtn.Visibility = Windows.Visibility.Collapsed
                CloseBtn.Visibility = Windows.Visibility.Visible
                If Not _FatalErrorEncountered Then
                    If SingleFile Then 'If only one file was selected
                        If _Cancelled Then
                            Me.Title = _TransferModeName & " Cancelled"
                        Else
                            Select Case _TransferVmList(0).Data.CurrentState
                                Case FileTransferItem.ProgressState.Complete
                                    If IsUpload Then
                                        Me.DirectoryRefreshRequired = True
                                        Me.Close()
                                    End If
                                    If OpenAutoChk.IsChecked AndAlso Not IsUpload Then
                                        OpenFile(_TransferVmList(0), True)
                                    Else
                                        TitleImg.Source = New BitmapImage(New Uri("\..\Images\Icons8\ok_48px.png", UriKind.Relative))
                                        OpenBtn.IsEnabled = True
                                        Me.Title = _TransferModeName & " Complete"
                                    End If
                                Case FileTransferItem.ProgressState.Failed
                                    SetFailedUi()
                                    MessageBox.Show("File transfer failed due to the following error: " & Environment.NewLine &
                                                _TransferVmList(0).ProgressDetails, "File Transfer Failed", MessageBoxButton.OK, MessageBoxImage.Error)
                            End Select
                        End If
                    Else 'If multiple files were selected
                        If _Cancelled Then
                            Me.Title = _TransferModeName & " Cancelled"
                        Else
                            Me.Title = _TransferModeName & " Finished"
                        End If
                        If IsUpload Then
                            Dim AllUploadsSuccessfull As Boolean = True
                            'If at least one item was uploaded, tell the main window to issue a directory refresh request
                            For Each Transfer As FileTransferItemVM In _TransferVmList
                                If Transfer.Data.CurrentState = FileTransferItem.ProgressState.Complete Then
                                    Me.DirectoryRefreshRequired = True
                                Else
                                    AllUploadsSuccessfull = False
                                End If
                            Next
                            If AllUploadsSuccessfull Then
                                Me.Close()
                            End If
                        End If
                    End If
                    Me.TitleLbl.Text = Me.Title
                End If
            End If
        Else
            Me.Dispatcher.Invoke(New Action(AddressOf TransfersFinished))
        End If
    End Sub

    Private Sub SetFailedUi()
        TitleImg.Source = New BitmapImage(New Uri("\..\Images\Icons8\cancel_48px.png", UriKind.Relative))
        Me.Title = _TransferModeName & " Failed"
        Me.TitleLbl.Text = Me.Title
        Me.CloseBtn.Visibility = Windows.Visibility.Visible
        Me.CancelBtn.Visibility = Windows.Visibility.Collapsed
    End Sub

    Private Sub CancelBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CancelBtn.Click
        Try
            CancelTransfers()
            CancelBtn.IsEnabled = False
            Me.Title = "Cancelling..."
            Me.TitleLbl.Text = "Cancelling..."
        Catch ex As Exception
            MessageBox.Show("Error requesting cancellation: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub OpenDownloadsDirLink_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            Process.Start("explorer.exe", Me.DownloadDirectory)
        Catch ex As Exception
            MessageBox.Show("Error opening downloads directory: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CloseBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Me.Close()
    End Sub

    Private Sub OpenBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OpenBtn.Click
        Try
            If SingleFile Then
                OpenFile(_TransferVmList(0), True)
            Else
                OpenSelectedFile()
            End If
        Catch ex As Exception
            MessageBox.Show("Error launching file: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub OpenFile(File As FileTransferItemVM, CloseAfterOpen As Boolean)
        Try
            Process.Start(File.Data.OutputPath)
            If CloseAfterOpen Then
                Me.Close()
            End If
        Catch ex As Exception
            MessageBox.Show("Error opening file: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub ColumnResizeHandler(Sender As Object, e As DragDeltaEventArgs)
        Try
            Dim SenderThumb As Thumb = DirectCast(e.OriginalSource, Thumb)
            If SenderThumb.TemplatedParent.GetType Is GetType(GridViewColumnHeader) Then
                Dim Header As GridViewColumnHeader = DirectCast(SenderThumb.TemplatedParent, GridViewColumnHeader)
                If Header.Column Is ProgressCol Then
                    ProgressCol.Width = ProgressColumnWidth
                End If
            End If
        Catch ex As Exception
            'Who really cares if this throws an exception. No one. That's who.
        End Try
    End Sub

    Private Sub ProgressBox_MouseDoubleClick(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        OpenSelectedFile()
    End Sub

    Private Sub OpenSelectedFile()
        Try
            If Not ProgressBox.SelectedItem Is Nothing Then
                Dim SelectedFile As FileTransferItemVM = DirectCast(ProgressBox.SelectedItem, FileTransferItemVM)
                If SelectedFile.Data.CurrentState = FileTransferItem.ProgressState.Complete Then
                    OpenFile(SelectedFile, False)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub OpenAutoChk_Checked(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OpenAutoChk.Checked
        SaveAutoOpenChk()
    End Sub

    Private Sub SaveAutoOpenChk()
        Try
            UserSettings.OpenFilesAfterDownload = CBool(OpenAutoChk.IsChecked)
            UserSettings.SaveSettings()
        Catch ex As Exception
            Log.WriteEntry("Error saving user preferences: " & ex.Message, False)
        End Try
    End Sub

    Private Sub OpenAutoChk_Unchecked(sender As System.Object, e As System.Windows.RoutedEventArgs)
        SaveAutoOpenChk()
    End Sub
End Class
