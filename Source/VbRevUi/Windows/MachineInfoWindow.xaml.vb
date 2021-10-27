Public Class MachineInfoWindow

    Public Property MachineName As String
    Public Property Client As VbRevClient
    Private _AllowClose As Boolean

    Private Sub MachineInfoWindow_Closing(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles Me.Closing
        If Not _AllowClose Then
            e.Cancel = True
            MessageBox.Show("Please wait for the server to respond before closing" & Environment.NewLine & Environment.NewLine & "I know waiting sucks but it makes life easier for me writing the networking code... so at least one of us is happy :)", "Waiting For Server", MessageBoxButton.OK, MessageBoxImage.Warning)
        End If
    End Sub

    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        NameLbl.Text = Me.MachineName
        Dim BgThread As New System.Threading.Thread(AddressOf ServerGetMachineInfo)
        BgThread.Name = "OS_INFO_THREAD"
        BgThread.IsBackground = True
        BgThread.Start()
    End Sub

    Private Sub CloseBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CloseBtn.Click
        Me.Close()
    End Sub

    Private Sub ServerGetMachineInfo()
        Dim ErrorMsg As String = String.Empty
        Dim OsInfo As OsInfoResponseMessage = Nothing
        Try
            OsInfo = Client.GetOsInfo
        Catch ex As Exception
            ErrorMsg = ex.Message
        End Try
        _AllowClose = True
        Me.Dispatcher.Invoke(New Action(Of String, OsInfoResponseMessage)(AddressOf GetMachineInfoFinished), ErrorMsg, OsInfo)
    End Sub

    Private Sub GetMachineInfoFinished(ErrorMsg As String, OsInfo As OsInfoResponseMessage)
        Try
            If Me.IsLoaded Then
                ProgressPanel.Visibility = Windows.Visibility.Collapsed
                CloseBtn.IsEnabled = True
                If String.IsNullOrEmpty(ErrorMsg) Then
                    If String.IsNullOrEmpty(OsInfo.MachineDnsDomainName) Then
                        DomainLbl.Text = OsInfo.MachineNetBiosDomainName
                    Else
                        DomainLbl.Text = OsInfo.MachineDnsDomainName & "  (NetBIOS: " & OsInfo.MachineNetBiosDomainName & ")"
                    End If
                    Is64BitLbl.Text = If(OsInfo.Is64Bit, "Yes", "No")
                    OsVersionLbl.Text = OsInfo.OsVersion
                    OsNameLbl.Text = OsInfo.OsName
                Else
                    MessageBox.Show(ErrorMsg, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
                End If
            End If
        Catch ex As Exception
            MessageBox.Show("Unexpected error updating UI: " & ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
        End Try
    End Sub


End Class
