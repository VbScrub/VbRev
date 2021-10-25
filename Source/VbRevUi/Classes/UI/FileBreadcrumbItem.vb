Public Class DirectoryBreadcrumbItem

    Public Property Name As String
    Public Property FullPath As String

    Public Sub New()
    End Sub

    Public Sub New(DirName As String, DirPath As String)
        Me.Name = DirName
        Me.FullPath = DirPath
    End Sub

End Class
