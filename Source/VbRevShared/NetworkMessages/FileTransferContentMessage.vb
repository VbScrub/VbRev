<Serializable()>
Public Class FileTransferContentMessage : Inherits NetworkMessage

    Public Property TotalFileSize As Int64
    Public Property IsFinalChunk As Boolean
    Public Property ChunkData As Byte()

    Public Sub New()
        Me.Type = MessageType.TransferFileContent
    End Sub

    Public Sub New(Bytes() As Byte, FinalChunk As Boolean, FileLength As Int64)
        Me.New()
        Me.ChunkData = Bytes
        Me.IsFinalChunk = FinalChunk
        Me.TotalFileSize = FileLength
    End Sub



End Class
