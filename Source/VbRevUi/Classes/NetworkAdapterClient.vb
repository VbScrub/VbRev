Public Class NetworkAdapterClient : Inherits ClientBase

    Public Sub New(Client As NetworkSession)
        Me.NetClient = Client
    End Sub

    Public Function GetTcpListeners() As List(Of ListenerItem)
        NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.EnumTcpListenersRequest))
        Dim Response As EnumTcpListenersResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.EnumTcpListenersResponse), EnumTcpListenersResponseMessage)
        Return Response.Listeners
    End Function

    Public Function GetNICs() As List(Of NetworkInterfaceItem)
        NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.EnumNetworkInterfacesRequest))
        Dim Response As EnumNetworkInterfacesResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.EnumNetworkInterfacesResponse), EnumNetworkInterfacesResponseMessage)
        Return Response.Interfaces
    End Function

End Class
