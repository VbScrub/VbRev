Public Class ProcessClient : Inherits ClientBase

    Public Sub New(Client As NetworkSession)
        Me.NetClient = Client
    End Sub

    Public Sub StartProcess(ProcessArgs As NewProcessArgs)
        NetClient.Send(New StartProcessRequestMessage(NetworkMessage.MessageType.StartProcessRequest, ProcessArgs))
        GetServerResponse(NetworkMessage.MessageType.Success) 'will throw exception if response is not success, so we don't need to check the result
    End Sub

    Public Sub EndProcess(PID As Integer)
        NetClient.Send(New SingleParamMessage(Of Integer)(NetworkMessage.MessageType.StopProcessRequest, PID))
        GetServerResponse(NetworkMessage.MessageType.Success) 'will throw exception if response is not success, so we don't need to check the result
    End Sub

    Public Function GetProcessList() As List(Of ProcessItem)
        NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.EnumProcessesRequest))
        Dim Response As EnumProcessesResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.EnumProcessesResponse), EnumProcessesResponseMessage)
        Return Response.ProcessList
    End Function

End Class
