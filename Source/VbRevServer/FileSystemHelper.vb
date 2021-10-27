Public Class FileSystemHelper

    Public Const Drives As String = "[DRIVES]"

    Public Shared Function EnumDirectory(Path As String) As List(Of FileSystemItem)
        Dim FileList As New List(Of FileSystemItem)
        If Path = Drives Then
            For Each Drive In IO.DriveInfo.GetDrives
                Try
                    Dim DriveItem As New FileSystemItem()
                    DriveItem.Type = FileSystemItem.FileItemType.Drive
                    DriveItem.Name = Drive.Name & If(String.IsNullOrWhiteSpace(Drive.VolumeLabel), String.Empty, " (" & Drive.VolumeLabel & ")")
                    DriveItem.FullPath = Drive.Name
                    FileList.Add(DriveItem)
                Catch ex As Exception
                    'Ignore (exceptions get thrown by DVD drives etc)
                End Try
            Next
        Else 'getting files/folders instead of drives
            For Each Directory As String In IO.Directory.GetDirectories(Path)
                Dim DirItem As New FileSystemItem
                DirItem.FullPath = Directory
                DirItem.Type = FileSystemItem.FileItemType.Directory
                DirItem.Name = IO.Path.GetFileName(Directory)
                FileList.Add(DirItem)
            Next

            For Each FilePath As String In IO.Directory.GetFiles(Path)
                Dim FileItem As New FileSystemItem
                FileItem.FullPath = FilePath
                FileItem.Type = FileSystemItem.FileItemType.File
                FileItem.Name = IO.Path.GetFileName(FilePath)
                Try
                    Dim Info As New IO.FileInfo(FilePath)
                    FileItem.Size = Info.Length
                    FileItem.ModifiedDate = Info.LastWriteTime
                    FileItem.CreatedDate = Info.CreationTime
                Catch ex As Exception
                    Log.WriteEntry("Error getting file info from " & FilePath, False)
                End Try
                FileList.Add(FileItem)
            Next
        End If

        Return FileList
    End Function

    Public Shared Sub RenameFile(Path As String, NewName As String)
        My.Computer.FileSystem.RenameFile(Path, NewName)
    End Sub

    Public Shared Sub RenameDirectory(Path As String, NewName As String)
        My.Computer.FileSystem.RenameDirectory(Path, NewName)
    End Sub

    Public Shared Function DeleteFiles(Files As List(Of FileSystemItem), ByRef Failures As String) As List(Of String)
        Dim SuccessList As New List(Of String)
        Dim FailureText As String = String.Empty
        For Each DeletedFile As FileSystemItem In Files
            Try
                If DeletedFile.Type = FileSystemItem.FileItemType.File Then
                    My.Computer.FileSystem.DeleteFile(DeletedFile.FullPath)
                    SuccessList.Add(DeletedFile.Name)
                ElseIf DeletedFile.Type = FileSystemItem.FileItemType.Directory Then
                    My.Computer.FileSystem.DeleteDirectory(DeletedFile.FullPath, FileIO.DeleteDirectoryOption.DeleteAllContents)
                    SuccessList.Add(DeletedFile.Name)
                Else
                    FailureText &= DeletedFile.Name & " is not a file or directory" & Environment.NewLine
                End If
            Catch ex As Exception
                FailureText &= DeletedFile.Name & " could not be deleted due to error: " & ex.Message & Environment.NewLine
            End Try
        Next
        Failures = FailureText
        Return SuccessList
    End Function

    Public Shared Sub CreateDirectory(Path As String)
        If IO.Directory.Exists(Path) Then
            Throw New ApplicationException("The directory already exists (" & Path & ")")
        End If
        IO.Directory.CreateDirectory(Path)
    End Sub


End Class
