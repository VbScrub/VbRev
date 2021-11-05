<Serializable()>
Public Class EnumServicesResponseMessage : Inherits NetworkMessage


    Public Property ServiceList As List(Of ServiceItem)

    Public Sub New()
        Me.Type = MessageType.EnumServicesResponse
    End Sub

    Public Sub New(Services As List(Of ServiceItem))
        Me.Type = MessageType.EnumServicesResponse
        Me.ServiceList = Services
    End Sub



End Class
