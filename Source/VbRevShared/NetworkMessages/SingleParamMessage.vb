<Serializable()>
Public Class SingleParamMessage(Of T) : Inherits NetworkMessage

    Public Property Parameter As T

    Public Sub New()
    End Sub

    Public Sub New(MsgType As MessageType)
        Me.Type = MsgType
    End Sub

    Public Sub New(MsgType As MessageType, Value As T)
        Me.Parameter = Value
        Me.Type = MsgType
    End Sub

End Class
