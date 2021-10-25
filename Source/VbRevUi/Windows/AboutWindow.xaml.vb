Public Class AboutWindow

    Private Sub CloseBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CloseBtn.Click
        Me.Close()
    End Sub

    Private Sub WebsiteLnk_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            Process.Start("http://vbscrub.com")
        Catch ex As Exception
            MessageBox.Show("Error launching URL handler. Please manually browse to http://vbscrub.com", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        VersionLbl.Text = My.Application.Info.Version.ToString
        BuildDateLbl.Text = "2nd May 2020"
    End Sub
End Class
