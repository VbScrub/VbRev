<Serializable()>
Public Class EnumProcessesResponseMessage : Inherits NetworkMessage

    Public Property ProcessList As List(Of ProcessItem)

    Public Sub New()
        Me.Type = MessageType.EnumProcessesResponse
    End Sub

    Public Sub New(Processes As List(Of ProcessItem))
        Me.Type = MessageType.EnumProcessesResponse
        Me.ProcessList = Processes
    End Sub

End Class
