Public Class StartProcessWindow

    Public Property Client As ProcessClient
   
    Private Sub CancelBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CancelBtn.Click
        Me.DialogResult = False
    End Sub

    Private Sub OkBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OkBtn.Click
        If String.IsNullOrWhiteSpace(FilePathBox.Text) Then
            MessageBox.Show("Please specify the name/path of the executable file that you would like to launch", "No File Specified", MessageBoxButton.OK, MessageBoxImage.Warning)
            Exit Sub
        End If
        If AltCredentialsChk.IsChecked AndAlso String.IsNullOrWhiteSpace(UsernameBox.Text) Then
            MessageBox.Show("Please specify a username or disable the option to use alternate credentials", "No Username Specified", MessageBoxButton.OK, MessageBoxImage.Warning)
            Exit Sub
        End If

        StatusPanel.Visibility = Windows.Visibility.Visible
        MainGrid.IsEnabled = False
        OkBtn.IsEnabled = False
        CancelBtn.IsEnabled = False

        Dim ProcArgs As New NewProcessArgs
        ProcArgs.File = FilePathBox.Text
        ProcArgs.Arguments = ArgumentsBox.Text
        ProcArgs.WorkingDirectory = WorkingDirBox.Text
        If AltCredentialsChk.IsChecked Then
            ProcArgs.RunAsUsername = UsernameBox.Text
            ProcArgs.RunAsPassword = PassBox.Password
            ProcArgs.RunAsDomain = DomainBox.Text
        End If

        Dim BgThread As New System.Threading.Thread(AddressOf StartProcess)
        BgThread.IsBackground = True
        BgThread.Name = "START_PROCESS_THREAD"
        BgThread.Start(ProcArgs)
    End Sub

    Private Sub StartProcess(Args As Object)
        Dim ErrorMsg As String = Nothing
        Try
            Client.StartProcess(DirectCast(Args, NewProcessArgs))
        Catch ex As Exception
            ErrorMsg = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf StartProcessFinished), ErrorMsg)
        End Try
    End Sub

    Private Sub StartProcessFinished(ErrorMsg As String)
        If Me.IsLoaded Then
            StatusPanel.Visibility = Windows.Visibility.Collapsed
            If String.IsNullOrEmpty(ErrorMsg) Then
                MessageBox.Show("Process started on remote machine successfully", "Process Started", MessageBoxButton.OK, MessageBoxImage.Information)
                Me.DialogResult = True
            Else
                MessageBox.Show("Error starting process: " & Environment.NewLine & ErrorMsg, "Error Starting Process", MessageBoxButton.OK, MessageBoxImage.Error)
                MainGrid.IsEnabled = True
                OkBtn.IsEnabled = True
                CancelBtn.IsEnabled = True
            End If
        End If
    End Sub

    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        FilePathBox.Focus()
    End Sub
End Class
