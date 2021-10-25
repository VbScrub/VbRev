<Serializable()>
Public Class DeleteFilesResponseMessage : Inherits NetworkMessage

    Public Property DeletedItemNames As List(Of String)
    Public Property DeletedItemsFailed As String

    Public Sub New()
        Me.Type = MessageType.DeleteFilesResponse
    End Sub

    Public Sub New(DeletedItems As List(Of String), Failures As String)
        Me.New()
        Me.DeletedItemNames = DeletedItems
        Me.DeletedItemsFailed = Failures
    End Sub

End Class
