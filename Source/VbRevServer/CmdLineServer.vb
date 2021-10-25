Public Class CmdLineServer

    Public Property NetClient As NetworkSession
    Public Property File As String
    Public Property Arguments As String
    Public Property InitialDirectory As String

    Private _Closed As Boolean
    Private _ClosedNotificationLock As New Object
    Private _Process As New Process

    Public Sub New(Client As NetworkSession, FileName As String, Args As String, WorkingDir As String)
        Me.NetClient = Client
        Me.File = FileName
        Me.Arguments = Args
        Me.InitialDirectory = WorkingDir
    End Sub

    Public Sub StartAsync()
        Dim BgThread As New System.Threading.Thread(AddressOf RunCmdLine)
        BgThread.Name = "CMDLINE_THREAD"
        BgThread.IsBackground = True
        BgThread.Start()
    End Sub

    Private Sub RunCmdLine()
        Try
            Try
                _Process.StartInfo.FileName = Me.File
                _Process.StartInfo.Arguments = Me.Arguments
                If Not String.IsNullOrEmpty(Me.InitialDirectory) Then
                    _Process.StartInfo.WorkingDirectory = Me.InitialDirectory
                End If
                _Process.StartInfo.UseShellExecute = False
                _Process.StartInfo.RedirectStandardInput = True
                _Process.StartInfo.RedirectStandardOutput = True
                _Process.StartInfo.RedirectStandardError = True
                _Process.Start()
                Log.WriteEntry("Process started: " & Me.File, False)
            Catch ex As Exception
                Log.WriteEntry("Error starting process " & Me.File & " : " & ex.Message, False)
                NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, "Error starting process: " & ex.Message))
                Exit Sub
            End Try

            'Start one thread for each output stream (StdOut and StdErr)
            Dim StdOutThread As New System.Threading.Thread(AddressOf ReadStream)
            StdOutThread.Name = "CMD_STDOUT_THREAD"
            StdOutThread.IsBackground = True
            StdOutThread.Start(_Process.StandardOutput)
            Dim StdErrThread As New System.Threading.Thread(AddressOf ReadStream)
            StdErrThread.Name = "CMD_STDERR_THREAD"
            StdErrThread.IsBackground = True
            StdErrThread.Start(_Process.StandardError)

            'Keep checking for input from client and pass it on to process when received
            Do
                Log.WriteEntry("Waiting for input from client...", False)
                Dim Message As NetworkMessage = NetClient.ReceiveMessage()
                Log.WriteEntry("Received " & Message.Type.ToString, False)
                If _Process.HasExited Then
                    SendClientCloseNotification()
                    Exit Sub
                End If
                Select Case Message.Type
                    Case NetworkMessage.MessageType.StopProcessRequest
                        Try
                            _Process.Kill()
                            SendClientCloseNotification()
                        Catch ex As Exception
                            Try
                                NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, "Error terminating process: " & ex.Message))
                            Catch SendEx As Exception
                                Log.WriteEntry("Error notifying client of failure to kill process: " & ex.Message, False)
                            End Try
                        End Try
                        Exit Sub
                    Case NetworkMessage.MessageType.CmdLineInputRequest
                        'Send data received from client to the process we're running via StdIn
                        _Process.StandardInput.WriteLine(DirectCast(Message, SingleParamMessage(Of String)).Parameter)
                    Case Else
                        Throw New ApplicationException("Unexpected message type received from client")
                End Select
            Loop
        Catch ex As Exception
            Log.WriteEntry("Error handling command line session: " & ex.Message, False)
            If Not ex.GetType Is GetType(NetworkSession.ClientDisconnectedException) Then
                NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, ex.Message))
            End If
        Finally
            Log.WriteEntry("Closing command line session", False)
            NetClient.Close()
        End Try
    End Sub

    Private Sub SendClientCloseNotification()
        Try
            SyncLock _ClosedNotificationLock
                'Only send this once
                If Not _Closed Then
                    _Closed = True
                    Log.WriteEntry("Notifying client of process termination", False)
                    NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.CmdLineClosedNotification))
                End If
            End SyncLock
        Catch ex As Exception
            Log.WriteEntry("Error sending process closed notification to client: " & ex.Message, False)
        End Try
    End Sub

    'This gets run from 2 threads at the same time (operating on different streams)
    Private Sub ReadStream(StreamToRead As Object)
        Try
            Do Until _Closed
                Dim CharBuffer(1023) As Char
                Dim ReadCount As Integer = DirectCast(StreamToRead, IO.StreamReader).Read(CharBuffer, 0, CharBuffer.Length)
                If ReadCount = 0 OrElse _Process.HasExited Then
                    SendClientCloseNotification()
                    Exit Sub
                Else
                    Array.Resize(CharBuffer, ReadCount)
                    Log.WriteEntry("Sending CmdLineOutputResponse to client", False)
                    NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.CmdLineOutputResponse, New String(CharBuffer)))
                End If
            Loop
        Catch ex As Exception
            Log.WriteEntry("Error reading from command line process output/error stream and sending to client: " & ex.Message, False)
        End Try
    End Sub

End Class
