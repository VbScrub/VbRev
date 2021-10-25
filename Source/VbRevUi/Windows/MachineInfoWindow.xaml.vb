Public Class MachineInfoWindow

    Public Property ServerInfo As ServerInfoResponseMessage


    Private Sub Window_Loaded(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MyBase.Loaded
        NameLbl.Text = ServerInfo.MachineName
        DomainLbl.Text = ServerInfo.MachineDomainName
        Is64BitLbl.Text = CStr(IIf(ServerInfo.Is64Bit, "Yes", "No"))
        OsVersionLbl.Text = ServerInfo.OsVersion
        OsNameLbl.Text = ServerInfo.OsName
    End Sub

    Private Sub CloseBtn_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles CloseBtn.Click
        Me.Close()
    End Sub
End Class
