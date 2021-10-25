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
        'Keeps looping and processing incoming messages
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
                    Log.WriteEntry("Server received " & Message.Type.ToString, False)
                    Select Case Message.Type
                        Case NetworkMessage.MessageType.KeepAlive
                            _NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.KeepAliveResponse))
                        Case NetworkMessage.MessageType.ServerInfoRequest
                            _NetClient.Send(GetServerInfo())
                        Case NetworkMessage.MessageType.EnumDirectoryRequest
                            Dim Path As String = DirectCast(Message, SingleParamMessage(Of String)).Parameter
                            Dim Files As List(Of FileSystemItem) = FileSystemServer.EnumDirectory(Path)
                            _NetClient.Send(New EnumDirectoryResponseMessage(Files))
                        Case NetworkMessage.MessageType.RenameFileRequest
                            Dim Request As RenameFileRequestMessage = DirectCast(Message, RenameFileRequestMessage)
                            If Request.IsDirectory Then
                                FileSystemServer.RenameDirectory(Request.Path, Request.NewName)
                            Else
                                FileSystemServer.RenameFile(Request.Path, Request.NewName)
                            End If
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.DeleteFilesRequest
                            Dim Response As New DeleteFilesResponseMessage
                            Dim FailedText As String = String.Empty
                            Response.DeletedItemNames = FileSystemServer.DeleteFiles(DirectCast(Message, DeleteFilesRequestMessage).Files, FailedText)
                            Response.DeletedItemsFailed = FailedText
                            _NetClient.Send(Response)
                        Case NetworkMessage.MessageType.DownloadFileRequest
                            FileTransfer(Message, False)
                        Case NetworkMessage.MessageType.UploadFileRequest
                            FileTransfer(Message, True)
                        Case NetworkMessage.MessageType.StartProcessRequest
                            Dim Request As StartProcessRequestMessage = DirectCast(Message, StartProcessRequestMessage)
                            ProcessServer.StartProcess(Request.ProcessArgs)
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.StopProcessRequest
                            Dim Request As SingleParamMessage(Of Integer) = DirectCast(Message, SingleParamMessage(Of Integer))
                            ProcessServer.EndProcess(Request.Parameter)
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
                            _NetClient.Send(New EnumProcessesResponseMessage(ProcessServer.GetProcesses))
                        Case NetworkMessage.MessageType.CreateDirectoryRequest
                            FileSystemServer.CreateDirectory(DirectCast(Message, SingleParamMessage(Of String)).Parameter)
                            SendSuccessResponse()
                        Case NetworkMessage.MessageType.EnumNetworkInterfacesRequest
                            _NetClient.Send(New EnumNetworkInterfacesResponseMessage(NetworkingServer.GetNICs))
                        Case NetworkMessage.MessageType.EnumTcpListenersRequest
                            _NetClient.Send(New EnumTcpListenersResponseMessage(NetworkingServer.GetTcpListeners))
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

    Private Sub FileTransfer(Message As NetworkMessage, ClientSendingFile As Boolean)
        Dim Request As SingleParamMessage(Of String) = DirectCast(Message, SingleParamMessage(Of String))
        Try
            Log.WriteEntry("Initiating file transfer connection to " & _RemoteMachine & " on port " & _Port, False)
            Dim TransferSession As New NetworkSession(New TcpClient(_RemoteMachine, _Port), False, TimeSpan.Zero) With {.RaiseClosedEvent = False}
            Dim TransferHandler As New FileTransferItem(Request.Parameter, Not ClientSendingFile)
            If ClientSendingFile Then
                TransferHandler.ReceiveFile(TransferSession, False)
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
            Info.Username = CStr(IIf(String.IsNullOrWhiteSpace(Environment.UserDomainName), Environment.UserName, Environment.UserDomainName & "\" & Environment.UserName))
        End Try
        Info.Is64Bit = Environment.Is64BitOperatingSystem
        Info.CurrentDirectory = Environment.CurrentDirectory
        Try
            Info.MachineDomainName = OsServer.GetComputerDomainName
        Catch ex As Exception
            Info.MachineDomainName = "Error: " & ex.Message
        End Try
        Try
            Dim OsInfo = GetOsInfo()
            Info.OsName = OsInfo.Item1
            Info.OsVersion = OsInfo.Item2
        Catch ex As Exception
            Info.OsName = "Error: " & ex.Message
        End Try
        Return Info
    End Function

    Private Function GetOsInfo() As Tuple(Of String, String)
        Dim Name As String = String.Empty
        Dim VersionNumber As String = String.Empty
        Using HKLM = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Default)
            Using SubKey = HKLM.OpenSubKey("Software\Microsoft\Windows NT\CurrentVersion")
                Name = CStr(SubKey.GetValue("ProductName", String.Empty))
                Dim ReleaseId As String = CStr(SubKey.GetValue("ReleaseId", String.Empty))
                If ReleaseId = String.Empty Then
                    VersionNumber = CStr(SubKey.GetValue("CurrentVersion", String.Empty))
                Else
                    VersionNumber = ReleaseId
                End If
                Return New Tuple(Of String, String)(Name, VersionNumber)
            End Using
        End Using

    End Function

End Class
