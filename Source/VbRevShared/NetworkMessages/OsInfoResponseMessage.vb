<Serializable()>
Public Class OsInfoResponseMessage : Inherits NetworkMessage

    Public Property OsName As String = String.Empty
    Public Property OsVersion As String = String.Empty
    Public Property Is64Bit As Boolean
    Public Property MachineNetBiosDomainName As String = String.Empty
    Public Property MachineDnsDomainName As String = String.Empty

    Public Sub New()
        Me.Type = MessageType.OsInfoResponse
    End Sub

End Class
