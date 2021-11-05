Public Class UiHelper


    Public Shared Sub NotInBeta()
        MessageBox.Show("This feature has not been implemented yet." & Environment.NewLine &
                        "Check http://vbscrub.com for updates on the latest version of this application", "Not Yet Implemented", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Public Shared Sub CopyToClipboard(Text As String)
        Try
            If Text Is Nothing Then
                Clipboard.SetText(String.Empty)
            Else
                Clipboard.SetText(Text)
            End If
        Catch ex As Exception
            MessageBox.Show("Error copying to clipboard: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub


End Class
