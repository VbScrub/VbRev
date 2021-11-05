Imports System.Net.Sockets

Public Class NetworkSession


    Public Class ClientDisconnectedException : Inherits ApplicationException
        Public Overrides ReadOnly Property Message As String
            Get
                Return "The connection to the remote machine has been lost"
            End Get
        End Property
    End Class

    Public Const ProtocolVersion As Integer = 7 'TODO: Increase this every time you change something that breaks compatibility between this version and old versions
    Public Const ReceiveBufferSize As Integer = 6144 '6 KB

    Public Event Closed()

    Public Property Client As TcpClient = Nothing
    Public Property Stream As NetworkStream = Nothing
    Public Property RemoteIp As String = String.Empty
    Public Property SendKeepAlive As Boolean = True
    Public Property KeepAliveInterval As TimeSpan = TimeSpan.FromSeconds(5)
    Public Property ReadTimeoutMs As Integer = 0
    Public Property RaiseClosedEvent As Boolean = True
    Public Property NetworkLock As New Object

    Private _KeepAliveTimer As New Timers.Timer(KeepAliveInterval.TotalMilliseconds) With {.AutoReset = False}
    Private _CurrentBuffer() As Byte
    Private _Closing As Boolean = False

    Public Sub New(NetSession As TcpClient, DoKeepAlive As Boolean, NetworkReadTimeout As TimeSpan)
        Me.Client = NetSession
        Me.Stream = NetSession.GetStream
        Me.ReadTimeoutMs = CInt(NetworkReadTimeout.TotalMilliseconds)
        Me.RemoteIp = DirectCast(Me.Client.Client.RemoteEndPoint, Net.IPEndPoint).Address.ToString()
        Me.SendKeepAlive = DoKeepAlive
        If DoKeepAlive Then
            AddHandler _KeepAliveTimer.Elapsed, AddressOf SendKeepAliveTimer_Elapsed
            _KeepAliveTimer.Start()
        End If
    End Sub

    'A way to clear network buffer in case of error. Not actually used at the moment
    Public Sub ClearStoredMessages()
        SyncLock _NetworkLock
            _CurrentBuffer = Nothing
            Me.ClearInboundNetworkBuffer()
        End SyncLock
    End Sub

    Public Function ReceiveMessage() As NetworkMessage
        Try
            Log.WriteEntry("Waiting for network message...", False)
            Dim CurrentMessageLength As Integer = 0
            'Make sure we don't already have a fully completed message in the buffer from previous reads
            If Not _CurrentBuffer Is Nothing AndAlso CanConstructMessage(_CurrentBuffer, CurrentMessageLength) Then
                Log.WriteEntry("Found previous message in queue", True)
                Dim ExistingMessage As NetworkMessage = ConstructMessage(_CurrentBuffer, CurrentMessageLength)
                Log.WriteEntry("Current buffer = " & _CurrentBuffer.Length & " - previous message = " & ExistingMessage.BinaryLength, True)
                If _CurrentBuffer.Length > ExistingMessage.BinaryLength Then
                    _CurrentBuffer = RemoveFromBufferStart(_CurrentBuffer, ExistingMessage.BinaryLength)
                    Log.WriteEntry("New length: " & _CurrentBuffer.Length, True)
                Else
                    Log.WriteEntry("Clearing buffer", True)
                    _CurrentBuffer = Nothing
                End If
                Return ExistingMessage
            End If
            Do
                'Read from network
                Log.WriteEntry("Reading from network stream", True)
                Dim NewBytes() As Byte = Me.ReadFromNetworkStream()
                'No previous message that we're waiting to complete
                If _CurrentBuffer Is Nothing Then
                    Log.WriteEntry("No previous buffer", True)
                    _CurrentBuffer = NewBytes
                Else
                    'We have a partially complete message already, so add the new bytes to that
                    Dim OldLength As Integer = _CurrentBuffer.Length
                    Log.WriteEntry("previous buffer found with length of " & OldLength, True)
                    Dim TempBuffer(OldLength + NewBytes.Length - 1) As Byte
                    Array.Copy(_CurrentBuffer, 0, TempBuffer, 0, _CurrentBuffer.Length)
                    Array.Copy(NewBytes, 0, TempBuffer, OldLength, NewBytes.Length)
                    _CurrentBuffer = TempBuffer
                    Log.WriteEntry("New buffer length " & _CurrentBuffer.Length, True)
                End If

                If Not CanConstructMessage(_CurrentBuffer, CurrentMessageLength) Then
                    Log.WriteEntry("Can't construct full message yet (full message length is " & CurrentMessageLength & ")", True)
                    Continue Do
                End If
                Log.WriteEntry("Can construct full message (full message length is " & CurrentMessageLength & ")", True)

                'Construct message from byte array
                Dim Message As NetworkMessage = ConstructMessage(_CurrentBuffer, CurrentMessageLength)
                'If we read part/all of the next message, remove the current message and store the rest ready for next caller
                If _CurrentBuffer.Length > CurrentMessageLength Then
                    _CurrentBuffer = RemoveFromBufferStart(_CurrentBuffer, CurrentMessageLength)
                Else
                    _CurrentBuffer = Nothing
                End If
                Log.WriteEntry("Received message of type " & Message.Type.ToString, False)
                Return Message
            Loop
        Catch ex As Exception
            Log.WriteEntry(ex.Message, False)
            _CurrentBuffer = Nothing
            Throw
        End Try
    End Function

    Private Function RemoveFromBufferStart(SourceBuffer() As Byte, Count As Integer) As Byte()
        Dim TempBuffer(SourceBuffer.Length - Count - 1) As Byte
        Array.Copy(SourceBuffer, Count, TempBuffer, 0, TempBuffer.Length)
        Return TempBuffer
    End Function

    Private Function CanConstructMessage(Bytes() As Byte, ByRef MessageLength As Integer) As Boolean
        'If we don't have enough bytes to get the length of the message then wait for more data
        If Bytes.Length < NetworkMessage.ProtocolId.Length + 4 Then
            Return False
        End If
        'Check for invalid data
        If Not NetworkMessage.IsValidProtocolId(Bytes) Then
            Throw New ApplicationException("Invalid data received from remote machine (no protocol ID)")
        End If
        'Get the total length of the message by grabbing first 32 bit integer after the protocol ID
        MessageLength = BitConverter.ToInt32(Bytes, NetworkMessage.ProtocolId.Length)
        Log.WriteEntry("Full message length is " & MessageLength & " and so far we have received " & Bytes.Length, True)
        'If the total length of the message is longer than our current data, wait for more data
        If MessageLength > Bytes.Length Then
            Return False
        Else
            Return True
        End If
    End Function

    Private Function ConstructMessage(Bytes() As Byte, Length As Integer) As NetworkMessage
        Log.WriteEntry("Constructing message from " & Length & " bytes", True)
        Dim Message As NetworkMessage = Nothing
        'Check for valid message type
        Dim MsgTypeInt As Integer = BitConverter.ToInt32(Bytes, NetworkMessage.ProtocolId.Length + 4)
        If Not NetworkMessage.MessageTypeToCarrier.ContainsKey(DirectCast(MsgTypeInt, NetworkMessage.MessageType)) Then
            Throw New ApplicationException("Invalid message type received from remote machine")
        End If
        Dim MsgType As NetworkMessage.MessageType = DirectCast(MsgTypeInt, NetworkMessage.MessageType)
        'Get correct network message carrier class for message type so that we call correct FromBytes override
        'Not super important right now as they all just use JSON serialization, but will be important later when individual message formats are optimised
        Select Case NetworkMessage.MessageTypeToCarrier(MsgType)
            Case NetworkMessage.MessageCarrierType.Void
                Message = New EmptyMessage()
            Case NetworkMessage.MessageCarrierType.ServerInfoResponse
                Message = New ServerInfoResponseMessage
            Case NetworkMessage.MessageCarrierType.SingleParamString
                Message = New SingleParamMessage(Of String)
            Case NetworkMessage.MessageCarrierType.SingleParamInt
                Message = New SingleParamMessage(Of Integer)
            Case NetworkMessage.MessageCarrierType.SingleParamBool
                Message = New SingleParamMessage(Of Boolean)
            Case NetworkMessage.MessageCarrierType.EnumDirectoryResponse
                Message = New EnumDirectoryResponseMessage()
            Case NetworkMessage.MessageCarrierType.RenameFileRequest
                Message = New RenameFileRequestMessage
            Case NetworkMessage.MessageCarrierType.DeleteFilesRequest
                Message = New DeleteFilesRequestMessage
            Case NetworkMessage.MessageCarrierType.DeleteFilesResponse
                Message = New DeleteFilesResponseMessage
            Case NetworkMessage.MessageCarrierType.TransferFileContent
                Message = New FileTransferContentMessage
            Case NetworkMessage.MessageCarrierType.SingleParamInt
                Message = New SingleParamMessage(Of Integer)
            Case NetworkMessage.MessageCarrierType.StartProcessRequest
                Message = New StartProcessRequestMessage
            Case NetworkMessage.MessageCarrierType.EnumProcessesResponse
                Message = New EnumProcessesResponseMessage
            Case NetworkMessage.MessageCarrierType.EnumNetworkInterfacesResponse
                Message = New EnumNetworkInterfacesResponseMessage
            Case NetworkMessage.MessageCarrierType.EnumTcpListenersResponse
                Message = New EnumTcpListenersResponseMessage
            Case NetworkMessage.MessageCarrierType.OsInfoResponse
                Message = New OsInfoResponseMessage
            Case NetworkMessage.MessageCarrierType.EnumServicesResponse
                Message = New EnumServicesResponseMessage
            Case NetworkMessage.MessageCarrierType.UserInfoResponse
                Message = New UserInfoResponseMessage
            Case Else
                Throw New ApplicationException("Unrecognised network message type " & MsgType.ToString)
        End Select
        'Deserialize bytes to object with FromBytes method
        Message = Message.FromBytes(Bytes, Length)
        Message.Type = MsgType
        Message.BinaryLength = Length
        Log.WriteEntry("Constructed " & Message.Type.ToString & " with length " & Message.BinaryLength.ToString, True)
        Return Message
    End Function

    Public Sub Send(Message As NetworkMessage)
        _KeepAliveTimer.Stop()
        SyncLock _NetworkLock
            WriteToNetworkStream(Message.GetBytes)
        End SyncLock
        If SendKeepAlive Then
            _KeepAliveTimer.Interval = Me.KeepAliveInterval.TotalMilliseconds
            _KeepAliveTimer.Start()
        End If
    End Sub

    Private Sub WriteToNetworkStream(Data() As Byte)
        Try
            If Not Client Is Nothing AndAlso Client.Connected AndAlso Not Stream Is Nothing Then
                Log.WriteEntry("Writing " & Data.Length & " bytes to network stream", True)
                Me.Stream.Write(Data, 0, Data.Length)
                Log.WriteEntry("Successfully wrote to stream", True)
            End If
        Catch IoEx As IO.IOException
            Log.WriteEntry("Error writing to network stream: " & IoEx.Message, False)
            If Not IoEx.InnerException Is Nothing AndAlso (IoEx.InnerException.GetType Is GetType(Net.Sockets.SocketException)) AndAlso
                                                 DirectCast(IoEx.InnerException, Net.Sockets.SocketException).NativeErrorCode = SocketError.ConnectionReset Then
                Throw New ClientDisconnectedException
            End If

        Catch ex As Exception
            Throw New ApplicationException("Error sending network data: " & ex.Message)
        End Try
    End Sub


    Private Sub SendKeepAliveTimer_Elapsed(sender As Object, e As Timers.ElapsedEventArgs)
        'TODO: Uncomment this when we find a good way to stop this from interrupting long running network requests that are waiting for specific responses from server.
        '      Could use a separate TCP connection but then that doesn't prove the main connection is still alive. Probably good enough though as it would notify
        '      user when machine loses connection entirely or shuts down etc

        'Try
        '    If SendKeepAlive Then
        '        SyncLock _NetworkLock
        '            Send(New EmptyMessage(NetworkMessage.MessageType.KeepAlive))
        '            Dim Response As NetworkMessage = ReceiveMessage()
        '            If Response.Type = NetworkMessage.MessageType.KeepAliveResponse Then
        '                _KeepAliveTimer.Start()
        '            Else
        '                Me.Close()
        '            End If
        '        End SyncLock
        '    End If
        'Catch ex As Exception
        '    Log.WriteEntry("Error getting keep alive response: " & ex.Message, False)
        '    Me.Close()
        'End Try
    End Sub

    Private Sub ClearInboundNetworkBuffer()
        Try
            Log.WriteEntry("Clearing TCP buffer", True)
            Dim Buffer(ReceiveBufferSize - 1) As Byte
            If Client.Connected Then
                Client.ReceiveTimeout = 500
                Dim ReadCount As Integer = 0
                Do While Stream.DataAvailable
                    ReadCount = Stream.Read(Buffer, 0, Buffer.Length)
                    Log.WriteEntry(ReadCount & " bytes dumped", True)
                    If ReadCount = 0 Then
                        Exit Sub
                    End If
                Loop
                Log.WriteEntry("No more data available", True)
            Else
                Throw New ClientDisconnectedException
            End If
        Catch ex As Exception

        End Try
    End Sub

    Private Function ReadFromNetworkStream() As Byte()
        Try
            Dim DataSize As Integer = 0
            Dim Buffer(ReceiveBufferSize - 1) As Byte
            If Client.Connected Then
                Client.ReceiveTimeout = ReadTimeoutMs
                DataSize = Stream.Read(Buffer, 0, Buffer.Length)
                Log.WriteEntry("Read " & DataSize & " from TCP session", True)
                Array.Resize(Buffer, DataSize)
                If DataSize = 0 Then
                    Throw New ClientDisconnectedException
                Else
                    Return Buffer
                End If
            Else
                Throw New ClientDisconnectedException
            End If
        Catch IoEx As IO.IOException When Not IoEx.InnerException Is Nothing AndAlso (IoEx.InnerException.GetType Is GetType(Net.Sockets.SocketException)) AndAlso
                                             DirectCast(IoEx.InnerException, Net.Sockets.SocketException).NativeErrorCode = SocketError.ConnectionReset
            Throw New ClientDisconnectedException
        End Try
    End Function


    Public Sub Close()
        If Not _Closing Then
            _Closing = True
            Me.SendKeepAlive = False
            _KeepAliveTimer.Stop()
            _CurrentBuffer = Nothing
            Try
                If Not Client Is Nothing Then
                    Client.Close()
                End If
            Catch ex As Exception
                Log.WriteEntry("Error closing client: " & ex.Message, False)
            End Try
            Try
                If Not Stream Is Nothing Then
                    Stream.Close()
                    Stream.Dispose()
                End If
            Catch ex As Exception
                Log.WriteEntry("Error closing stream: " & ex.Message, False)
            End Try
            If Me.RaiseClosedEvent Then
                RaiseEvent Closed()
            End If
        End If
    End Sub





End Class
