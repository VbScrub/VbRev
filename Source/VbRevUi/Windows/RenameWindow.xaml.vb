Public Class RenameWindow

    Public Property WindowTitle As String = String.Empty

    Private Sub CancelBtn_Click(sender As Object, e As RoutedEventArgs) Handles CancelBtn.Click
        Me.DialogResult = False
    End Sub

    Private Sub OKBtn_Click(sender As Object, e As RoutedEventArgs) Handles OKBtn.Click
        If String.IsNullOrEmpty(TitleBox.Text) Then
            MessageBox.Show("Please enter a title for the window", "No Title Entered", MessageBoxButton.OK, MessageBoxImage.Warning)
            Exit Sub
        End If
        Me.WindowTitle = TitleBox.Text
        Me.DialogResult = True
    End Sub

    Private Sub Window_Loaded(sender As Object, e As RoutedEventArgs)
        TitleBox.Text = Me.WindowTitle
    End Sub
End Class
