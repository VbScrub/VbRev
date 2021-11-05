Imports System.Net.Sockets
Imports System.Collections.ObjectModel

Public Class FileSystemPage

    Public Event OpenCmd(Path As String)

    Public Property Client As FileSystemClient
    Public Property MachineName As String = String.Empty

    Private _CurrentDirectory As String = FileSystemClient.DrivesPath
    Private _FileBeingRenamed As FileSystemItemVM
    Private _DirectoryRequested As String

    Private _Files As New ObservableCollection(Of FileSystemItemVM)
    Private _FileAddressBarItems As New ObservableCollection(Of DirectoryBreadcrumbItem)



    Private Sub FileSystemPage_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs)
        AddressBarBreadcrumbBox.ItemsSource = _FileAddressBarItems
        FilesBox.ItemsSource = _Files
    End Sub

    Public Sub InitialLoad(Directory As String)
        _CurrentDirectory = Directory
        UpdateFileAddressBar()
        ServerEnumDirectory(Directory)
    End Sub

    Private Sub FilesBox_MouseDoubleClick(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        EnterSelectedFileOrDirectory()
    End Sub

    Private Sub EnterSelectedFileOrDirectory()
        Try
            If Not FilesBox.SelectedItem Is Nothing Then
                Select Case DirectCast(FilesBox.SelectedItem, FileSystemItemVM).Data.Type
                    Case FileSystemItem.FileItemType.File
                        DownloadSelectedFiles()
                    Case FileSystemItem.FileItemType.Drive, FileSystemItem.FileItemType.Directory
                        ServerEnumDirectory(DirectCast(FilesBox.SelectedItem, FileSystemItemVM).Data.FullPath)
                    Case Else
                        MessageBox.Show("Unrecognised file/directory type", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                End Select
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub UpdateFileAddressBar()
        Try
            _FileAddressBarItems.Clear()
            _FileAddressBarItems.Add(New DirectoryBreadcrumbItem("Drives:", FileSystemClient.DrivesPath))
            If Not _CurrentDirectory = FileSystemClient.DrivesPath Then
                Dim Dirs() As String = _CurrentDirectory.TrimEnd(IO.Path.DirectorySeparatorChar).Split(IO.Path.DirectorySeparatorChar)
                Dim CurrentPath As String = String.Empty
                For i As Integer = 0 To Dirs.Length - 1
                    CurrentPath &= Dirs(i) & IO.Path.DirectorySeparatorChar
                    _FileAddressBarItems.Add(New DirectoryBreadcrumbItem(Dirs(i), CurrentPath))
                Next
            End If
        Catch ex As Exception
            MessageBox.Show("Error parsing file path to construct address bar breadcrumb items: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub CopyToClipboard(Text As String)
        Try
            My.Computer.Clipboard.SetText(Text)
        Catch ex As Exception
            MessageBox.Show("Error copying to clipboard: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub BreadcrumbCopyMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        CopyToClipboard(_CurrentDirectory)
    End Sub


    Private Sub FilesCopyNameMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            If Not FilesBox.SelectedItem Is Nothing Then
                CopyToClipboard(DirectCast(FilesBox.SelectedItem, FileSystemItemVM).Name)
            End If
        Catch ex As Exception
            MessageBox.Show("Error getting selected file name: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub FilesCopyPathMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            If Not FilesBox.SelectedItem Is Nothing Then
                CopyToClipboard(DirectCast(FilesBox.SelectedItem, FileSystemItemVM).Data.FullPath)
            End If
        Catch ex As Exception
            MessageBox.Show("Error getting selected file path: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub FilesDownloadMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        DownloadSelectedFiles()
    End Sub

    Private Sub AddressBarBox_MouseLeftButtonUp(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        EditFileAddressBar()
    End Sub

    Private Sub EditFileAddressBar()
        AddressBarBreadcrumbBox.Visibility = Windows.Visibility.Collapsed
        AddressBarEditBox.Visibility = Windows.Visibility.Visible
        If _CurrentDirectory = FileSystemClient.DrivesPath Then
            AddressBarEditBox.Text = String.Empty
        Else
            AddressBarEditBox.Text = _CurrentDirectory
        End If
        AddressBarEditBox.Focus()
        AddressBarEditBox.SelectAll()
    End Sub

    Private Sub FinishFileAddressBarEdit()
        If AddressBarEditBox.Visibility = Windows.Visibility.Visible Then
            AddressBarBreadcrumbBox.Visibility = Windows.Visibility.Visible
            AddressBarEditBox.Visibility = Windows.Visibility.Collapsed
            FilesBox.Focus()
        End If
    End Sub

    Private Sub BreadcrumbItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        ServerEnumDirectory(DirectCast(DirectCast(sender, Button).DataContext, DirectoryBreadcrumbItem).FullPath)
    End Sub

    Private Sub AddressBarEditBox_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        If e.Key = Key.Enter Then
            e.Handled = True
            ServerEnumDirectory(AddressBarEditBox.Text)
        ElseIf e.Key = Key.Escape Then
            e.Handled = True
            FinishFileAddressBarEdit()
        End If
    End Sub

    Private Sub ServerEnumDirectory(Path As String)
        Try
            If Not Path = FileSystemClient.DrivesPath Then
                If Not FileSystemClient.IsValidPath(Path) Then
                    MessageBox.Show("Path contains invalid characters." & Environment.NewLine & "Please enter a valid and fully qualified path (e.g C:\Windows)", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Exit Sub
                End If
                If Not IO.Path.IsPathRooted(Path) Then
                    MessageBox.Show("Please enter a fully qualified path (e.g C:\Windows)", "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Exit Sub
                End If
                If Not Path.EndsWith(IO.Path.DirectorySeparatorChar) Then
                    'Append \ to end of string to avoid "C:" going to working directory of C drive instead of root of C drive
                    Path &= IO.Path.DirectorySeparatorChar
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Error validating path: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            Exit Sub
        End Try
        _DirectoryRequested = Path
        HideNewFolderUi()
        RaiseSendingServerRequestEvent("Requesting file list...")
        Client.EnumDirectoryAsync(Path, AddressOf ServerEnumDirectoryFinished)
    End Sub

    Private Sub ServerEnumDirectoryFinished(ErrorMessage As String, Files As List(Of FileSystemItem))
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                If Not String.IsNullOrEmpty(ErrorMessage) Then
                    RaiseSendingServerRequestFinishedEvent("Error", Nothing)
                    MessageBox.Show(ErrorMessage, "Error Retrieving File List", MessageBoxButton.OK, MessageBoxImage.Error)
                Else
                    Try
                        RaiseSendingServerRequestFinishedEvent(Files.Count & " files found on server", Nothing)
                        _Files.Clear()
                        For i As Integer = 0 To Files.Count - 1
                            _Files.Add(New FileSystemItemVM(Files(i)))
                        Next
                        _CurrentDirectory = _DirectoryRequested
                        UpdateFileAddressBar()
                        FinishFileAddressBarEdit()
                        If _Files.Count > 0 Then
                            FilesBox.ScrollIntoView(_Files(0))
                        End If
                    Catch ex As Exception
                        MessageBox.Show("Error updating UI with file list: " & ex.Message, "Error Updating UI", MessageBoxButton.OK, MessageBoxImage.Error)
                    End Try
                End If
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of String, List(Of FileSystemItem))(AddressOf ServerEnumDirectoryFinished), ErrorMessage, Files)
        End If
    End Sub

    Public Sub WindowKeyDownEvent(Key As KeyEventArgs)
        FileSystemPage_KeyDown(Me, Key)
    End Sub

    Private Sub AddressBarEditBox_LostFocus(sender As System.Object, e As System.Windows.RoutedEventArgs)
        FinishFileAddressBarEdit()
    End Sub

    Private Sub BreadcrumbEditMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        EditFileAddressBar()
    End Sub

    Private Sub FilesBox_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs)
        e.Handled = True
        If FilesBox.SelectedItem Is Nothing Then
            DownloadFileBtn.IsEnabled = False
        ElseIf DirectCast(FilesBox.SelectedItem, FileSystemItemVM).Data.Type = FileSystemItem.FileItemType.Drive Then
            DownloadFileBtn.IsEnabled = False
        Else
            DownloadFileBtn.IsEnabled = True
        End If
        RenameFileBtn.IsEnabled = DownloadFileBtn.IsEnabled
        DeleteFileBtn.IsEnabled = DownloadFileBtn.IsEnabled
        ViewFilePermissionsBtn.IsEnabled = DownloadFileBtn.IsEnabled
    End Sub

    Private Sub FilesBox_MouseDown_1(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        Dim Result As HitTestResult = VisualTreeHelper.HitTest(Me, e.GetPosition(Me))
        If Result.VisualHit.GetType IsNot GetType(ListBoxItem) Then
            HideFileRenameUI(DirectCast(FilesBox.SelectedItem, FileSystemItemVM))
            FilesBox.UnselectAll()
            FinishFileAddressBarEdit()
        End If
    End Sub

    Private Sub FilesRenameMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        ShowRenameFileBox()
    End Sub

    Private Sub FilesDeleteMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        ServerDeleteFiles()
    End Sub

    Private Sub FilesRefreshBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        ServerEnumDirectory(_CurrentDirectory)
    End Sub

    Private Sub FilesBox_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        Select Case e.Key
            Case Key.Back
                e.Handled = True
                MoveUpDirectoryLevel()
            Case Key.F2
                e.Handled = True
                ShowRenameFileBox()
            Case Key.Delete
                e.Handled = True
                ServerDeleteFiles()
            Case Key.Enter
                e.Handled = True
                EnterSelectedFileOrDirectory()
        End Select
    End Sub

    Private Sub MoveUpDirectoryLevel()
        If Not String.IsNullOrEmpty(_CurrentDirectory) AndAlso Not _CurrentDirectory = FileSystemClient.DrivesPath Then
            Try
                Dim NewPath As String = _CurrentDirectory.TrimEnd(IO.Path.DirectorySeparatorChar)
                Dim LastIndex As Integer = NewPath.LastIndexOf(IO.Path.DirectorySeparatorChar)
                If LastIndex > 0 Then
                    NewPath = NewPath.Remove(LastIndex)
                Else
                    NewPath = FileSystemClient.DrivesPath
                End If
                ServerEnumDirectory(NewPath)
            Catch ex As Exception
                MessageBox.Show("Error getting parent directory path: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Try
        End If
    End Sub

    Private Sub SearchFilesBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles SearchFilesBtn.Click
        UiHelper.NotInBeta()
    End Sub

    Private Sub RenameFileBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles RenameFileBtn.Click
        ShowRenameFileBox()
    End Sub

    Private Sub ShowRenameFileBox()
        Try
            If Not FilesBox.SelectedItem Is Nothing Then
                Dim SelectedFile As FileSystemItemVM = DirectCast(FilesBox.SelectedItem, FileSystemItemVM)
                If Not SelectedFile.Data.Type = FileSystemItem.FileItemType.Directory AndAlso Not SelectedFile.Data.Type = FileSystemItem.FileItemType.File Then
                    MessageBox.Show("This type of item cannot be renamed", "File Or Directory Not Selected", MessageBoxButton.OK, MessageBoxImage.Warning)
                    Exit Sub
                End If
                SelectedFile.RenameBoxText = SelectedFile.Name
                SelectedFile.NameLblVisibility = Windows.Visibility.Collapsed
                SelectedFile.RenameBoxVisibility = Windows.Visibility.Visible
            End If
        Catch ex As Exception
            MessageBox.Show("Error renaming file: " & ex.Message)
        End Try
    End Sub

    Private Sub RenameFileBox_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        Try
            Dim SelectedFile As FileSystemItemVM = DirectCast(DirectCast(sender, TextBox).DataContext, FileSystemItemVM)
            Select Case e.Key
                Case Key.Escape
                    HideFileRenameUI(SelectedFile)
                Case Key.F2
                    e.Handled = True
                Case Key.Back
                    e.Handled = True
                Case Key.Enter
                    e.Handled = True
                    _FileBeingRenamed = SelectedFile
                    ServerRenameFile(SelectedFile, SelectedFile.RenameBoxText)
            End Select
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ServerRenameFile(FileToRename As FileSystemItemVM, NewName As String)
        If String.IsNullOrWhiteSpace(NewName) Then
            MessageBox.Show("Please enter a file name", "No Name Entered", MessageBoxButton.OK, MessageBoxImage.Warning)
            Exit Sub
        End If
        If String.Compare(NewName, FileToRename.Name, True) = 0 Then
            HideFileRenameUI(FileToRename)
            Exit Sub
        End If
        For Each InvalidChar In IO.Path.GetInvalidFileNameChars
            If NewName.Contains(InvalidChar) Then
                MessageBox.Show("New file name contains invalid characters", "Invalid Characters Detected", MessageBoxButton.OK, MessageBoxImage.Warning)
                Exit Sub
            End If
        Next
        RaiseSendingServerRequestEvent("Renaming file...")
        Client.RenameFileAsync(FileToRename.Data.FullPath, NewName, FileToRename.Data.Type = FileSystemItem.FileItemType.Directory, AddressOf ServerRenameFileFinished)
    End Sub

    Private Sub ServerRenameFileFinished(ErrorMessage As String)
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                If String.IsNullOrEmpty(ErrorMessage) Then
                    RaiseSendingServerRequestFinishedEvent("Successfully renamed file", Nothing)
                    Try
                        If Not _FileBeingRenamed Is Nothing Then
                            Dim FullPath As String = _FileBeingRenamed.Data.FullPath.TrimEnd(IO.Path.DirectorySeparatorChar)
                            Dim WithoutFileName As String = FullPath.Remove(FullPath.LastIndexOf(IO.Path.DirectorySeparatorChar))
                            _FileBeingRenamed.Data.FullPath = IO.Path.Combine(WithoutFileName, _FileBeingRenamed.RenameBoxText)
                            _FileBeingRenamed.Data.Name = _FileBeingRenamed.RenameBoxText
                            _FileBeingRenamed.OnPropertyChanged("Name")
                            HideFileRenameUI(_FileBeingRenamed)
                        End If
                    Catch ex As Exception
                        MessageBox.Show("Error updating local file representations. Suggest refresh of directory to ensure all local properties are correct. Error details: " & ex.Message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Warning)
                    End Try
                Else
                    RaiseSendingServerRequestFinishedEvent("Error renaming file")
                    MessageBox.Show(ErrorMessage, "Error Renaming File", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf ServerRenameFileFinished), ErrorMessage)
        End If
    End Sub

    Private Sub RenameFileBox_LostFocus(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            Dim SelectedFile As FileSystemItemVM = DirectCast(DirectCast(sender, TextBox).DataContext, FileSystemItemVM)
            HideFileRenameUI(SelectedFile)
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub HideFileRenameUI(File As FileSystemItemVM)
        If Not File Is Nothing Then
            File.RenameBoxVisibility = Windows.Visibility.Collapsed
            File.NameLblVisibility = Windows.Visibility.Visible
        End If
    End Sub

    Private Sub RenameBox_IsVisibleChanged(sender As System.Object, e As System.Windows.DependencyPropertyChangedEventArgs)
        Try
            If CBool(e.NewValue) Then
                Dim RenameBox As TextBox = DirectCast(sender, TextBox)
                RenameBox.Focus()
                Dim SelectedFile As FileSystemItemVM = DirectCast(RenameBox.DataContext, FileSystemItemVM)
                If SelectedFile.Data.Type = FileSystemItem.FileItemType.Directory OrElse Not SelectedFile.Name.Contains(".") Then
                    RenameBox.SelectAll()
                Else
                    RenameBox.Select(0, SelectedFile.Name.LastIndexOf("."c))
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub FilesUpLevelBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        MoveUpDirectoryLevel()
    End Sub


    Private Sub FileSystemPage_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        If FilesBox.IsEnabled Then
            If e.Key = Key.Back Then
                e.Handled = True
                MoveUpDirectoryLevel()
            ElseIf e.Key = Key.F5 Then
                e.Handled = True
                ServerEnumDirectory(_CurrentDirectory)
            End If
        End If
    End Sub

    Private Sub DeleteFileBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles DeleteFileBtn.Click
        ServerDeleteFiles()
    End Sub

    Private Sub ServerDeleteFiles()
        If Not FilesBox.SelectedItems Is Nothing AndAlso FilesBox.SelectedItems.Count > 0 Then
            If MessageBox.Show("Are you sure you want to delete " & FilesBox.SelectedItems.Count & " item" & If(FilesBox.SelectedItems.Count > 1, "s", String.Empty) & " from the remote machine?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning) = MessageBoxResult.Yes Then
                Try
                    Dim FileList As New List(Of FileSystemItem)
                    For Each FileVM As FileSystemItemVM In FilesBox.SelectedItems
                        FileList.Add(FileVM.Data)
                    Next
                    RaiseSendingServerRequestEvent("Deleting selected files...")
                    Client.DeleteFilesAsync(FileList, AddressOf ServerDeleteFilesFinished)
                Catch ex As Exception
                    MessageBox.Show("Error building file list to send to server: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End If
    End Sub

    Private Sub ServerDeleteFilesFinished(ErrorMessage As String, SuccessfullyDeletedFiles As List(Of String), Failed As String)
        If Me.Dispatcher.CheckAccess Then
            If Me.IsLoaded Then
                Try
                    If String.IsNullOrEmpty(ErrorMessage) Then
                        RaiseSendingServerRequestFinishedEvent(SuccessfullyDeletedFiles.Count & " file" & If(SuccessfullyDeletedFiles.Count > 1, "s", String.Empty) & " deleted")
                        For Each DeletedFile As String In SuccessfullyDeletedFiles
                            For i As Integer = _Files.Count - 1 To 0 Step -1
                                If _Files(i).Name = DeletedFile Then
                                    _Files.RemoveAt(i)
                                    Exit For
                                End If
                            Next
                        Next
                        If Not String.IsNullOrEmpty(Failed) Then
                            If SuccessfullyDeletedFiles.Count > 0 Then
                                MessageBox.Show(SuccessfullyDeletedFiles.Count & " files deleted successfully but the following errors were encountered deleting other files: " & Environment.NewLine & Failed, "Error Deleting Files", MessageBoxButton.OK, MessageBoxImage.Error)
                            Else
                                MessageBox.Show("Errors were encountered whilst deleting files: " & Environment.NewLine & Failed, "Error Deleting Files", MessageBoxButton.OK, MessageBoxImage.Error)
                            End If
                        End If
                    Else
                        RaiseSendingServerRequestFinishedEvent("Error deleting files")
                        MessageBox.Show("Error deleting files: " & ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                    End If
                Catch ex As Exception
                    MessageBox.Show("Unexpected error handling server response and updating UI: " & ex.Message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        Else
            Me.Dispatcher.Invoke(New Action(Of String, List(Of String), String)(AddressOf ServerDeleteFilesFinished), ErrorMessage, SuccessfullyDeletedFiles, Failed)
        End If
    End Sub

    Private Sub DownloadFileBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles DownloadFileBtn.Click
        DownloadSelectedFiles()
    End Sub

    Private Sub DownloadSelectedFiles()
        Try
            If Not FilesBox.SelectedItems Is Nothing Then
                Dim DirectoryPath As String = String.Empty
                Try
                    DirectoryPath = IO.Path.Combine(UserSettings.FileDownloadLocation, Client.NetClient.RemoteIp & " (" & MachineName & ")")
                    If Not IO.Directory.Exists(DirectoryPath) Then
                        IO.Directory.CreateDirectory(DirectoryPath)
                    End If
                Catch ex As Exception
                    MessageBox.Show("Error creating directory to download file to (" & DirectoryPath & ") : " & Environment.NewLine & ex.Message, "Error Creating Directory", MessageBoxButton.OK, MessageBoxImage.Error)
                    Exit Sub
                End Try
                Dim DownloadList As New List(Of FileTransferItem)
                For Each SelectedFile As FileSystemItemVM In FilesBox.SelectedItems
                    If Not SelectedFile.Data.Type = FileSystemItem.FileItemType.File Then
                        MessageBox.Show("Directory downloads are not supported... yet." & Environment.NewLine &
                                        "To download the contents of a directory, please open the directory and download all files within", "Directory Download Not Supported", MessageBoxButton.OK, MessageBoxImage.Information)
                        Exit Sub
                    End If
                    DownloadList.Add(New FileTransferItem(SelectedFile.Name, SelectedFile.Data.FullPath, IO.Path.Combine(DirectoryPath, SelectedFile.Name), TimeSpan.FromMilliseconds(400)))
                Next
                If DownloadList.Count > 0 Then
                    Dim TransferWnd As New TransferFilesWindow
                    TransferWnd.FileTransferList = DownloadList
                    TransferWnd.DownloadDirectory = DirectoryPath
                    TransferWnd.SingleFile = (DownloadList.Count = 1)
                    TransferWnd.Port = Client.FileTransferPort
                    TransferWnd.ManagerSession = Client.NetClient
                    If TransferWnd.SingleFile Then
                        TransferWnd.SizeToContent = SizeToContent.Height
                        TransferWnd.Width = 540
                        TransferWnd.ResizeMode = ResizeMode.CanMinimize
                    End If
                    TransferWnd.ShowDialog()
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error setting up file transfer window: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub UploadFileBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles UploadFileBtn.Click
        Try
            Dim OFD As New Microsoft.Win32.OpenFileDialog
            OFD.Multiselect = True
            OFD.Title = "Select Files To Upload"
            If OFD.ShowDialog Then
                UploadFiles(OFD.FileNames)
            End If
        Catch ex As Exception
            MessageBox.Show("Error managing file selection window: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub UploadFiles(Paths As String())
        Try
            Dim UploadList As New List(Of FileTransferItem)
            For Each SelectedFilePath As String In Paths
                UploadList.Add(New FileTransferItem(IO.Path.GetFileName(SelectedFilePath), SelectedFilePath, IO.Path.Combine(_CurrentDirectory, IO.Path.GetFileName(SelectedFilePath)), TimeSpan.FromMilliseconds(400)))
            Next
            Dim TransferWnd As New TransferFilesWindow
            TransferWnd.IsUpload = True
            TransferWnd.FileTransferList = UploadList
            TransferWnd.SingleFile = (UploadList.Count = 1)
            TransferWnd.Port = Client.FileTransferPort
            TransferWnd.ManagerSession = Client.NetClient
            If TransferWnd.SingleFile Then
                TransferWnd.SizeToContent = SizeToContent.Height
                TransferWnd.Width = 500
                TransferWnd.ResizeMode = ResizeMode.CanMinimize
            End If
            TransferWnd.ShowDialog()
            If TransferWnd.DirectoryRefreshRequired Then
                ServerEnumDirectory(_CurrentDirectory)
            End If
        Catch ex As Exception
            MessageBox.Show("Error generating file upload list from selected items: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ViewFilePermissionsBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ViewFilePermissionsBtn.Click
        UiHelper.NotInBeta()
    End Sub

    Private Sub OpenCmdHereMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        If FilesBox.SelectedItem Is Nothing Then
            RaiseEvent OpenCmd(_CurrentDirectory)
        Else
            Try
                Dim SelectedItem As FileSystemItemVM = DirectCast(FilesBox.SelectedItem, FileSystemItemVM)
                If SelectedItem.Data.Type = FileSystemItem.FileItemType.File Then
                    RaiseEvent OpenCmd(_CurrentDirectory)
                ElseIf SelectedItem.Data.Type = FileSystemItem.FileItemType.Directory OrElse SelectedItem.Data.Type = FileSystemItem.FileItemType.Drive Then
                    RaiseEvent OpenCmd(SelectedItem.Data.FullPath)
                End If
            Catch ex As Exception
                MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Try
        End If

    End Sub


    Private Sub FilesBox_Drop(sender As System.Object, e As System.Windows.DragEventArgs)
        Try
            If e.Data.GetDataPresent(DataFormats.FileDrop) Then
                Dim Paths() As String = DirectCast(e.Data.GetData(DataFormats.FileDrop), String())
                UploadFiles(Paths)
            End If
        Catch ex As Exception
            MessageBox.Show("Error getting drag/drop file paths: " & ex.Message, "Drag/Drop Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub NewDirBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles NewDirBtn.Click
        If Not _CurrentDirectory = FileSystemClient.DrivesPath Then
            NewFolderPanel.Visibility = Windows.Visibility.Visible
            NewFolderBox.Focus()
        End If
    End Sub

    Private Sub NewFolderConfirmBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles NewFolderConfirmBtn.Click
        ServerNewDirectory()
    End Sub

    Private Sub ServerNewDirectory()
        If Not _CurrentDirectory = FileSystemClient.DrivesPath Then
            If String.IsNullOrWhiteSpace(NewFolderBox.Text) Then
                MessageBox.Show("Please enter a name for the new folder", "No Name Specified", MessageBoxButton.OK, MessageBoxImage.Warning)
            Else
                Try
                    Dim Path As String = IO.Path.Combine(_CurrentDirectory, NewFolderBox.Text)
                    RaiseSendingServerRequestEvent("Creating new directory...")
                    System.Threading.ThreadPool.QueueUserWorkItem(AddressOf SendNewDirRequest, Path)
                Catch ex As Exception
                    MessageBox.Show("Error parsing new folder path: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End Try
            End If
        End If
    End Sub

    Private Sub SendNewDirRequest(Path As Object)
        Dim ErrorMessage As String = Nothing
        Try
            Me.Client.CreateDirectory(CStr(Path))
        Catch ex As Exception
            ErrorMessage = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf SendNewDirRequestFinished), ErrorMessage)
        End Try
    End Sub

    Private Sub SendNewDirRequestFinished(ErrorMessage As String)
        If String.IsNullOrEmpty(ErrorMessage) Then
            RaiseSendingServerRequestFinishedEvent("Directory created successfully")
            HideNewFolderUi()
            ServerEnumDirectory(_CurrentDirectory)
        Else
            RaiseSendingServerRequestFinishedEvent("Error")
            MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End If
    End Sub

    Private Sub CancelNewFolderBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CancelNewFolderBtn.Click
        HideNewFolderUi()
    End Sub

    Private Sub HideNewFolderUi()
        NewFolderPanel.Visibility = Windows.Visibility.Collapsed
        NewFolderBox.Clear()
    End Sub

    Private Sub NewFolderBox_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        If e.Key = Key.Enter Then
            e.Handled = True
            ServerNewDirectory()
        End If
    End Sub
End Class
