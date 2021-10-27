Public Class CmdTabItemHeader

    Public Event TabCloseRequested(TabId As Guid)

    Private _TabVm As CmdLineTabVM = Nothing
    Private _InRenameMode As Boolean = False


    Private Sub CmdTabItemHeader_Loaded(sender As Object, e As System.Windows.RoutedEventArgs) Handles Me.Loaded
        _TabVm = DirectCast(Me.DataContext, CmdLineTabVM)
    End Sub

    Private Sub CloseTabBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            If _TabVm.Data.IsRunning Then
                Dim Response = MessageBox.Show("The command line process (" & _TabVm.Data.File & ") is still running on the remote machine. Would you like to terminate it when closing this tab?", "Process Still Running", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning)
                If Response = MessageBoxResult.Cancel Then
                    Exit Sub
                ElseIf Response = MessageBoxResult.No Then
                    RaiseEvent TabCloseRequested(_TabVm.Id)
                ElseIf Response = MessageBoxResult.Yes Then
                    _TabVm.Data.EndProcess() 'should really run this on a background thread
                    RaiseEvent TabCloseRequested(_TabVm.Id)
                End If
            Else
                RaiseEvent TabCloseRequested(_TabVm.Id)
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub TabHeaderContent_MouseDoubleClick(sender As System.Object, e As System.Windows.Input.MouseButtonEventArgs)
        Try
            e.Handled = True
            If Not _InRenameMode Then
                _InRenameMode = True
                TitleLbl.Visibility = Windows.Visibility.Collapsed
                RenameBox.Text = _TabVm.Title
                RenameBox.Visibility = Windows.Visibility.Visible
                RenameBox.Focus()
                RenameBox.SelectAll()
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    'Private Sub RenameBox_IsVisibleChanged(sender As System.Object, e As System.Windows.DependencyPropertyChangedEventArgs)
    '    'Try
    '    '    If CBool(e.NewValue) Then
    '    '        Dim RenameBox As TextBox = DirectCast(sender, TextBox)
    '    '        RenameBox.Focus()
    '    '        RenameBox.SelectAll()
    '    '    End If
    '    'Catch ex As Exception
    '    '    MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
    '    'End Try
    'End Sub

    Private Sub RenameBox_KeyDown(sender As System.Object, e As System.Windows.Input.KeyEventArgs)
        Try
            If e.Key = Key.Enter Then
                e.Handled = True
                _TabVm.Title = RenameBox.Text
                HideRenameBox()
            ElseIf e.Key = Key.Escape Then
                e.Handled = True
                HideRenameBox()
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub HideRenameBox()
        TitleLbl.Visibility = Windows.Visibility.Visible
        RenameBox.Visibility = Windows.Visibility.Collapsed
        _InRenameMode = False
    End Sub

    Private Sub RenameBox_LostFocus(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            HideRenameBox()
        Catch ex As Exception
            MessageBox.Show("Unexpected error: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

   


End Class
