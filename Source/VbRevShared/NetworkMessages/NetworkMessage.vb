<Serializable()>
Public Class NetworkMessage 'Base class inherited by all messages sent over network

    '== Message format =========================================
    '| Vb (2 bytes) | Length (4 bytes) | Type (4 bytes) | Data |
    '===========================================================

    'NOTE: When adding new message type, add mapping to MessageTypeToCarrier property
    Public Enum MessageType As Integer
        Unknown = 0
        Success = 1
        ErrorUnknown = 2
        ErrorDetail = 3
        Any = 4
        KeepAlive = 100
        KeepAliveResponse = 101
        StillWorking = 102
        InvalidData = 103
        UnrecognisedMessageType = 104
        ServerInfoRequest = 201
        ServerInfoResponse = 202
        EnumDirectoryRequest = 203
        EnumDirectoryResponse = 204
        RenameFileRequest = 205
        DeleteFilesRequest = 206
        DeleteFilesResponse = 207
        DownloadFileRequest = 208
        TransferFileContent = 209
        UploadFileRequest = 210
        StartProcessRequest = 211
        StopProcessRequest = 212
        CmdLineInputRequest = 213
        CmdLineOutputResponse = 214
        StartCmdLineRequest = 215
        CmdLineClosedNotification = 216
        EnumProcessesRequest = 217
        EnumProcessesResponse = 218
        CreateDirectoryRequest = 219
        EnumNetworkInterfacesRequest = 220
        EnumNetworkInterfacesResponse = 221
        EnumTcpListenersRequest = 222
        EnumTcpListenersResponse = 223
    End Enum

    'NOTE: When adding a new item to this enum, add a corresponding Select Case branch to NetworkSession.ConstructMessage method
    Public Enum MessageCarrierType As Integer
        Unknown
        Empty
        SingleParamString
        SingleParamBool
        SingleParamInt
        ServerInfoResponse
        EnumDirectoryResponse
        RenameFileRequest
        DeleteFilesRequest
        DeleteFilesResponse
        TransferFileContent
        StartProcessRequest
        EnumProcessesResponse
        EnumNetworkInterfacesResponse
        EnumTcpListenersResponse
    End Enum


    Private Shared _MessageTypeToCarrier As Dictionary(Of MessageType, MessageCarrierType)
    Public Shared ReadOnly Property MessageTypeToCarrier As Dictionary(Of MessageType, MessageCarrierType)
        Get
            'Map indvidual message types to the formats that carry them over the network, instead of having separate class for every single
            'message type even though several of them transfer the same data (single string etc)
            If _MessageTypeToCarrier Is Nothing Then
                _MessageTypeToCarrier = New Dictionary(Of MessageType, MessageCarrierType)
                _MessageTypeToCarrier.Add(MessageType.Success, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.ErrorUnknown, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.ErrorDetail, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.InvalidData, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.KeepAlive, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.KeepAliveResponse, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.UnrecognisedMessageType, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.StillWorking, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.ServerInfoRequest, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.ServerInfoResponse, MessageCarrierType.ServerInfoResponse)
                _MessageTypeToCarrier.Add(MessageType.EnumDirectoryRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.EnumDirectoryResponse, MessageCarrierType.EnumDirectoryResponse)
                _MessageTypeToCarrier.Add(MessageType.RenameFileRequest, MessageCarrierType.RenameFileRequest)
                _MessageTypeToCarrier.Add(MessageType.DeleteFilesRequest, MessageCarrierType.DeleteFilesRequest)
                _MessageTypeToCarrier.Add(MessageType.DeleteFilesResponse, MessageCarrierType.DeleteFilesResponse)
                _MessageTypeToCarrier.Add(MessageType.DownloadFileRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.TransferFileContent, MessageCarrierType.TransferFileContent)
                _MessageTypeToCarrier.Add(MessageType.UploadFileRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.StartProcessRequest, MessageCarrierType.StartProcessRequest)
                _MessageTypeToCarrier.Add(MessageType.StopProcessRequest, MessageCarrierType.SingleParamInt)
                _MessageTypeToCarrier.Add(MessageType.CmdLineOutputResponse, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.CmdLineInputRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.StartCmdLineRequest, MessageCarrierType.StartProcessRequest)
                _MessageTypeToCarrier.Add(MessageType.CmdLineClosedNotification, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.EnumProcessesRequest, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.EnumProcessesResponse, MessageCarrierType.EnumProcessesResponse)
                _MessageTypeToCarrier.Add(MessageType.CreateDirectoryRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.EnumNetworkInterfacesRequest, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.EnumNetworkInterfacesResponse, MessageCarrierType.EnumNetworkInterfacesResponse)
                _MessageTypeToCarrier.Add(MessageType.EnumTcpListenersRequest, MessageCarrierType.Empty)
                _MessageTypeToCarrier.Add(MessageType.EnumTcpListenersResponse, MessageCarrierType.EnumTcpListenersResponse)
            End If
            Return _MessageTypeToCarrier
        End Get
    End Property

    Public Shared Property ProtocolId As Byte() = New Byte(1) {86, 98}

    <NonSerialized()> _
    Private _Type As MessageType
    Public Property Type As MessageType
        Get
            Return _Type
        End Get
        Set(value As MessageType)
            _Type = value
        End Set
    End Property

    <NonSerialized()>
    Private _BinaryLength As Integer
    Public Property BinaryLength As Integer
        Get
            Return _BinaryLength
        End Get
        Set(value As Integer)
            _BinaryLength = value
        End Set
    End Property

    <NonSerialized()> _
    Private _Formatter As New Runtime.Serialization.Formatters.Binary.BinaryFormatter

    'Can be implemented by subclasses to get their class specific data (file path arguments etc) if BinaryFormatter is not suitable
    Protected Overridable Function GetDataBytes() As Byte()
        Using MemStrm As New IO.MemoryStream
            _Formatter.Serialize(MemStrm, Me)
            Return MemStrm.ToArray
        End Using
    End Function

    Public Function GetBytes() As Byte()
        Try
            Log.WriteEntry("Serializing " & Me.Type.ToString, True)
            Dim DataBytes() As Byte = Me.GetDataBytes()
            Dim DataByteLength As Integer = 0
            'If message only has a type and no additional data (EmptyMessage) then DataBytes is null
            If Not DataBytes Is Nothing Then
                DataByteLength = DataBytes.Length
            End If
            Dim LengthBytes(3) As Byte '32 bit Int
            Dim TypeBytes(3) As Byte '32 bit Int
            'Make final byte array correct length to hold protocol ID, length, type, and optionally data
            Dim FinalBytes(ProtocolId.Length + LengthBytes.Length + TypeBytes.Length + DataByteLength - 1) As Byte
            LengthBytes = BitConverter.GetBytes(FinalBytes.Length)
            TypeBytes = BitConverter.GetBytes(Me.Type)
            'Copy everything into the final byte array
            Array.Copy(ProtocolId, 0, FinalBytes, 0, ProtocolId.Length)
            Array.Copy(LengthBytes, 0, FinalBytes, ProtocolId.Length, LengthBytes.Length)
            Array.Copy(TypeBytes, 0, FinalBytes, ProtocolId.Length + LengthBytes.Length, TypeBytes.Length)
            If Not DataBytes Is Nothing Then
                Array.Copy(DataBytes, 0, FinalBytes, ProtocolId.Length + LengthBytes.Length + TypeBytes.Length, DataByteLength)
            End If
            Return FinalBytes
        Catch ex As Exception
            Throw New ApplicationException("Error converting network message to byte array: " & ex.Message)
        End Try
    End Function

    'If child class does not override this, we just use the built in binary serialization
    Public Overridable Function FromBytes(Bytes() As Byte) As NetworkMessage
        Using MemStrm As New IO.MemoryStream(Bytes, ProtocolId.Length + 8, Bytes.Length - (ProtocolId.Length + 8))
            Return DirectCast(_Formatter.Deserialize(MemStrm), NetworkMessage)
        End Using
    End Function

    Public Shared Function IsValidProtocolId(Bytes() As Byte) As Boolean
        If Bytes Is Nothing OrElse Bytes.Length < ProtocolId.Length Then
            Return False
        End If
        For i As Integer = 0 To ProtocolId.Length - 1
            If Not Bytes(i) = ProtocolId(i) Then
                Return False
            End If
        Next
        Return True
    End Function



End Class
