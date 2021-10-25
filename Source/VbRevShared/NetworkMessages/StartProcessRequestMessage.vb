<Serializable()>
Public Class StartProcessRequestMessage : Inherits NetworkMessage

    Public Property ProcessArgs As NewProcessArgs

    Public Sub New()
        Me.Type = MessageType.StartProcessRequest
    End Sub

    Public Sub New(MsgType As MessageType, ProcInfo As NewProcessArgs)
        Me.Type = MsgType
        Me.ProcessArgs = ProcInfo
    End Sub

End Class
