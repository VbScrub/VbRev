<Serializable()>
Public Class ServiceItem

    <Flags()>
    Public Enum ServiceType As Integer
        FileSystemDriver = 2
        KernelDriver = 1
        Win32OwnProcess = &H10
        Win32SharedProcess = &H20
        UserOwnprocess = &H50
        UserSharedProcess = &H60
        InteractiveProcess = &H100
    End Enum

    Public Property Name As String
    Public Property DisplayName As String
    Public Property Type As ServiceType

End Class
