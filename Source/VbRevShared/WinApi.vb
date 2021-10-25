Imports System.Runtime.InteropServices
Imports System.Net.Sockets
Imports System.ComponentModel

Public Class WinApi

#Region "Constants"

    Public Const ErrorInsufficientBuffer As UInteger = 122

#End Region

#Region "Enums"

    Public Enum TCP_TABLE_CLASS
        TcpTableBasicListener = 0
        TcpTableBasicConnections = 1
        TcpTableBasicAll = 2
        TcpTableOwnerPidListener = 3
        TcpTableOwnerPidConnections = 4
        TcpTableOwnerPidAll = 5
        TcpTableOwnerModuleListener = 6
        TcpTableOwnerModuleConnections = 7
        TcpTableOwnerModuleAll = 8
    End Enum

    <Flags()> _
    Public Enum ProcessAccess As UInteger
        AllAccess = CreateThread Or DuplicateHandle Or QueryInformation Or SetInformation Or Terminate Or VMOperation Or VMRead Or VMWrite Or Synchronize
        CreateThread = &H2
        DuplicateHandle = &H40
        QueryInformation = &H400
        QueryLimitedInformation = &H1000
        SetInformation = &H200
        Terminate = &H1
        VMOperation = &H8
        VMRead = &H10
        VMWrite = &H20
        Synchronize = &H100000
    End Enum

    <Flags()> _
    Public Enum TokenRights As UInteger
        READ_CONTROL = &H20000
        STANDARD_RIGHTS_REQUIRED = &HF0000
        STANDARD_RIGHTS_READ = READ_CONTROL
        STANDARD_RIGHTS_WRITE = READ_CONTROL
        STANDARD_RIGHTS_EXECUTE = READ_CONTROL
        STANDARD_RIGHTS_ALL = &H1F0000
        SPECIFIC_RIGHTS_ALL = &HFFFF
        TOKEN_ASSIGN_PRIMARY = &H1
        TOKEN_DUPLICATE = &H2
        TOKEN_IMPERSONATE = &H4
        TOKEN_QUERY = &H8
        TOKEN_QUERY_SOURCE = &H10
        TOKEN_ADJUST_PRIVILEGES = &H20
        TOKEN_ADJUST_GROUPS = &H40
        TOKEN_ADJUST_DEFAULT = &H80
        TOKEN_ADJUST_SESSIONID = &H100
    End Enum

    Public Enum SERVICE_STATES As UInteger
        SERVICE_CONTINUE_PENDING = 5
        SERVICE_PAUSE_PENDING = 6
        SERVICE_PAUSED = 7
        SERVICE_RUNNING = 4
        SERVICE_START_PENDING = 2
        SERVICE_STOP_PENDING = 3
        SERVICE_STOPPED = 1
    End Enum

    Public Enum SERVICE_CONTROL_CODES As UInteger
        SERVICE_CONTROL_CONTINUE = 3
        SERVICE_CONTROL_INTERROGATE = 4
        SERVICE_CONTROL_NETBINDADD = 7
        SERVICE_CONTROL_NETBINDDISABLE = &HA
        SERVICE_CONTROL_NETBINDENABLE = 9
        SERVICE_CONTROL_NETBINDREMOVE = 8
        SERVICE_CONTROL_PARAMCHANGE = 6
        SERVICE_CONTROL_PAUSE = 2
        SERVICE_CONTROL_STOP = 1
    End Enum

    Public Enum NETSETUP_JOIN_STATUS
        NetSetupUnknownStatus = 0
        NetSetupUnjoined = 1
        NetSetupWorkgroupName = 2
        NetSetupDomainName = 3
    End Enum

#End Region

