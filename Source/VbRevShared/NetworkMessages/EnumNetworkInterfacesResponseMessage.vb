<Serializable()>
Public Class EnumNetworkInterfacesResponseMessage : Inherits NetworkMessage

    Public Property Interfaces As List(Of NetworkInterfaceItem)

    Public Sub New()
        Me.Type = MessageType.EnumNetworkInterfacesResponse
    End Sub

    Public Sub New(NICs As List(Of NetworkInterfaceItem))
        Me.New()
        Me.Interfaces = NICs
    End Sub

End Class
