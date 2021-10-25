<Serializable()>
Public Class RenameFileRequestMessage : Inherits NetworkMessage

    Public Property Path As String
    Public Property NewName As String
    Public Property IsDirectory As Boolean

    Public Sub New()
        Me.Type = MessageType.RenameFileRequest
    End Sub

    Public Sub New(FilePath As String, FileNewName As String, FileIsDirectory As Boolean)
        Me.New()
        Me.Path = FilePath
        Me.NewName = FileNewName
        Me.IsDirectory = FileIsDirectory
    End Sub

End Class
