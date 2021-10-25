<Serializable()>
Public Class DeleteFilesRequestMessage : Inherits NetworkMessage

    Public Property Files As List(Of FileSystemItem)

    Public Sub New()
        Me.Type = MessageType.DeleteFilesRequest
    End Sub

    Public Sub New(ItemsToBeDeleted As List(Of FileSystemItem))
        Me.New()
        Me.Files = ItemsToBeDeleted
    End Sub

End Class
