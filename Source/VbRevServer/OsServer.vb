Imports VbRev.Shared.WinApi
Imports System.Runtime.InteropServices

Public Class OsServer

    Public Shared Function GetComputerDomainName() As String
        Dim NamePtr As IntPtr
        Dim DomainInfo As NETSETUP_JOIN_STATUS
        Dim Result As Integer = NetGetJoinInformation(Nothing, NamePtr, DomainInfo)
        If Not Result = 0 Then
            Throw New ComponentModel.Win32Exception
        End If
        Dim DomaiName As String = Marshal.PtrToStringAuto(NamePtr)
        NetApiBufferFree(NamePtr)
        Return DomaiName
    End Function


    Public Shared Function Is32BitProcessOn64BitOs(ByVal TargetProcess As Process) As Boolean
        Dim IsWow64 As Boolean = False
        If MethodExistsInDll("kernel32.dll", "IsWow64Process") Then
            IsWow64Process(TargetProcess.Handle, IsWow64)
        End If
        Return IsWow64
    End Function

    Public Shared Function MethodExistsInDll(ByVal ModuleName As String, ByVal MethodName As String) As Boolean
        Dim ModuleHandle As IntPtr = GetModuleHandle(ModuleName)
        If ModuleHandle = IntPtr.Zero Then
            Return False
        End If
        Return (GetProcAddress(ModuleHandle, MethodName) <> IntPtr.Zero)
    End Function

End Class
