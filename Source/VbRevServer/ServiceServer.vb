Public Class ServiceServer

    Public Property NetClient As NetworkSession


    Public Sub New(Client As NetworkSession)
        Me.NetClient = Client
    End Sub

    Public Function GetServices(FromRegistry As Boolean) As List(Of ServiceItem)

    End Function

    Public Function GetServiceControlManagerHandle(ByVal MachineName As String, ByVal Access As UInteger) As IntPtr
        Dim ScmPtr As IntPtr = WinApi.OpenSCManager(MachineName, Nothing, Access)
        If ScmPtr = IntPtr.Zero Then
            Throw New ApplicationException("Unable to connect to service manager on " & MachineName & ". The last error that was reported was: " & New System.ComponentModel.Win32Exception().Message)
        End If
        Return ScmPtr
    End Function

End Class
