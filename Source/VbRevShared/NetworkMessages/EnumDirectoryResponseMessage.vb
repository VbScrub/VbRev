<Serializable()>
Public Class EnumDirectoryResponseMessage : Inherits NetworkMessage

    Public Property Files As List(Of FileSystemItem)

    Public Sub New(FileList As List(Of FileSystemItem))
        Me.New()
        Me.Files = FileList
    End Sub

    Public Sub New()
        Me.Type = MessageType.EnumDirectoryResponse
    End Sub

End Class
