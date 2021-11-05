Public Class NetworkingPage

    Public Property Client As NetworkAdapterClient



    Private Function ApplyListenersFilter(FilterObject As Object) As Boolean
        If String.IsNullOrWhiteSpace(FilterListenersBox.Text) Then
            Return True
        End If
        If FilterObject Is Nothing Then
            Return False
        End If
        Dim CurrentItem As ListenerItem = DirectCast(FilterObject, ListenerItem)
        If CurrentItem.Port.ToString.IndexOf(FilterListenersBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        If Not String.IsNullOrEmpty(CurrentItem.IpAddress) AndAlso CurrentItem.IpAddress.IndexOf(FilterListenersBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        If Not String.IsNullOrEmpty(CurrentItem.ProcessName) AndAlso CurrentItem.ProcessName.IndexOf(FilterListenersBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        Return False
    End Function

    Private Sub FilterListenersBox_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs)
        If Not ListenersListView.ItemsSource Is Nothing Then
            CollectionViewSource.GetDefaultView(ListenersListView.ItemsSource).Refresh()
        End If
    End Sub

    Private Sub RefreshListenersBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles RefreshListenersBtn.Click
        RefreshListenersBtn.Content = "Refresh"
        Dim BgThread As New System.Threading.Thread(AddressOf ServerEnumTcpListeners)
        BgThread.Name = "ENUM_LISTENERS_THREAD"
        BgThread.IsBackground = True
        RaiseSendingServerRequestEvent("Getting TCP listener list...")
        BgThread.Start()
    End Sub


    Private Sub ServerEnumTcpListeners()
        Dim ErrorMsg As String = Nothing
        Dim Results As List(Of ListenerItem) = Nothing
        Try
            Results = Client.GetTcpListeners
        Catch ex As Exception
            ErrorMsg = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String, List(Of ListenerItem))(AddressOf EnumTcpListenersFinished), ErrorMsg, Results)
        End Try
    End Sub

    Private Sub EnumTcpListenersFinished(ErrorMsg As String, Results As List(Of ListenerItem))
        Try
            If String.IsNullOrEmpty(ErrorMsg) Then
                ListenersListView.ItemsSource = Results
                CollectionViewSource.GetDefaultView(ListenersListView.ItemsSource).Filter = New Predicate(Of Object)(AddressOf ApplyListenersFilter)
                RaiseSendingServerRequestFinishedEvent(Results.Count & " listeners found")
            Else
                RaiseSendingServerRequestFinishedEvent("Error")
                MessageBox.Show(ErrorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            RaiseSendingServerRequestFinishedEvent("Error")
        End Try
    End Sub

    Private Sub RefreshInterfacesBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles RefreshInterfacesBtn.Click
        RefreshInterfacesBtn.Content = "Refresh"
        Dim BgThread As New System.Threading.Thread(AddressOf ServerGetNetworkInterfaces)
        BgThread.Name = "ENUM_NICS_THREAD"
        BgThread.IsBackground = True
        RaiseSendingServerRequestEvent("Getting NIC list...")
        BgThread.Start()
    End Sub

    Private Sub ServerGetNetworkInterfaces()
        Dim ErrorMsg As String = Nothing
        Dim NICs As List(Of NetworkInterfaceItem) = Nothing
        Try
            NICs = Client.GetNICs
        Catch ex As Exception
            ErrorMsg = ex.Message
        Finally
            Me.Dispatcher.Invoke(New Action(Of String, List(Of NetworkInterfaceItem))(AddressOf GetNICsFinished), ErrorMsg, NICs)
        End Try
    End Sub

    Private Sub GetNICsFinished(ErrorMessage As String, NICs As List(Of NetworkInterfaceItem))
        Try
            If String.IsNullOrEmpty(ErrorMessage) Then
                RaiseSendingServerRequestFinishedEvent("Found " & NICs.Count & " NICs")
                InterfacesListbox.ItemsSource = NICs
            Else
                RaiseSendingServerRequestFinishedEvent("Error")
                MessageBox.Show(ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End If
        Catch ex As Exception
            MessageBox.Show("Error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            RaiseSendingServerRequestFinishedEvent("Error")
        End Try
    End Sub

    Private Sub PortTunnelLink_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Try
            Process.Start("https://vbscrub.com/tools/porttunnel-pt-exe/")
        Catch ex As Exception
            MessageBox.Show("Error launching URL handler. Please manually browse to https://vbscrub.com/tools/porttunnel-pt-exe", "Error", MessageBoxButton.OK, MessageBoxImage.Warning)
        End Try
    End Sub

    Private Sub TabPage_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded

    End Sub
End Class
