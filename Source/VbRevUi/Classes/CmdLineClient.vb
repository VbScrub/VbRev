Public Class CmdLineClient : Inherits ClientBase


    Public Event DataReceived(Data As String)
    Public Event Closed(Status As String)

    Public Property File As String
    Public Property Arguments As String
    Public Property WorkingDirectory As String
    Public Property IsRunning As Boolean = True
    Private _CloseRequested As Boolean
    Private _ClosedEventRaised As Boolean
    Private _CloseLock As New Object

    Public Sub New(Client As NetworkSession, FileName As String, FileArguments As String, WorkingDir As String)
        Me.NetClient = Client
        Me.File = FileName
        Me.Arguments = FileArguments
        Me.WorkingDirectory = WorkingDir
    End Sub

    Public Sub ReceiveData()
        Try
            Do
                Dim Response As NetworkMessage = NetClient.ReceiveMessage()
                If Response.Type = NetworkMessage.MessageType.CmdLineOutputResponse Then
                    RaiseEvent DataReceived(DirectCast(Response, SingleParamMessage(Of String)).Parameter)
                ElseIf Response.Type = NetworkMessage.MessageType.CmdLineClosedNotification Then
                    _CloseRequested = True
                    ProcessOrConnectionClosed("Remote process has terminated")
                ElseIf Response.Type = NetworkMessage.MessageType.ErrorDetail Then
                    Throw New ApplicationException(DirectCast(Response, SingleParamMessage(Of String)).Parameter)
                Else
                    Throw New ApplicationException("Unexpected message type received from server (received " & Response.Type.ToString & ")")
                End If
            Loop Until _CloseRequested
        Catch ex As Exception
            ProcessOrConnectionClosed("Error receiving command line output from server: " & ex.Message)
        Finally
            NetClient.Close()
        End Try
    End Sub

    Public Sub SendInput(Command As String)
        If Not _CloseRequested Then
            Try
                NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.CmdLineInputRequest, Command))
            Catch ex As Exception
                _CloseRequested = True
                ProcessOrConnectionClosed("Error sending command to server: " & ex.Message)
                NetClient.Close()
            End Try
        End If
    End Sub

    Private Sub ProcessOrConnectionClosed(StatusMessage As String)
        Me.IsRunning = False
        SyncLock _CloseLock
            If Not _ClosedEventRaised Then
                _ClosedEventRaised = True
                RaiseEvent Closed(StatusMessage)
            End If
        End SyncLock
    End Sub

    Public Sub EndProcess()
        _CloseRequested = True
        Try
            NetClient.Send(New SingleParamMessage(Of Integer)(NetworkMessage.MessageType.StopProcessRequest, 0))
        Catch ex As Exception
            ProcessOrConnectionClosed("Error sending command to server: " & ex.Message)
            NetClient.Close()
        End Try
    End Sub


End Class
