Public Class UserInfoWindow

    Public Property Client As VbRevClient
    Public Property Username As String

    Private _AllowClose As Boolean

    Private Sub UserInfoWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        If Not _AllowClose Then
            e.Cancel = True
            MessageBox.Show("Please wait for the server to respond before closing" & Environment.NewLine & Environment.NewLine & "I know waiting sucks but it makes life easier for me writing the networking code... so at least one of us is happy :)", "Waiting For Server", MessageBoxButton.OK, MessageBoxImage.Warning)
        End If
    End Sub

    Private Sub CloseBtn_Click(sender As Object, e As RoutedEventArgs) Handles CloseBtn.Click
        Me.Close()
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        UsernameLbl.Text = Me.Username
        Dim BgThread As New Threading.Thread(AddressOf ServerGetUserInfo)
        BgThread.IsBackground = True
        BgThread.Name = "USER_INFO_THREAD"
        BgThread.Start()
    End Sub

    Private Sub ServerGetUserInfo()
        Dim ErrorMsg As String = String.Empty
        Dim UserInfo As UserInfoResponseMessage = Nothing
        Try
            UserInfo = Client.GetUserInfo
        Catch ex As Exception
            ErrorMsg = ex.Message
        End Try
        _AllowClose = True
        Me.Dispatcher.Invoke(New Action(Of String, UserInfoResponseMessage)(AddressOf GetUserInfoFinished), ErrorMsg, UserInfo)
    End Sub

    Private Sub GetUserInfoFinished(ErrorMsg As String, UserInfo As UserInfoResponseMessage)
        Try
            If Me.IsLoaded Then
                ProgressPanel.Visibility = Windows.Visibility.Collapsed
                CloseBtn.IsEnabled = True
                If String.IsNullOrEmpty(ErrorMsg) Then
                    CopySidLbl.Visibility = Visibility.Visible
                    SidLbl.Text = UserInfo.Sid
                    SessionIdLbl.Text = UserInfo.SessionId.ToString
                    GroupsListView.ItemsSource = UserInfo.Groups
                Else
                    MessageBox.Show(ErrorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


    Private Sub CopySidLnk_Click(sender As Object, e As RoutedEventArgs)
        Try
            UiHelper.CopyToClipboard(SidLbl.Text)
        Catch ex As Exception
            MessageBox.Show("Error setting clipboard text: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub
End Class
