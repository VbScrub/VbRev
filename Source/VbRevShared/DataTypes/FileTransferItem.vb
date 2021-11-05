Public Class FileTransferItem

    Private Const ChunkSize As Integer = 1048576 '1 MB

    Public Enum ProgressState As Integer
        Unknown = 0
        Queued = 1
        Transferring = 2
        Complete = 3
        Failed = 4
        Cancelled = 5
    End Enum

    Public Event ProgressUpdated(State As ProgressState, ProgressPercent As Integer, StateDetails As String)

    Public Property FileName As String
    Public Property SourcePath As String
    Public Property OutputPath As String
    Public Property CurrentState As ProgressState = ProgressState.Queued
    Public Property ProgressUpdateFrequency As TimeSpan
    Public Property ClientMode As Boolean
    Private _Cancelled As Boolean

    'This constructor is used on the server
    Public Sub New(FilePath As String, IsUpload As Boolean)
        If IsUpload Then
            Me.SourcePath = FilePath
        Else
            Me.OutputPath = FilePath
        End If
    End Sub

    'server doesn't care about progress updates so we're only using this constructor on the client
    Public Sub New(Name As String, SourceFilePath As String, OutputFilePath As String, ReportProgressEvery As TimeSpan)
        Me.ClientMode = True
        Me.ProgressUpdateFrequency = ReportProgressEvery
        Me.FileName = Name
        Me.SourcePath = SourceFilePath
        Me.OutputPath = OutputFilePath
    End Sub

    Public Sub UpdateProgress(State As ProgressState, ProgressPercentage As Integer, Details As String)
        Me.CurrentState = State
        RaiseEvent ProgressUpdated(State, ProgressPercentage, Details)
    End Sub

    Public Sub RequestCancellation()
        _Cancelled = True
    End Sub

    Private Sub Cancel(NetClient As NetworkSession)
        If Not Me.ProgressUpdateFrequency = TimeSpan.Zero Then
            UpdateProgress(ProgressState.Cancelled, 0, "Transfer cancelled")
        End If
        If Not NetClient Is Nothing Then
            NetClient.Close()
        End If
    End Sub

    'This can be running on both server and client
    Public Sub ReceiveFile(NetClient As NetworkSession, Overwrite As Boolean)
        If _Cancelled Then
            Cancel(NetClient)
            Exit Sub
        End If
        If ClientMode Then
            UpdateProgress(ProgressState.Transferring, 0, "Initiating download...")
        End If
        Dim FileCreated As Boolean = False
        Dim CompletedSuccessfully As Boolean = False
        Try
            Using File As New IO.FileStream(Me.OutputPath, If(Overwrite, IO.FileMode.Create, IO.FileMode.CreateNew), IO.FileAccess.ReadWrite, IO.FileShare.ReadWrite)
                FileCreated = True
                If Not ClientMode Then
                    'If running on server, client will be waiting for success or error message (so client can receive error message about initial file creation on server)
                    NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.Success))
                End If
                Dim TotalFileLength As Int64 = 0
                Dim BytesReceived As Int64 = 0
                Dim Timer As New Stopwatch
                Do
                    If _Cancelled Then
                        Timer.Stop()
                        Cancel(NetClient)
                        Exit Sub
                    End If
                    If Me.ClientMode Then
                        Timer.Start()
                    End If
                    Dim NetMsg As NetworkMessage = NetClient.ReceiveMessage()

                    If NetMsg.Type = NetworkMessage.MessageType.ErrorDetail Then
                        Throw New ApplicationException("Error reported from remote machine: " & DirectCast(NetMsg, SingleParamMessage(Of String)).Parameter)
                    ElseIf Not NetMsg.Type = NetworkMessage.MessageType.TransferFileContent Then
                        Throw New ApplicationException("Unexpected response from remote machine (expecting " & NetworkMessage.MessageType.TransferFileContent.ToString & " but received " & NetMsg.Type.ToString & ")")
                    End If
                    Dim TransferMessage As FileTransferContentMessage = DirectCast(NetMsg, FileTransferContentMessage)
                    If TransferMessage.IsFinalChunk Then
                        Exit Do
                    End If
                    File.Write(TransferMessage.ChunkData, 0, TransferMessage.ChunkData.Length)
                    If Me.ClientMode Then
                        BytesReceived += TransferMessage.ChunkData.Length
                        If Not TransferMessage.TotalFileSize = 0 Then
                            TotalFileLength = TransferMessage.TotalFileSize
                        End If
                        If Timer.Elapsed > Me.ProgressUpdateFrequency Then
                            Timer.Reset()
                            Dim ProgressPercentage As Integer = 0
                            If Not TotalFileLength = 0 AndAlso Not BytesReceived = 0 Then
                                ProgressPercentage = CInt((BytesReceived / TotalFileLength) * 100)
                            End If
                            UpdateProgress(ProgressState.Transferring, ProgressPercentage, FileHelper.GetFileSizeString(BytesReceived) & " / " & FileHelper.GetFileSizeString(TotalFileLength) & " downloaded")
                        End If
                    End If
                Loop
                Timer.Stop()
            End Using
            CompletedSuccessfully = True
            If Me.ClientMode Then
                UpdateProgress(ProgressState.Complete, 100, "Download completed successfully")
            Else
                NetClient.Send(New EmptyMessage(NetworkMessage.MessageType.Success))
            End If
        Catch ex As Exception
            Log.WriteEntry("File download error: " & ex.Message, False)

            If Me.ClientMode Then
                UpdateProgress(ProgressState.Failed, Nothing, ex.Message)
            Else
                If Not ex.GetType Is GetType(NetworkSession.ClientDisconnectedException) Then
                    Try
                        NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, ex.Message))
                    Catch SendEx As Exception
                        Log.WriteEntry("Error sending error details to client: " & SendEx.Message, False)
                    End Try
                End If
            End If
        Finally
            If FileCreated AndAlso Not CompletedSuccessfully Then
                DeleteFailedFile(Me.OutputPath)
            End If
            NetClient.Close()
        End Try
    End Sub

    Private Sub DeleteFailedFile(File As String)
        Try
            If IO.File.Exists(File) Then
                IO.File.Delete(File)
            End If
        Catch Ex As Exception
            Log.WriteEntry("Error deleting cancelled/failed file transfer: " & Ex.Message, False)
        End Try
    End Sub

    'This can be running on both server and client
    Public Sub SendFile(NetClient As NetworkSession)
        If _Cancelled Then
            Cancel(NetClient)
            Exit Sub
        End If
        If Me.ClientMode Then
            UpdateProgress(ProgressState.Transferring, 0, "Initiating upload...")
        End If

        Try
            Log.WriteEntry("Opening file " & Me.SourcePath, True)
            Using File As New IO.FileStream(Me.SourcePath, IO.FileMode.Open, IO.FileAccess.Read, IO.FileShare.ReadWrite)
                If ClientMode Then
                    Dim NetMsg As NetworkMessage = NetClient.ReceiveMessage()
                    If NetMsg.Type = NetworkMessage.MessageType.ErrorDetail Then
                        Throw New ApplicationException("Error reported from remote machine: " & DirectCast(NetMsg, SingleParamMessage(Of String)).Parameter)
                    ElseIf Not NetMsg.Type = NetworkMessage.MessageType.Success Then
                        Throw New ApplicationException("Unexpected response from server (expecting " & NetworkMessage.MessageType.TransferFileContent.ToString & " but received " & NetMsg.Type.ToString & ")")
                    End If
                End If
                Dim BytesSent As Int64 = 0
                Dim Timer As New Stopwatch
                Do
                    If _Cancelled Then
                        Timer.Stop()
                        Cancel(NetClient)
                        Exit Sub
                    End If
                    If ClientMode Then
                        Timer.Start()
                    End If
                    Dim CurrentChunk(ChunkSize - 1) As Byte
                    Array.Clear(CurrentChunk, 0, CurrentChunk.Length)
                    Dim ReadCount As Integer = File.Read(CurrentChunk, 0, ChunkSize)
                    Array.Resize(CurrentChunk, ReadCount)
                    If ReadCount = 0 Then
                        NetClient.Send(New FileTransferContentMessage(Nothing, True, File.Length))
                        Exit Do
                    Else
                        BytesSent += ReadCount
                        NetClient.Send(New FileTransferContentMessage(CurrentChunk, False, File.Length))
                    End If

                    If Timer.Elapsed > Me.ProgressUpdateFrequency Then
                        Timer.Reset()
                        Dim ProgressPercentage As Integer = 0
                        If Not File.Length = 0 AndAlso Not BytesSent = 0 Then
                            ProgressPercentage = CInt((BytesSent / File.Length) * 100)
                        End If
                        UpdateProgress(ProgressState.Transferring, ProgressPercentage, FileHelper.GetFileSizeString(BytesSent) & " / " & FileHelper.GetFileSizeString(File.Length) & " uploaded")
                    End If
                Loop
                Timer.Stop()
            End Using
            If ClientMode Then
                Dim NetMsg As NetworkMessage = NetClient.ReceiveMessage()
                If NetMsg.Type = NetworkMessage.MessageType.ErrorDetail Then
                    Throw New ApplicationException("Error reported from remote machine: " & DirectCast(NetMsg, SingleParamMessage(Of String)).Parameter)
                ElseIf Not NetMsg.Type = NetworkMessage.MessageType.Success Then
                    Throw New ApplicationException("Unexpected response from server (expecting " & NetworkMessage.MessageType.TransferFileContent.ToString & " but received " & NetMsg.Type.ToString & ")")
                End If
                UpdateProgress(ProgressState.Complete, 100, "Upload completed successfully")
            End If
        Catch ex As Exception
            Log.WriteEntry("File upload error: " & ex.Message, False)
            If ClientMode Then
                UpdateProgress(ProgressState.Failed, Nothing, ex.Message)
            Else
                If Not ex.GetType Is GetType(NetworkSession.ClientDisconnectedException) Then
                    Try
                        NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.ErrorDetail, ex.Message))
                    Catch SendEx As Exception
                        Log.WriteEntry("Error sending error details to client: " & SendEx.Message, False)
                    End Try
                End If
            End If
        Finally
            NetClient.Close()
        End Try
    End Sub

End Class
