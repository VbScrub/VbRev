<Serializable()>
Public Class ServerInfoResponseMessage : Inherits NetworkMessage

    Public Property MachineName As String = String.Empty
    Public Property Username As String = String.Empty
    Public Property ProtocolVersion As Integer
    Public Property CurrentDirectory As String = String.Empty

    Public Sub New()
        Me.Type = MessageType.ServerInfoResponse
    End Sub

End Class
