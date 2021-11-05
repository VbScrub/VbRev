Imports System.Net.Sockets

Public Class VbRevServer


    Public Event Closed()

    Private _NetClient As NetworkSession


    Private _Running As Boolean
    Private _ServerThread As System.Threading.Thread
    Private _RemoteMachine As String
    Private _Port As Integer

    Public Sub New(NetSession As NetworkSession, RemoteMachine As String, Port As Integer)
        _NetClient = NetSession
        _NetClient.SendKeepAlive = False
        _RemoteMachine = RemoteMachine
        _Port = Port
        _ServerThread = New System.Threading.Thread(AddressOf RunServer)
        _ServerThread.Name = "MAIN_SERVER_THREAD"
        _ServerThread.IsBackground = True
    End Sub

    Public Sub Start()
        If Not _Running Then
            _Running = True
            _ServerThread.Start()
        End If
    End Sub

    Private Sub RunServer()
        Do
            Dim Message As NetworkMessage = Nothing
            Try
                Message = _NetClient.ReceiveMessage()
            Catch DiscEx As NetworkSession.ClientDisconnectedException
                Log.WriteEntry(_NetClient.RemoteIp & " disconnected", False)
                RaiseEvent Closed()
                Exit Sub
            Catch ex As Exception
                Log.WriteEntry("Error reading network stream from " & _NetClient.RemoteIp & " : " & ex.Message, False)
                RaiseEvent Closed()
                Exit Sub
            End Try
            Try
                If Not Message Is Nothing Then
                    Select Case Message.Type
                        Case NetworkMessage.MessageType.KeepAlive
                            _NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.KeepAliveResponse))
                        Case NetworkMessage.MessageType.ServerInfoRequest
                            _NetClient.Send(GetServerInfo())
                        Case NetworkMessage.MessageType.EnumDirectoryRequest
                            Dim Path As String = DirectCast(Message, SingleParamMessage(Of String)).Parameter
                            Dim Files As List(Of FileSystemItem) = FileSystemHelper.EnumDirectory(Path)
                            _NetClient.Send(New EnumDirectoryResponseMessage(Files))
                        Case NetworkMessage.MessageType.RenameFileRequest
                            Dim Request As RenameFileRequestMessage = DirectCast(Message, RenameFileRequestMessage)
                            If Request.IsDirectory Then
                                FileSystemHelper.RenameDirectory(Request.Path, Request.NewName)
                            Else
                                FileSystemHelper.RenameFile(Request.Path, Request.NewName)
                            End If
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.DeleteFilesRequest
                            Dim Response As New DeleteFilesResponseMessage
                            Dim FailedText As String = String.Empty
                            Response.DeletedItemNames = FileSystemHelper.DeleteFiles(DirectCast(Message, DeleteFilesRequestMessage).Files, FailedText)
                            Response.DeletedItemsFailed = FailedText
                            _NetClient.Send(Response)
                        Case NetworkMessage.MessageType.DownloadFileRequest
                            FileTransfer(Message, False)
                        Case NetworkMessage.MessageType.UploadFileRequest
                            FileTransfer(Message, True)
                        Case NetworkMessage.MessageType.StartProcessRequest
                            Dim Request As StartProcessRequestMessage = DirectCast(Message, StartProcessRequestMessage)
                            ProcessHelper.StartProcess(Request.ProcessArgs)
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.StopProcessRequest
                            Dim Request As SingleParamMessage(Of Integer) = DirectCast(Message, SingleParamMessage(Of Integer))
                            ProcessHelper.EndProcess(Request.Parameter)
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.StartCmdLineRequest
                            Dim Request As StartProcessRequestMessage = DirectCast(Message, StartProcessRequestMessage)
                            Try
                                Dim CmdSession As New NetworkSession(New TcpClient(_RemoteMachine, _Port), False, TimeSpan.Zero) With {.RaiseClosedEvent = False}
                                Dim CmdSrv As New CmdLineServer(CmdSession, Request.ProcessArgs.File, Request.ProcessArgs.Arguments, Request.ProcessArgs.WorkingDirectory)
                                CmdSrv.StartAsync()
                            Catch ex As Exception
                                _NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, "Error establishing additional TCP connection for command line: " & ex.Message))
                            End Try
                        Case NetworkMessage.MessageType.EnumProcessesRequest
                            _NetClient.Send(New EnumProcessesResponseMessage(ProcessHelper.GetProcesses))
                        Case NetworkMessage.MessageType.CreateDirectoryRequest
                            FileSystemHelper.CreateDirectory(DirectCast(Message, SingleParamMessage(Of String)).Parameter)
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.EnumNetworkInterfacesRequest
                            _NetClient.Send(New EnumNetworkInterfacesResponseMessage(NetworkingHelper.GetNICs))
                        Case NetworkMessage.MessageType.EnumTcpListenersRequest
                            _NetClient.Send(New EnumTcpListenersResponseMessage(NetworkingHelper.GetTcpListeners))
                        Case NetworkMessage.MessageType.OsInfoRequest
                            _NetClient.Send(OsHelper.GetOsInfo)
                        Case NetworkMessage.MessageType.EnumServicesRequest
                            _NetClient.Send(New EnumServicesResponseMessage(ServiceHelper.GetServices(DirectCast(Message, SingleParamMessage(Of Boolean)).Parameter)))
                        Case NetworkMessage.MessageType.UserInfoRequest
                            _NetClient.Send(New UserInfoResponseMessage(UserHelper.GetGroups, Security.Principal.WindowsIdentity.GetCurrent.User.ToString, UserHelper.GetSessionId))
                        Case NetworkMessage.MessageType.StartServiceRequest
                            ServiceHelper.StartService(DirectCast(Message, SingleParamMessage(Of String)).Parameter)
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.StopServiceRequest
                            ServiceHelper.StopService(DirectCast(Message, SingleParamMessage(Of String)).Parameter)
                            SendSuccessResponse()
                        Case Else
                            _NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.UnrecognisedMessageType))
                    End Select
                End If
            Catch ex As Exception
                Log.WriteEntry(ex.Message, False)
                If Not ex.GetType Is GetType(NetworkSession.ClientDisconnectedException) Then
                    Try
                        _NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, ex.Message))
                        Continue Do
                    Catch SendEx As Exception
                        RaiseEvent Closed()
                        Exit Sub
                    End Try
                End If
            End Try
        Loop Until Not _Running
    End Sub

    Private Sub SendSuccessResponse()
        _NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.Success))
    End Sub

    Private Sub FileTransfer(Message As NetworkMessage, Receiving As Boolean)
        Dim Request As SingleParamMessage(Of String) = DirectCast(Message, SingleParamMessage(Of String))
        Try
            Log.WriteEntry("Initiating file transfer connection to " & _RemoteMachine & " on port " & _Port, False)
            Dim TransferSession As New NetworkSession(New TcpClient(_RemoteMachine, _Port), False, TimeSpan.Zero) With {.RaiseClosedEvent = False}
            Dim TransferHandler As New FileTransferItem(Request.Parameter, Not Receiving)
            If Receiving Then
                TransferHandler.ReceiveFile(TransferSession, True)
            Else
                TransferHandler.SendFile(TransferSession)
            End If
        Catch ex As Exception
            _NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, "Error establishing additional TCP connection for file transfer: " & ex.Message))
        End Try
    End Sub

    Public Sub [Stop]()
        _Running = False
        Try
            _ServerThread.Abort()
        Catch ex As Exception

        End Try
        RaiseEvent Closed()
    End Sub

    Private Function GetServerInfo() As ServerInfoResponseMessage
        Dim Info As New ServerInfoResponseMessage
        Info.ProtocolVersion = NetworkSession.ProtocolVersion
        Info.MachineName = My.Computer.Name
        Try
            Info.Username = System.Security.Principal.WindowsIdentity.GetCurrent(Security.Principal.TokenAccessLevels.Query).Name
        Catch ex As Exception
            Info.Username = If(String.IsNullOrWhiteSpace(Environment.UserDomainName), Environment.UserName, Environment.UserDomainName & "\" & Environment.UserName)
        End Try
        Info.CurrentDirectory = Environment.CurrentDirectory
        Return Info
    End Function





End Class
