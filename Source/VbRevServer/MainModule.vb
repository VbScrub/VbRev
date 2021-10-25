Imports System.Net.Sockets

Module MainModule

    Private _Server As VbRevServer
    Private _AllowClose As New System.Threading.AutoResetEvent(False)

    Sub Main()
        System.Threading.Thread.Sleep(2000)
        Try
            If My.Application.CommandLineArgs.Count < 2 OrElse My.Application.CommandLineArgs(0) = "/?" Then
                ShowHelp()
                Exit Sub
            End If
            Log.Enabled = My.Application.CommandLineArgs.Contains("/debug")
            Log.VerboseEnabled = My.Application.CommandLineArgs.Contains("/v")
            Dim RemoteMachine As String = My.Application.CommandLineArgs(0)
            Dim PortNumber As Integer = Integer.Parse(My.Application.CommandLineArgs(1))
            Console.WriteLine("Connecting to " & RemoteMachine & " on port " & PortNumber & "...")
            Dim Client As New TcpClient(RemoteMachine, PortNumber)
            _Server = New VbRevServer(New NetworkSession(Client, False, TimeSpan.FromSeconds(0)), RemoteMachine, PortNumber)
            AddHandler _Server.Closed, AddressOf Server_Closed
            _Server.Start()
            Console.WriteLine("Successfully connected to " & My.Application.CommandLineArgs(0))
            _AllowClose.WaitOne()
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
    End Sub

    Private Sub ShowHelp()
        Console.WriteLine("Help text not yet implemented. See http://vbscrub.com for more information")
    End Sub

    Private Sub Server_Closed()
        Console.WriteLine("Closing down")
        _AllowClose.Set()
    End Sub

End Module
