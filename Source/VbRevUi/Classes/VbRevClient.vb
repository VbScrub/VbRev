﻿Public Class VbRevClient : Inherits ClientBase


    Public Sub New(Client As NetworkSession)
        Me.NetClient = Client
    End Sub


    Public Function GetInitialInfo() As ServerInfoResponseMessage
        Try
            NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.ServerInfoRequest))
            Dim Response As ServerInfoResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.ServerInfoResponse), ServerInfoResponseMessage)
            If Response.ProtocolVersion <> NetworkSession.ProtocolVersion Then
                Throw New ApplicationException("Version mismatch. Please ensure you are running the same version of the VbRev server and client")
            End If
            Return Response
        Catch ex As Exception
            Try
                NetClient.Close()
            Catch CloseEx As Exception
                Log.WriteEntry("Error closing network session: " & ex.Message, False)
            End Try
            Throw
        End Try
    End Function

    







End Class