#Region "Structs"

    <StructLayout(LayoutKind.Sequential)>
    Public Structure UNICODE_STRING
        Public Length As UInt16
        Public MaximumLength As UInt16
        '64 bit version of this actually has 4 bytes of padding here (after MaximumLength and before Buffer), but the default Pack size for structs handles this for us
        Public Buffer As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure RTL_USER_PROCESS_PARAMETERS
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=16)>
        Public Reserved1() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=10)>
        Public Reserved2() As IntPtr
        Public ImagePathName As UNICODE_STRING
        Public CommandLine As UNICODE_STRING
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure PEB_32
        <MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst:=2)>
        Public Reserved1() As Byte
        Public BeingDebugged As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=1)> _
        Public Reserved2() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=2)> _
        Public Reserved3() As IntPtr
        Public Ldr As IntPtr
        Public ProcessParameters As IntPtr
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=3)> _
        Public Reserved4() As IntPtr
        Public AtlThunkSListPtr As IntPtr
        Public Reserved5 As IntPtr
        Public Reserved6 As UInteger
        Public Reserved7 As IntPtr
        Public Reserved8 As UInteger
        Public AtlThunkSListPtr32 As UInteger
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=45)> _
        Public Reserved9() As IntPtr
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=96)> _
        Public Reserved10() As Byte
        Public PostProcessInitRoutine As IntPtr
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=128)> _
        Public Reserved11() As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=1)> _
        Public Reserved12() As IntPtr
        Public SessionId As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure PEB_64
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=2)>
        Public Reserved1() As Byte
        Public BeingDebugged As Byte
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=21)>
        Public Reserved2() As Byte
        Public LoaderData As IntPtr
        Public ProcessParameters As IntPtr
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=520)>
        Public Reserved3() As Byte
        Public PostProcessInitRoutine As IntPtr
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=136)>
        Public Reserved4() As Byte
        Public SessionId As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure PROCESS_BASIC_INFORMATION
        Public Reserved1 As IntPtr
        Public PebBaseAddress As IntPtr
        <MarshalAs(UnmanagedType.ByValArray, SizeConst:=2)>
        Public Reserved2() As IntPtr
        Public UniqueProcessID As IntPtr
        Public Reserved3 As IntPtr
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure MibTcpTableOwnerPid
        Public NumberOfEntries As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Public Structure MibTcpRow
        Public state As Net.NetworkInformation.TcpState
        Public localAddr As UInteger
        Public localPort1 As Byte
        Public localPort2 As Byte
        Public ignoreLocalPort3 As Byte 'ignore last 2 bytes of port number as port numbers can only be 16 bit
        Public ignoreLocalPort4 As Byte
        Public remoteAddr As UInteger
        Public remotePort1 As Byte
        Public remotePort2 As Byte
        Public ignoreRemotePort3 As Byte
        Public ignoreRemotePort4 As Byte
        Public owningPid As UInteger
    End Structure

#End Region

#Region "Methods"

    'Generic function used instead of Marshal.PtrToStructure just to make code easier to read
    Public Shared Function PtrToStructCast(Of T)(Pointer As IntPtr) As T
        Return DirectCast(Marshal.PtrToStructure(Pointer, GetType(T)), T)
    End Function

#End Region

