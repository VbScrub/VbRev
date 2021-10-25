<Serializable()>
Public Class EnumTcpListenersResponseMessage : Inherits NetworkMessage

    Public Property Listeners As List(Of ListenerItem)

    Public Sub New()
        Me.Type = MessageType.EnumTcpListenersResponse
    End Sub

    Public Sub New(TcpListeners As List(Of ListenerItem))
        Me.New()
        Me.Listeners = TcpListeners
    End Sub

End Class
