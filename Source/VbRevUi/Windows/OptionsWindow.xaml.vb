Public Class OptionsWindow

    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        Try
            GetFileIconsChk.IsChecked = UserSettings.GetFileIcons
            StartListenerOnLaunchChk.IsChecked = UserSettings.StartListenerOnLaunch
            ListenPortBox.Text = UserSettings.DefaultListenerPort.ToString
            OpenFilesAfterDownloadChk.IsChecked = UserSettings.OpenFilesAfterDownload
            FileDownloadPathBox.Text = UserSettings.FileDownloadLocation
            NetworkTimeoutBox.Text = UserSettings.NetworkReadTimeoutSeconds.ToString
        Catch ex As Exception
            MessageBox.Show("Error loading settings: " & ex.Message, "Error Loading Settings", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub CancelBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CancelBtn.Click
        Me.DialogResult = False
    End Sub

    Private Sub OKBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles OKBtn.Click
        Try
            Dim PortNumber As Integer = 0
            If String.IsNullOrWhiteSpace(ListenPortBox.Text) OrElse Not Integer.TryParse(ListenPortBox.Text, PortNumber) OrElse PortNumber < 1 OrElse PortNumber > 65535 Then
                MessageBox.Show("Please enter a valid port number between 1 and 65535 for the default listener port", "Invalid Port Specified", MessageBoxButton.OK, MessageBoxImage.Warning)
                Exit Sub
            End If
            If String.IsNullOrWhiteSpace(FileDownloadPathBox.Text) Then
                MessageBox.Show("Please specify a valid directory for files to be downloaded to", "Invalid Download Path", MessageBoxButton.OK, MessageBoxImage.Warning)
                Exit Sub
            End If
            Try
                If Not IO.Directory.Exists(FileDownloadPathBox.Text) Then
                    If MessageBox.Show("The specified file downloads directory does not exist. Would you like to create it now?", "Directory Not Found", MessageBoxButton.YesNo, MessageBoxImage.Information) = MessageBoxResult.Yes Then
                        IO.Directory.CreateDirectory(FileDownloadPathBox.Text)
                    End If
                End If
            Catch ex As Exception
                MessageBox.Show("Error validating/creating file downloads directory: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                Exit Sub
            End Try
           
            Dim NetReadTimeout As Integer = 0
            If String.IsNullOrWhiteSpace(NetworkTimeoutBox.Text) OrElse Not Integer.TryParse(NetworkTimeoutBox.Text, NetReadTimeout) OrElse NetReadTimeout < 1 Then
                MessageBox.Show("Please enter a valid number of seconds for the network timeout setting", "Invalid Timeout Specified", MessageBoxButton.OK, MessageBoxImage.Warning)
                Exit Sub
            End If
            UserSettings.StartListenerOnLaunch = CBool(StartListenerOnLaunchChk.IsChecked)
            UserSettings.GetFileIcons = CBool(GetFileIconsChk.IsChecked)
            UserSettings.DefaultListenerPort = PortNumber
            UserSettings.OpenFilesAfterDownload = CBool(OpenFilesAfterDownloadChk.IsChecked)
            UserSettings.FileDownloadLocation = FileDownloadPathBox.Text
            UserSettings.NetworkReadTimeoutSeconds = NetReadTimeout
            UserSettings.SaveSettings()
            Me.DialogResult = True
        Catch ex As Exception
            MessageBox.Show("Error validating settings and saving to " & UserSettings.ConfigFilePath & " : " & ex.Message, "Error Saving Settings", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub DownloadPathBrowseBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles DownloadPathBrowseBtn.Click
        Try
            Dim FolderWnd As New System.Windows.Forms.FolderBrowserDialog 'thanks for no WPF version of FolderBrowserDialog Microsoft
            FolderWnd.Description = "Select directory for downloads to be saved to"
            If FolderWnd.ShowDialog = Forms.DialogResult.OK Then
                FileDownloadPathBox.Text = FolderWnd.SelectedPath
            End If
        Catch ex As Exception
            MessageBox.Show("Error loading folder browser dialog: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub
End Class
