<Serializable()>
Public Class NetworkMessage 'Base class inherited by all messages sent over network

    '== Message format =========================================
    '| Vb (2 bytes) | Length (4 bytes) | Type (4 bytes) | Data |
    '===========================================================

    'TODO: When adding new message type, add mapping to dictionary in MessageTypeToCarrier property
    Public Enum MessageType As Integer
        Unknown = 0
        Success = 1
        ErrorUnknown = 2
        ErrorDetail = 3
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
        OsInfoRequest = 224
        OsInfoResponse = 225
        EnumServicesRequest = 226
        EnumServicesResponse = 227
        UserInfoRequest = 228
        UserInfoResponse = 229
        StartServiceRequest = 230
        StopServiceRequest = 231

    End Enum

    'TODO: When adding a new item to this enum add a corresponding Select Case branch to NetworkSession.ConstructMessage method
    Public Enum MessageCarrierType As Integer
        Unknown
        Void
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
        OsInfoResponse
        EnumServicesResponse
        UserInfoResponse
    End Enum


    Private Shared _MessageTypeToCarrier As Dictionary(Of MessageType, MessageCarrierType)
    Public Shared ReadOnly Property MessageTypeToCarrier As Dictionary(Of MessageType, MessageCarrierType)
        Get
            'Map indvidual message types to the formats that carry them over the network, instead of having separate class for every single
            'message type even though several of them transfer the same data (single string etc)
            If _MessageTypeToCarrier Is Nothing Then
                _MessageTypeToCarrier = New Dictionary(Of MessageType, MessageCarrierType)
                _MessageTypeToCarrier.Add(MessageType.Success, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.ErrorUnknown, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.ErrorDetail, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.InvalidData, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.KeepAlive, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.KeepAliveResponse, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.UnrecognisedMessageType, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.StillWorking, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.ServerInfoRequest, MessageCarrierType.Void)
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
                _MessageTypeToCarrier.Add(MessageType.CmdLineClosedNotification, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.EnumProcessesRequest, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.EnumProcessesResponse, MessageCarrierType.EnumProcessesResponse)
                _MessageTypeToCarrier.Add(MessageType.CreateDirectoryRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.EnumNetworkInterfacesRequest, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.EnumNetworkInterfacesResponse, MessageCarrierType.EnumNetworkInterfacesResponse)
                _MessageTypeToCarrier.Add(MessageType.EnumTcpListenersRequest, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.EnumTcpListenersResponse, MessageCarrierType.EnumTcpListenersResponse)
                _MessageTypeToCarrier.Add(MessageType.OsInfoRequest, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.OsInfoResponse, MessageCarrierType.OsInfoResponse)
                _MessageTypeToCarrier.Add(MessageType.EnumServicesRequest, MessageCarrierType.SingleParamBool)
                _MessageTypeToCarrier.Add(MessageType.EnumServicesResponse, MessageCarrierType.EnumServicesResponse)
                _MessageTypeToCarrier.Add(MessageType.UserInfoRequest, MessageCarrierType.Void)
                _MessageTypeToCarrier.Add(MessageType.UserInfoResponse, MessageCarrierType.UserInfoResponse)
                _MessageTypeToCarrier.Add(MessageType.StartServiceRequest, MessageCarrierType.SingleParamString)
                _MessageTypeToCarrier.Add(MessageType.StopServiceRequest, MessageCarrierType.SingleParamString)
            End If
            Return _MessageTypeToCarrier
        End Get
    End Property

    Public Shared Property ProtocolId As Byte() = New Byte(1) {86, 98}

    <NonSerialized(), Newtonsoft.Json.JsonIgnore>
    Private _Type As MessageType
    <Newtonsoft.Json.JsonIgnore>
    Public Property Type As MessageType
        Get
            Return _Type
        End Get
        Set(value As MessageType)
            _Type = value
        End Set
    End Property

    <NonSerialized(), Newtonsoft.Json.JsonIgnore>
    Private _BinaryLength As Integer
    <Newtonsoft.Json.JsonIgnore>
    Public Property BinaryLength As Integer
        Get
            Return _BinaryLength
        End Get
        Set(value As Integer)
            _BinaryLength = value
        End Set
    End Property

    <NonSerialized, Newtonsoft.Json.JsonIgnore>
    Private Shared _JsonBinder As New JsonNoAssemblyBinder

    'Can be implemented by subclasses to get their class specific data (file path arguments etc) if BinaryFormatter is not suitable
    Protected Overridable Function GetDataBytes() As Byte()
        Dim settings As New Newtonsoft.Json.JsonSerializerSettings()
        settings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
        settings.SerializationBinder = _JsonBinder
        Dim Json As String = Newtonsoft.Json.JsonConvert.SerializeObject(Me, settings)
        Log.WriteEntry("Serialized JSON string: " & Json, True)
        Return Text.Encoding.Unicode.GetBytes(Json)
        'Using MemStrm As New IO.MemoryStream
        '    _XmlSerializer.Serialize(MemStrm, Me)
        '    Return MemStrm.ToArray
        'End Using
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


    Public Overridable Function FromBytes(Bytes() As Byte, Length As Integer) As NetworkMessage
        Dim JsonString As String = Text.Encoding.Unicode.GetString(Bytes, ProtocolId.Length + 8, Length - (ProtocolId.Length + 8))
        Dim settings As New Newtonsoft.Json.JsonSerializerSettings()
        settings.SerializationBinder = _JsonBinder
        settings.TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects
        Log.WriteEntry("Deserializing JSON string: " & JsonString, True)
        Return Newtonsoft.Json.JsonConvert.DeserializeObject(Of NetworkMessage)(JsonString, settings)
        'Using MemStrm As New IO.MemoryStream(Bytes, ProtocolId.Length + 8, Bytes.Length - (ProtocolId.Length + 8))
        '    Return DirectCast(_XmlSerializer.Deserialize(MemStrm), NetworkMessage)
        'End Using
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
