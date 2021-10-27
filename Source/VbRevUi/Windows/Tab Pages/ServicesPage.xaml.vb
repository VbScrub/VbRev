Public Class ServicesPage

    Public Property Client As ServiceClient


    Private Sub StartMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        StartService()
    End Sub

    Private Sub StopMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        StopService()
    End Sub

    Private Sub ViewAclMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        AppHelper.NotInBeta()
    End Sub

    Private Sub StartBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles StartBtn.Click
        StartService()
    End Sub

    Private Sub StartService()
        AppHelper.NotInBeta()
        'Dim Svc As ServiceItem = GetSelectedService()
        'If Not Svc Is Nothing Then

        'End If
    End Sub

    Private Sub StopService()
        AppHelper.NotInBeta()
        'Dim Svc As ServiceItem = GetSelectedService()
        'If Not Svc Is Nothing Then

        'End If
    End Sub

    Private Sub RefreshBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles RefreshBtn.Click
        RefreshBtn.Content = "Refresh"
        Dim BgThread As New System.Threading.Thread(AddressOf GetServices)
        BgThread.IsBackground = True
        BgThread.Name = "ENUM_SERVICES_THREAD"
        RaiseSendingServerRequestEvent("Requesting service list...")
        BgThread.Start(RegistryRadio.IsChecked)
    End Sub

    Private Sub GetServices(FromReg As Object)
        Dim ErrorMsg As String = String.Empty
        Dim Services As List(Of ServiceItem) = Nothing
        Try
            Services = Client.GetServices(CBool(FromReg))
        Catch ex As Exception
            ErrorMsg = ex.Message
        End Try
        Me.Dispatcher.Invoke(New Action(Of String, List(Of ServiceItem))(AddressOf GetServicesFinished), ErrorMsg, Services)
    End Sub

    Private Sub GetServicesFinished(ErrorMsg As String, Services As List(Of ServiceItem))
        If Me.IsLoaded Then
            Try
                If String.IsNullOrEmpty(ErrorMsg) Then
                    RaiseSendingServerRequestFinishedEvent(Services.Count & " services found")
                    ServicesListView.ItemsSource = Services
                    CollectionViewSource.GetDefaultView(ServicesListView.ItemsSource).Filter = New Predicate(Of Object)(AddressOf ApplyFilter)
                Else
                    RaiseSendingServerRequestFinishedEvent("Error")
                    MessageBox.Show(ErrorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            Catch ex As Exception
                MessageBox.Show("Error updating UI: " & ex.Message, "Unexpected Error", MessageBoxButton.OK, MessageBoxImage.Error)
                RaiseSendingServerRequestFinishedEvent("Error")
            End Try
        End If
    End Sub


    Private Sub CopySvcNameMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim Svc As ServiceItem = GetSelectedService()
        If Not Svc Is Nothing Then
            AppHelper.CopyToClipboard(Svc.Name)
        End If
    End Sub

    Private Sub CopyDisplayNameMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim Svc As ServiceItem = GetSelectedService()
        If Not Svc Is Nothing Then
            AppHelper.CopyToClipboard(Svc.DisplayName)
        End If
    End Sub

   
    Private Sub ExplainRegVsScmLink_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        MessageBox.Show("Reading services from the registry can be done by all users." & Environment.NewLine & Environment.NewLine &
                        "Querying the Service Control Manager is usually more restricted. " & Environment.NewLine & Environment.NewLine &
                        "The disadvantage of using the registry method is that live data about each service's current state is not available. Also if someone manually modified the registry data it could be different " &
                        "to the live config that is being used by the services (until the machine is restarted and then the new registry data would be loaded and used by the SCM)", "Service Collection Methods", MessageBoxButton.OK, MessageBoxImage.Information)
    End Sub

    Private Sub FilterBox_TextChanged(sender As System.Object, e As System.Windows.Controls.TextChangedEventArgs)
        If Not ServicesListView.ItemsSource Is Nothing Then
            CollectionViewSource.GetDefaultView(ServicesListView.ItemsSource).Refresh()
        End If
    End Sub

    Private Function ApplyFilter(FilterObject As Object) As Boolean
        If String.IsNullOrWhiteSpace(FilterBox.Text) Then
            Return True
        End If
        If FilterObject Is Nothing Then
            Return False
        End If
        Dim CurrentItem As ServiceItem = DirectCast(FilterObject, ServiceItem)
        If CurrentItem.Name.IndexOf(FilterBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        If Not String.IsNullOrEmpty(CurrentItem.DisplayName) AndAlso CurrentItem.DisplayName.IndexOf(FilterBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        If Not String.IsNullOrEmpty(CurrentItem.RunningAs) AndAlso CurrentItem.RunningAs.IndexOf(FilterBox.Text, StringComparison.CurrentCultureIgnoreCase) > -1 Then
            Return True
        End If
        Return False
    End Function

    Private Sub ViewAclBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ViewAclBtn.Click
        AppHelper.NotInBeta()
    End Sub

    Private Sub ServicesListView_SelectionChanged(sender As System.Object, e As System.Windows.Controls.SelectionChangedEventArgs) Handles ServicesListView.SelectionChanged
        ServiceButtonsPanel.IsEnabled = Not ServicesListView.SelectedItem Is Nothing
    End Sub

    Private Sub CopyBinPathMenuItem_Click(sender As System.Object, e As System.Windows.RoutedEventArgs)
        Dim Svc As ServiceItem = GetSelectedService()
        If Not Svc Is Nothing Then
            AppHelper.CopyToClipboard(Svc.BinPath)
        End If
    End Sub


    Private Function GetSelectedService() As ServiceItem
        If Not ServicesListView.SelectedItem Is Nothing Then
            Return DirectCast(ServicesListView.SelectedItem, ServiceItem)
        Else
            Return Nothing
        End If
    End Function

   
    Private Sub StopBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles StopBtn.Click
        StopService()
    End Sub
End Class
