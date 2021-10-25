<Serializable()>
Public Class FileSystemItem

    Public Enum FileItemType As Integer
        Unknown = 0
        File = 1
        Directory = 2
        Drive = 3
    End Enum

    Public Property Type As FileItemType
    Public Property Size As Int64
    Public Property ModifiedDate As Date
    Public Property CreatedDate As Date
    Public Property Name As String
    Public Property FullPath As String

End Class
