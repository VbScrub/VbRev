Public Class ServiceClient : Inherits ClientBase

    Public Sub New(Client As NetworkSession)
        Me.NetClient = Client
    End Sub

    Public Function GetServices(FromRegistry As Boolean) As List(Of ServiceItem)
        NetClient.Send(New SingleParamMessage(Of Boolean)(NetworkMessage.MessageType.EnumServicesRequest, FromRegistry))
        Dim Response As EnumServicesResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.EnumServicesResponse), EnumServicesResponseMessage)
        Response.ServiceList.Sort()
        Return Response.ServiceList
    End Function

End Class


