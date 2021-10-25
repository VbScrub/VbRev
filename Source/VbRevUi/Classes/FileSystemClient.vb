Public Class FileSystemClient : Inherits ClientBase

    Public Const DrivesPath As String = "[DRIVES]"

    Public Property FileTransferPort As Integer

    Delegate Sub EnumDirCallback(ErrorMessage As String, Files As List(Of FileSystemItem))
    Delegate Sub RenameFileCallback(ErrorMessage As String)
    Delegate Sub DeleteFilesCallback(ErrorMessage As String, DeletedItemNames As List(Of String), Failed As String)

    Public Sub New(Client As NetworkSession, Port As Integer)
        Me.NetClient = Client
        Me.FileTransferPort = Port
    End Sub

    Public Shared Function IsValidPath(Path As String) As Boolean
        For Each InvalidChar As Char In IO.Path.GetInvalidPathChars
            If Path.Contains(InvalidChar) Then
                Return False
            End If
        Next
        Return True
    End Function

    Public Sub EnumDirectoryAsync(Path As String, Callback As EnumDirCallback)
        System.Threading.ThreadPool.QueueUserWorkItem(AddressOf EnumDirectory, New Tuple(Of String, EnumDirCallback)(Path, Callback))
    End Sub

    Private Sub EnumDirectory(Args As Object)
        Dim ErrorMsg As String = Nothing
        Dim Params As Tuple(Of String, EnumDirCallback) = DirectCast(Args, Tuple(Of String, EnumDirCallback))
        Try
            NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.EnumDirectoryRequest, Params.Item1))
            Dim Response As EnumDirectoryResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.EnumDirectoryResponse), EnumDirectoryResponseMessage)
            If Response.Files Is Nothing Then
                Throw New ApplicationException("Null file list received from server")
            End If
            Params.Item2.Invoke(Nothing, Response.Files)
        Catch ex As Exception
            Params.Item2.Invoke(ex.Message, Nothing)
        End Try
    End Sub

    Public Sub RenameFileAsync(Path As String, NewName As String, IsDirectory As Boolean, Callback As RenameFileCallback)
        System.Threading.ThreadPool.QueueUserWorkItem(AddressOf RenameFile, New RenameFileArgs(Path, NewName, IsDirectory, Callback))
    End Sub

    Private Sub RenameFile(Args As Object)
        Dim ErrorMsg As String = Nothing
        Dim RenameArgs As RenameFileArgs = DirectCast(Args, RenameFileArgs)
        Try
            NetClient.Send(New RenameFileRequestMessage(RenameArgs.Path, RenameArgs.NewName, RenameArgs.IsDirectory))
            GetServerResponse(NetworkMessage.MessageType.Success) 'will throw exception if response is not success
            RenameArgs.Callback.Invoke(Nothing)
        Catch ex As Exception
            RenameArgs.Callback.Invoke(ex.Message)
        End Try
    End Sub

    Public Sub DeleteFilesAsync(Files As List(Of FileSystemItem), Callback As DeleteFilesCallback)
        System.Threading.ThreadPool.QueueUserWorkItem(AddressOf DeleteFiles, New DeleteFilesArgs(Files, Callback))
    End Sub

    Private Sub DeleteFiles(Args As Object)
        Dim ErrorMessage As String = Nothing
        Dim DeleteParams As DeleteFilesArgs = DirectCast(Args, DeleteFilesArgs)
        Try
            NetClient.Send(New DeleteFilesRequestMessage(DeleteParams.Files))
            Dim Response As DeleteFilesResponseMessage = DirectCast(GetServerResponse(NetworkMessage.MessageType.DeleteFilesResponse), DeleteFilesResponseMessage)
            DeleteParams.Callback.Invoke(Nothing, Response.DeletedItemNames, Response.DeletedItemsFailed)
        Catch ex As Exception
            DeleteParams.Callback.Invoke(ex.Message, Nothing, Nothing)
        End Try
    End Sub

    Public Sub CreateDirectory(Path As String)
        NetClient.Send(New SingleParamMessage(Of String)(NetworkMessage.MessageType.CreateDirectoryRequest, Path))
        GetServerResponse(NetworkMessage.MessageType.Success) 'will throw exception if server doesn't respond with success
    End Sub


#Region "Child Classes"

    Public Class RenameFileArgs
        Public Property Path As String
        Public Property NewName As String
        Public Property IsDirectory As Boolean
        Public Property Callback As RenameFileCallback
        Public Sub New(PathArg As String, NewNameArg As String, IsDirectoryArg As Boolean, CallbackArg As RenameFileCallback)
            Path = PathArg
            NewName = NewNameArg
            IsDirectory = IsDirectoryArg
            Callback = CallbackArg
        End Sub
    End Class

    Public Class DeleteFilesArgs
        Public Property Files As List(Of FileSystemItem)
        Public Property Callback As DeleteFilesCallback
        Public Sub New(FilesArg As List(Of FileSystemItem), CallbackArg As DeleteFilesCallback)
            Files = FilesArg
            Callback = CallbackArg
        End Sub
    End Class

#End Region





End Class
