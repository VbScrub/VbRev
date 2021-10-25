Public Class ProcessPage : Inherits TabPage

    Public Property Client As ProcessClient

    Private _Loaded As Boolean

    Private Sub NewProcessBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles NewProcessBtn.Click
        Dim ProcWnd As New StartProcessWindow
        ProcWnd.Client = Me.Client
        If ProcWnd.ShowDialog() Then
            ServerEnumProcesses()
        End If
    End Sub

    Private Sub RefreshBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles RefreshBtn.Click
        RefreshBtn.Content = "Refresh"
        ServerEnumProcesses()
    End Sub

    Private Sub ServerEnumProcesses()
        RaiseSendingServerRequestEvent("Getting process list from server")
        Dim BgThread As New System.Threading.Thread(AddressOf GetProcesses)
        BgThread.IsBackground = True
        BgThread.Name = "ENUM_PROCESSES_THREAD"
        BgThread.Start()
    End Sub

    Private Sub GetProcesses()
        Dim ErrorMsg As String = Nothing
        Dim ProcList As List(Of ProcessItem) = Nothing
        Try
            ProcList = Client.GetProcessList
            ProcList.Sort()
        Catch ex As Exception
            ErrorMsg = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String, List(Of ProcessItem))(AddressOf GetProcessesFinished), ErrorMsg, ProcList)
        End Try
    End Sub

    Private Sub GetProcessesFinished(ErrorMsg As String, ProcessList As List(Of ProcessItem))
        Try
            If String.IsNullOrEmpty(ErrorMsg) Then
                RaiseSendingServerRequestFinishedEvent(ProcessList.Count & " processes found")
                ProcessListView.ItemsSource = ProcessList
                CollectionViewSource.GetDefaultView(ProcessListView.ItemsSource).Filter = New Predicate(Of Object)(AddressOf ApplyFilter)
            Else
                RaiseSendingServerRequestFinishedEvent("Error getting processes")
                MessageBox.Show("Error getting process list from server: " & ErrorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub

    Private Sub ProcessListView_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles ProcessListView.SelectionChanged
        EndProcessBtn.IsEnabled = Not ProcessListView.SelectedItem Is Nothing
    End Sub

    Private Sub CopyNameMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        If Not ProcessListView.SelectedItem Is Nothing Then
            Try
                Dim Text As String = DirectCast(ProcessListView.SelectedItem, ProcessItem).FileName
                If Not String.IsNullOrEmpty(Text) Then
                    My.Computer.Clipboard.SetText(Text)
                End If
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Try
        End If
    End Sub

    Private Sub EndProcessMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        ServerEndProcess()
    End Sub

    Private Sub ServerEndProcess()
        If ProcessListView.SelectedItem Is Nothing Then
            MessageBox.Show("Please select a process to terminate", "No Process Selected", MessageBoxButton.OK, MessageBoxImage.Warning)
            Exit Sub
        End If
        Dim BgThread As New System.Threading.Thread(AddressOf SendEndProcessRequest)
        BgThread.Name = "END_PROC_THREAD"
        BgThread.IsBackground = True
        RaiseSendingServerRequestEvent("Requesting process termination...")
        BgThread.Start(DirectCast(ProcessListView.SelectedItem, ProcessItem).PID)
    End Sub

    Private Sub SendEndProcessRequest(PID As Object)
        Dim ErrorMsg As String = Nothing
        Try
            Me.Client.EndProcess(CInt(PID))
        Catch ex As Exception
            ErrorMsg = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String)(AddressOf EndProcessFinished), ErrorMsg)
        End Try
    End Sub

    Private Sub EndProcessFinished(ErrorMessage As String)
        If String.IsNullOrEmpty(ErrorMessage) Then
            RaiseSendingServerRequestFinishedEvent("Process terminated successfully")
            MessageBox.Show("Process terminated successfully", "Process Terminated", MessageBoxButton.OK, MessageBoxImage.Information)
            ServerEnumProcesses()
        Else
            RaiseSendingServerRequestFinishedEvent("Error")
            MessageBox.Show("Error requesting process termination: " & ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End If
    End Sub

    Private Sub EndProcessBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles EndProcessBtn.Click
        ServerEndProcess()
    End Sub

    Private Sub CopyPathMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        If Not ProcessListView.SelectedItem Is Nothing Then
            Try
                Dim Text As String = DirectCast(ProcessListView.SelectedItem, ProcessItem).FileLocation
                If Not String.IsNullOrEmpty(Text) Then
                    My.Computer.Clipboard.SetText(Text)
                End If
            Catch ex As Exception
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
            End Try
        End If
    End Sub

    Private Sub FilterBox_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs)
        If Not ProcessListView.ItemsSource Is Nothing Then
            CollectionViewSource.GetDefaultView(ProcessListView.ItemsSource).Refresh()
        End If
    End Sub

    Private Function ApplyFilter(FilterObject As Object) As Boolean
        If String.IsNullOrWhiteSpace(FilterBox.Text) Then
            Return True
        End If
        If FilterObject Is Nothing Then
            Return False
        End If
        Dim CurrentItem As ProcessItem = DirectCast(FilterObject, ProcessItem)
        If CurrentItem.FileName.IndexOf(FilterBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        If Not String.IsNullOrEmpty(CurrentItem.FileLocation) AndAlso CurrentItem.FileLocation.IndexOf(FilterBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        If Not String.IsNullOrEmpty(CurrentItem.RunningAsUser) AndAlso CurrentItem.RunningAsUser.IndexOf(FilterBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        Return False
    End Function

End Class