#Region "API Definitions"

    <DllImport("ntdll.dll", EntryPoint:="RtlNtStatusToDosError", SetLastError:=True)> _
    Public Shared Function RtlNtStatusToDosError(NtStatus As Integer) As Integer
    End Function

    <DllImport("kernel32.dll", EntryPoint:="IsWow64Process", SetLastError:=True)> _
    Public Shared Function IsWow64Process(<InAttribute()> ByVal hProcess As System.IntPtr, <OutAttribute()> ByRef Wow64Process As Boolean) As <MarshalAsAttribute(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("kernel32.dll", EntryPoint:="GetModuleHandle", SetLastError:=True)> _
    Public Shared Function GetModuleHandle(ByVal moduleName As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Ansi, SetLastError:=True, ExactSpelling:=True)> _
    Public Shared Function GetProcAddress(ByVal hModule As IntPtr, ByVal methodName As String) As IntPtr
    End Function

    <DllImport("ntdll.dll", EntryPoint:="NtQueryInformationProcess", SetLastError:=True)> _
    Public Shared Function NtQueryInformationProcess(ByVal handle As IntPtr, ByVal processinformationclass As UInteger, ByRef ProcessInformation As PROCESS_BASIC_INFORMATION,
                                                         ByVal ProcessInformationLength As Integer, ByRef ReturnLength As UInteger) As Integer
    End Function

    <DllImport("kernel32.dll", EntryPoint:="ReadProcessMemory", SetLastError:=True)> _
    Public Shared Function ReadProcessMemory(<InAttribute()> ByVal hProcess As System.IntPtr, <InAttribute()> ByVal lpBaseAddress As IntPtr, <Out()> ByVal lpBuffer As Byte(),
                                                 ByVal nSize As UInteger, <Out()> ByRef lpNumberOfBytesRead As UInteger) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("Iphlpapi.dll", EntryPoint:="GetExtendedTcpTable")> _
    Public Shared Function GetExtendedTcpTable(ByVal pTcpTable As IntPtr, ByRef pdwSize As UInteger, <MarshalAs(UnmanagedType.Bool)> ByVal bOrder As Boolean,
                                                   ByVal ulAf As UInteger, ByVal TableClass As TCP_TABLE_CLASS, ByVal Reserved As UInteger) As UInteger
    End Function

    <DllImport("Psapi.dll", EntryPoint:="GetModuleFileNameExW", SetLastError:=True)> _
    Public Shared Function GetModuleFileNameExW(ByVal hProcess As IntPtr, ByVal hModule As IntPtr, <MarshalAs(UnmanagedType.LPWStr)> ByVal lpFilename As Text.StringBuilder, ByVal nSize As UInteger) As UInteger
    End Function


    <DllImport("Psapi.dll", EntryPoint:="GetProcessImageFileNameW", SetLastError:=True)> _
    Public Shared Function GetProcessImageFileName(ByVal hProcess As IntPtr, <MarshalAs(UnmanagedType.LPWStr)> ByVal lpImageFileName As System.Text.StringBuilder, ByVal nSize As UInteger) As UInteger
    End Function

    <DllImport("kernel32.dll", EntryPoint:="QueryFullProcessImageNameW", SetLastError:=True)> _
    Public Shared Function QueryFullProcessImageName(ByVal hProcess As IntPtr, ByVal dwFlags As UInteger, <MarshalAs(UnmanagedType.LPWStr)> ByVal lpExeName As System.Text.StringBuilder, ByRef lpdwSize As UInteger) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("kernel32.dll", EntryPoint:="OpenProcess", SetLastError:=True)> _
    Public Shared Function OpenProcess(ByVal dwDesiredAccess As ProcessAccess, <MarshalAs(UnmanagedType.Bool)> ByVal bInheritHandle As Boolean, ByVal dwProcessId As UInteger) As IntPtr
    End Function

    <DllImport("kernel32.dll", EntryPoint:="CloseHandle", SetLastError:=True)> _
    Public Shared Function CloseHandle(<InAttribute()> ByVal Handle As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="OpenProcessToken", SetLastError:=True)> _
    Public Shared Function OpenProcessToken(<InAttribute()> ByVal ProcessHandle As IntPtr, ByVal DesiredAccess As TokenRights, ByRef TokenHandle As System.IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="StartServiceW", SetLastError:=True)> _
    Public Shared Function StartService(<InAttribute()> ByVal hService As IntPtr, ByVal dwNumServiceArgs As UInteger, _
                                        ByVal lpServiceArgVectors As System.IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="OpenServiceW", SetLastError:=True)> _
    Public Shared Function OpenService(<InAttribute()> ByVal hSCManager As IntPtr, <InAttribute(), MarshalAs(UnmanagedType.LPWStr)> ByVal lpServiceName As String, _
                                        ByVal dwDesiredAccess As UInteger) As System.IntPtr
    End Function

    <DllImport("advapi32.dll", EntryPoint:="EnumServicesStatusExW", SetLastError:=True)> _
    Public Shared Function EnumServicesStatusEx(<InAttribute()> ByVal hSCManager As IntPtr, ByVal InfoLevel As UInteger, ByVal dwServiceType As UInteger, ByVal dwServiceState As UInteger, ByVal lpServices As System.IntPtr, ByVal cbBufSize As UInteger, <OutAttribute()> ByRef pcbBytesNeeded As UInteger, _
                                               <OutAttribute()> ByRef lpServicesReturned As UInteger, ByVal lpResumeHandle As System.IntPtr, <InAttribute(), MarshalAs(UnmanagedType.LPWStr)> ByVal pszGroupName As String) As <MarshalAsAttribute(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="CloseServiceHandle", SetLastError:=True)> _
    Public Shared Function CloseServiceHandle(<InAttribute()> ByVal hSCObject As IntPtr) As <MarshalAsAttribute(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="OpenSCManagerW", SetLastError:=True)> _
    Public Shared Function OpenSCManager(<InAttribute(), MarshalAsAttribute(UnmanagedType.LPWStr)> ByVal lpMachineName As String, _
                                         <InAttribute(), MarshalAsAttribute(UnmanagedType.LPWStr)> ByVal lpDatabaseName As String, _
                                         ByVal dwDesiredAccess As UInteger) As IntPtr
    End Function

    <DllImport("Netapi32.dll", SetLastError:=True, EntryPoint:="NetGetJoinInformation")> _
    Public Shared Function NetGetJoinInformation(<InAttribute(), MarshalAs(UnmanagedType.LPWStr)> ByVal lpServer As String, ByRef lpNameBuffer As System.IntPtr, ByRef BufferType As NETSETUP_JOIN_STATUS) As Integer
    End Function

    <DllImport("Netapi32.dll", SetLastError:=True, EntryPoint:="NetApiBufferFree")> _
    Public Shared Function NetApiBufferFree(ByVal Buffer As System.IntPtr) As Integer
    End Function

    <DllImport("shell32.dll", EntryPoint:="ExtractIconExW", CallingConvention:=CallingConvention.StdCall, SetLastError:=True)> _
    Public Shared Function ExtractIconEx(<InAttribute(), MarshalAs(UnmanagedType.LPWStr)> ByVal lpszFile As String, ByVal nIconIndex As Integer, ByRef phiconLarge As System.IntPtr,
                                             ByRef phiconSmall As System.IntPtr, ByVal nIcons As UInteger) As UInteger
    End Function

    <DllImport("user32.dll", EntryPoint:="DestroyIcon", SetLastError:=True)> _
    Public Shared Function DestroyIcon(<InAttribute()> ByVal hIcon As System.IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

#End Region
   

End Class
