Imports System.Runtime.InteropServices

Public Class ProcessHelper

    Public Shared Sub StartProcess(ProcArgs As NewProcessArgs)
        Dim Proc As New Process
        Proc.StartInfo.UseShellExecute = False
        If Not String.IsNullOrEmpty(ProcArgs.WorkingDirectory) Then
            Proc.StartInfo.WorkingDirectory = ProcArgs.WorkingDirectory
        End If
        If Not String.IsNullOrEmpty(ProcArgs.Arguments) Then
            Proc.StartInfo.Arguments = ProcArgs.Arguments
        End If
        If Not String.IsNullOrEmpty(ProcArgs.RunAsUsername) Then
            Proc.StartInfo.UserName = ProcArgs.RunAsUsername
            Dim SecPassword As New Security.SecureString
            If Not String.IsNullOrEmpty(ProcArgs.RunAsPassword) Then
                For i As Integer = 0 To ProcArgs.RunAsPassword.Length - 1
                    SecPassword.AppendChar(ProcArgs.RunAsPassword(i))
                Next
            End If
            Proc.StartInfo.Password = SecPassword
            Proc.StartInfo.Domain = ProcArgs.RunAsDomain
        End If
        Proc.StartInfo.FileName = ProcArgs.File
        Proc.Start()
    End Sub

    Public Shared Sub EndProcess(PID As Integer)
        Process.GetProcessById(PID).Kill()
    End Sub

    Public Shared Function GetProcesses() As List(Of ProcessItem)
        Dim ProcList As Process() = Process.GetProcesses
        Dim Processes As New List(Of ProcessItem)
        For Each Proc In ProcList
            Try
                Dim NewProc As New ProcessItem(Proc.ProcessName)
                NewProc.PID = Proc.Id
                NewProc.SessionId = Proc.SessionId
                Try
                    'This fails for any processes not run by our user and therefore those process names have no file extension in our list.
                    'Look into using CreateToolhelp32Snapshot Win32 API and grabbing szExeFile member from PROCESSENTRY32 struct
                    NewProc.FileName = Proc.MainModule.ModuleName
                    NewProc.FileLocation = Proc.MainModule.FileName
                Catch ex As Exception
                    Log.WriteEntry("Error getting file path for PID " & NewProc.PID & ": " & ex.Message, False)
                End Try
                Try
                    Dim TokenHandle As IntPtr = IntPtr.Zero
                    If WinApi.OpenProcessToken(Proc.Handle, WinApi.TokenRights.TOKEN_QUERY, TokenHandle) Then
                        Using User As New System.Security.Principal.WindowsIdentity(TokenHandle)
                            NewProc.RunningAsUser = User.Name
                            WinApi.CloseHandle(TokenHandle)
                        End Using
                    End If
                Catch ex As Exception
                    Log.WriteEntry("Error getting username for PID " & NewProc.PID & ": " & ex.Message, False)
                End Try
                Try
                    NewProc.CommandLine = GetCommandLine(Proc)
                Catch ex As Exception
                    Log.WriteEntry("Error getting command line for PID " & NewProc.PID & ": " & ex.Message, False)
                End Try
                Processes.Add(NewProc)
            Catch ex As Exception
                Log.WriteEntry("Error getting process information for PID " & Proc.Id & ": " & ex.Message, False)
            End Try
        Next
        Return Processes
    End Function

    Public Shared Function GetCommandLine(ByVal TargetProcess As Process) As String
        'If we're on a 64 bit OS then the target process will have a 64 bit PEB if we are calling this function from a 64 bit process (regardless of
        'whether or not the target process is 32 bit or 64 bit).
        'If we are calling this function from a 32 bit process and the target process is 32 bit then we will get a 32 bit PEB, even on a 64 bit OS. 
        'The one situation we can't handle is if we are calling this function from a 32 bit process and the target process is 64 bit. For that we need to use the
        'undocumented NtWow64QueryInformationProcess64 and NtWow64ReadVirtualMemory64 APIs
        Dim Is64BitPeb As Boolean = False
        If Environment.Is64BitOperatingSystem() Then
            If OsHelper.Is32BitProcessOn64BitOs(Process.GetCurrentProcess) Then
                If Not OsHelper.Is32BitProcessOn64BitOs(TargetProcess) Then
                    'TODO: Use NtWow64ReadVirtualMemory64 to read from 64 bit processes when we are a 32 bit process instead of throwing this exception
                    Throw New InvalidOperationException("This function cannot be used against a 64 bit process when the calling process is 32 bit")
                End If
            Else
                Is64BitPeb = True
            End If
        End If

        'Open the target process for memory reading
        Using MemoryReader As New ProcessMemoryReader(TargetProcess)
            Dim ProcessInfo As WinApi.PROCESS_BASIC_INFORMATION = Nothing
            'Get basic information about the process, including the PEB address
            Dim Result As Integer = WinApi.NtQueryInformationProcess(TargetProcess.Handle, 0, ProcessInfo, Marshal.SizeOf(ProcessInfo), 0)
            If Not Result = 0 Then
                Throw New System.ComponentModel.Win32Exception(WinApi.RtlNtStatusToDosError(Result))
            End If
            'Get pointer from the ProcessParameters member of the PEB (PEB has different structure on x86 vs x64 so different structures needed for each)
            Dim PebLength As Integer
            If Is64BitPeb Then
                PebLength = Marshal.SizeOf(GetType(WinApi.PEB_64))
            Else
                PebLength = Marshal.SizeOf(GetType(WinApi.PEB_32))
            End If
            'Read the PEB from the PebBaseAddress pointer
            'NOTE: This pointer points to memory in the target process' address space, so Marshal.PtrToStructure won't work. We have to read it with the ReadProcessMemory API 
            Dim PebBytes() As Byte = MemoryReader.Read(ProcessInfo.PebBaseAddress, PebLength)
            'Using GCHandle.Alloc get a pointer to the byte array we read from the target process, so we can use PtrToStructure to convert those bytes to our PEB_32 or PEB_64 structure
            Dim PebBytesPtr As GCHandle = GCHandle.Alloc(PebBytes, GCHandleType.Pinned)
            Try
                Dim ProcParamsPtr As IntPtr
                'Get a pointer to the RTL_USER_PROCESS_PARAMETERS structure (again this pointer refers to the target process' memory)
                If Is64BitPeb Then
                    Dim PEB As WinApi.PEB_64 = WinApi.PtrToStructCast(Of WinApi.PEB_64)(PebBytesPtr.AddrOfPinnedObject)
                    ProcParamsPtr = PEB.ProcessParameters
                Else
                    Dim PEB As WinApi.PEB_32 = WinApi.PtrToStructCast(Of WinApi.PEB_32)(PebBytesPtr.AddrOfPinnedObject)
                    ProcParamsPtr = PEB.ProcessParameters
                End If
                'Now that we've got the pointer from the ProcessParameters member, we read the RTL_USER_PROCESS_PARAMETERS structure that is stored at that location in the target process' memory
                Dim ProcParamsBytes() As Byte = MemoryReader.Read(ProcParamsPtr, Marshal.SizeOf(GetType(WinApi.RTL_USER_PROCESS_PARAMETERS)))
                'Again we use GCHandle.Alloc to get a pointer to the byte array we just read
                Dim ProcParamsBytesPtr As GCHandle = GCHandle.Alloc(ProcParamsBytes, GCHandleType.Pinned)
                Try
                    'Convert the byte array to a RTL_USER_PROCESS_PARAMETERS structure
                    Dim ProcParams As WinApi.RTL_USER_PROCESS_PARAMETERS = WinApi.PtrToStructCast(Of WinApi.RTL_USER_PROCESS_PARAMETERS)(ProcParamsBytesPtr.AddrOfPinnedObject)
                    'Get the CommandLine member of the RTL_USER_PROCESS_PARAMETERS structure
                    Dim CmdLineUnicodeString As WinApi.UNICODE_STRING = ProcParams.CommandLine
                    'The Buffer member of the UNICODE_STRING structure points to the actual command line string we want, so we read from the location that points to
                    Dim CmdLineBytes() As Byte = MemoryReader.Read(CmdLineUnicodeString.Buffer, CmdLineUnicodeString.Length)
                    'Convert the bytes to a regular .NET String and return it
                    Return System.Text.Encoding.Unicode.GetString(CmdLineBytes)
                Finally
                    'Clean up the GCHandle we created for the RTL_USER_PROCESS_PARAMETERS bytes
                    If ProcParamsBytesPtr.IsAllocated Then
                        ProcParamsBytesPtr.Free()
                    End If
                End Try
            Finally
                'Clean up the GCHandle we created for the PEB bytes
                If PebBytesPtr.IsAllocated Then
                    PebBytesPtr.Free()
                End If
            End Try
        End Using
    End Function


    'Public Shared Function GetProcessFilePath(ByVal PID As Integer) As String
    '    Dim Length As UInteger = 260
    '    Dim Path As New Text.StringBuilder(CInt(Length))
    '    Dim hProcess As IntPtr = Win32.OpenProcess(Win32.ProcessAccess.QueryInformation, False, CUInt(PID))
    '    If Not hProcess = IntPtr.Zero Then
    '        Try
    '            'Dim Count As UInteger = NativeMethods.GetModuleFileNameExW(hProcess, Nothing, Path, Length)
    '            'Return Path.ToString(0, CInt(Count))
    '            If Not Win32.QueryFullProcessImageName(hProcess, 0, Path, Length) Then
    '                Throw New ComponentModel.Win32Exception
    '            End If
    '            'Length = NativeMethods.GetProcessImageFileName(hProcess, Path, Length)
    '            Return Path.ToString(0, CInt(Length))
    '        Finally
    '            WindowsApi.Win32.CloseHandle(hProcess)
    '        End Try
    '    Else
    '        Throw New Win32Exception
    '    End If
    'End Function

End Class
