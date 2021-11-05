<Serializable()>
Public Class EmptyMessage : Inherits NetworkMessage

    Protected Overrides Function GetDataBytes() As Byte()
        Return Nothing
    End Function

    Public Overrides Function FromBytes(Bytes() As Byte, Length As Integer) As NetworkMessage
        Return New EmptyMessage
    End Function

    Public Sub New()
    End Sub

    Public Sub New(MsgType As MessageType)
        Me.Type = MsgType
    End Sub

End Class
