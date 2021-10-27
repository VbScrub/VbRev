<Serializable()>
Public Class NewProcessArgs

    Public Property File As String
    Public Property Arguments As String
    Public Property WorkingDirectory As String
    Public Property RunAsUsername As String
    Public Property RunAsPassword As String
    Public Property RunAsDomain As String


    Public Sub New()
    End Sub

    Public Sub New(FileName As String, FileArgs As String, WorkingDir As String)
        Me.File = FileName
        Me.Arguments = FileArgs
        Me.WorkingDirectory = WorkingDir
    End Sub


    Public Sub New(FileName As String, FileArgs As String, WorkingDir As String, Username As String, Password As String, Domain As String)
        Me.New(FileName, FileArgs, WorkingDir)
        Me.RunAsUsername = Username
        Me.RunAsPassword = Password
        Me.RunAsDomain = Domain
    End Sub

End Class
