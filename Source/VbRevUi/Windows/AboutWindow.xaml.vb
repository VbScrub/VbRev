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
        'TODO: Update build date label
        BuildDateLbl.Text = "5th November 2021"
    End Sub

    Private Sub Icons8Lnk_Click(sender As Object, e As RoutedEventArgs)
        Try
            Process.Start("http://icons8.com")
        Catch ex As Exception
            MessageBox.Show("Error launching URL handler. Please manually browse to http://icons8.com", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub IconShockLnk_Click(sender As Object, e As RoutedEventArgs)
        Try
            Process.Start("http://iconshock.com")
        Catch ex As Exception
            MessageBox.Show("Error launching URL handler. Please manually browse to http://iconshock.com", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub
End Class
