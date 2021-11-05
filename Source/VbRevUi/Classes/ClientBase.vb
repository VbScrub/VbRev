Public MustInherit Class ClientBase

    Public Property NetClient As NetworkSession

    Protected Function GetServerResponse(ExpectedResponse As NetworkMessage.MessageType) As NetworkMessage
        Dim Response As NetworkMessage = NetClient.ReceiveMessage()
        Select Case Response.Type
            Case NetworkMessage.MessageType.ErrorDetail
                Throw New ApplicationException("Server responded with the following error: " & DirectCast(Response, SingleParamMessage(Of String)).Parameter)
            Case NetworkMessage.MessageType.ErrorUnknown
                Throw New ApplicationException("Server responded with an unknown error")
            Case NetworkMessage.MessageType.UnrecognisedMessageType
                Throw New ApplicationException("Server responded with error: Unrecognised message type")
            Case NetworkMessage.MessageType.Unknown
                Throw New ApplicationException("Unrecognised message type received from server")
        End Select
        If Not Response.Type = ExpectedResponse Then
            Throw New ApplicationException("Unexpected response from server (expecting " & ExpectedResponse.ToString & " but received " & Response.Type.ToString & ")")
        End If
        Return Response
    End Function


End Class

