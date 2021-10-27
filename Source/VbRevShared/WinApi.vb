Imports System.Runtime.InteropServices
Imports System.Net.Sockets
Imports System.ComponentModel

Public Class WinApi

#Region "Constants"

    Public Const ErrorInsufficientBuffer As UInteger = 122
    Public Const GENERIC_READ As UInteger = &H80000000UI
    Public Const GENERIC_ALL As UInteger = &H10000000
    Public Const GENERIC_WRITE As UInteger = &H40000000
    Public Const GENERIC_EXECUTE As UInteger = &H20000000
    Public Const READ_CONTROL As UInteger = &H20000
    Public Const SC_ENUM_PROCESS_INFO As UInteger = 0
    Public Const SERVICE_WIN32 As UInteger = &H30
    Public Const SERVICE_STATE_ALL As UInteger = 3


#End Region

#Region "Enums"

    <Flags()> _
    Public Enum ServiceManagerRights As UInteger
        SC_MANAGER_CONNECT = 1
        SC_MANAGER_CREATE_SERVICE = 2
        SC_MANAGER_ENUMERATE_SERVICE = 4
        SC_MANAGER_LOCK = 8
        SC_MANAGER_MODIFY_BOOT_CONFIG = &H20
        SC_MANAGER_QUERY_LOCK_STATUS = &H10
        SC_MANAGER_ALL_ACCESS = &HF003F
        GENERIC_READ = WinApi.GENERIC_READ
        GENERIC_WRITE = WinApi.GENERIC_WRITE
        GENERIC_EXECUTE = WinApi.GENERIC_EXECUTE
        GENERIC_ALL = WinApi.GENERIC_ALL
        READ_CONTROL = WinApi.READ_CONTROL
        STANDARD_RIGHTS_REQUIRED = &HF0000
        DELETE = &H10000
        WRITE_DAC = &H40000
        WRITE_OWNER = &H80000
        SYNCHRONIZE = &H100000
    End Enum

    <Flags()> _
    Public Enum ServiceRights As UInteger
        SERVICE_QUERY_CONFIG = 1
        SERVICE_CHANGE_CONFIG = 2
        SERVICE_QUERY_STATUS = 4
        SERVICE_ENUMERATE_DEPENDENTS = 8
        SERVICE_START = &H10
        SERVICE_STOP = &H20
        SERVICE_INTERROGATE = &H80
        SERVICE_USER_DEFINED_CONTROL = &H100
        SERVICE_PAUSE_CONTINUE = &H40
        SERVICE_ALL_ACCESS = &HF01FF
        GENERIC_READ = WinApi.GENERIC_READ
        GENERIC_WRITE = WinApi.GENERIC_WRITE
        GENERIC_EXECUTE = WinApi.GENERIC_EXECUTE
        GENERIC_ALL = WinApi.GENERIC_ALL
        READ_CONTROL = WinApi.READ_CONTROL
        ACCESS_SYSTEM_SECURITY = &H1000000
        DELETE = &H10000
        WRITE_DAC = &H40000
        WRITE_OWNER = &H80000
        SYNCHRONIZE = &H100000
    End Enum

    Public Enum DsGetDcNameFlags As Integer
        DS_FORCE_REDISCOVERY = &H1
        DS_DIRECTORY_SERVICE_REQUIRED = &H10
        DS_DIRECTORY_SERVICE_PREFERRED = &H20
        DS_GC_SERVER_REQUIRED = &H40
        DS_PDC_REQUIRED = &H80
        DS_BACKGROUND_ONLY = &H100
        DS_IP_REQUIRED = &H200
        DS_KDC_REQUIRED = &H400
        DS_TIMESERV_REQUIRED = &H800
        DS_WRITABLE_REQUIRED = &H1000
        DS_GOOD_TIMESERV_PREFERRED = &H2000
        DS_AVOID_SELF = &H4000
        DS_ONLY_LDAP_NEEDED = &H8000
        DS_IS_FLAT_NAME = &H10000
        DS_IS_DNS_NAME = &H20000
        DS_TRY_NEXTCLOSEST_SITE = &H40000
        DS_DIRECTORY_SERVICE_6_REQUIRED = &H80000
        DS_WEB_SERVICE_REQUIRED = &H100000
        DS_RETURN_DNS_NAME = &H40000000
        DS_RETURN_FLAT_NAME = &H80000000
    End Enum

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

#Region "Structs & Classes"

    <StructLayout(LayoutKind.Sequential)> _
    Public Class QUERY_SERVICE_CONFIG
        Public dwServiceType As UInteger
        Public dwStartType As UInteger
        Public dwErrorControl As UInteger
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpBinaryPathName As String
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpLoadOrderGroup As String
        Public dwTagId As UInteger
        Public lpDependencies As IntPtr
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpServiceStartName As String
        <MarshalAs(UnmanagedType.LPWStr)> _
        Public lpDisplayName As String
    End Class

    <StructLayout(LayoutKind.Sequential)> _
    Public Structure ENUM_SERVICE_STATUS_PROCESS
        <MarshalAs(UnmanagedType.LPTStr)> _
        Public lpServiceName As String
        <MarshalAs(UnmanagedType.LPTStr)> _
        Public lpDisplayName As String
        Public ServiceStatusProcess As SERVICE_STATUS_PROCESS
    End Structure

    <StructLayout(LayoutKind.Sequential)> _
    Public Structure SERVICE_STATUS_PROCESS
        Public dwServiceType As UInteger
        Public dwCurrentState As SERVICE_STATES
        Public dwControlsAccepted As UInteger
        Public dwWin32ExitCode As UInteger
        Public dwServiceSpecificExitCode As UInteger
        Public dwCheckPoint As UInteger
        Public dwWaitHint As UInteger
        Public dwProcessId As UInteger
        Public dwServiceFlags As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Unicode)> _
    Public Structure DomainControllerInfo
        <MarshalAs(UnmanagedType.LPWStr)>
        Public DomainControllerName As String
        <MarshalAs(UnmanagedType.LPWStr)>
        Public DomainControllerAddress As String
        Public DomainControllerAddressType As Integer
        Public DomainGuid As Guid
        <MarshalAs(UnmanagedType.LPWStr)>
        Public DomainName As String
        <MarshalAs(UnmanagedType.LPWStr)>
        Public DnsForestName As String
        Public Flags As Integer
        <MarshalAs(UnmanagedType.LPWStr)>
        Public DcSiteName As String
        <MarshalAs(UnmanagedType.LPWStr)>
        Public ClientSiteName As String
    End Structure

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

    <DllImport("advapi32.dll", EntryPoint:="QueryServiceStatusEx", SetLastError:=True)> _
    Public Shared Function QueryServiceStatusEx(<InAttribute()> ByVal hService As IntPtr, ByVal InfoLevel As UInteger,
                                                ByVal lpBuffer As IntPtr, ByVal cbBufSize As UInteger,
                                                <Out()> ByRef pcbBytesNeeded As UInteger) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="ControlService", SetLastError:=True)> _
    Public Shared Function ControlService(ByVal hService As IntPtr, ByVal dwControl As SERVICE_CONTROL_CODES, ByRef lpServiceStatus As SERVICE_STATUS_PROCESS) As Boolean
    End Function

    <DllImport("advapi32.dll", EntryPoint:="QueryServiceConfigW", SetLastError:=True)>
    Public Shared Function QueryServiceConfig(<InAttribute()> ByVal hService As IntPtr, <Out()> ByVal lpServiceConfig As IntPtr,
                                              ByVal cbBufSize As UInteger, <Out()> ByRef pcbBytesNeeded As UInteger) As <MarshalAs(UnmanagedType.Bool)> Boolean
    End Function

    <DllImport("shlwapi.dll", CharSet:=CharSet.Unicode)>
    Public Shared Function SHLoadIndirectString(<InAttribute()> ByVal pszSource As String, <MarshalAs(UnmanagedType.LPWStr), Out()> ByVal pszOutBuf As Text.StringBuilder,
                                                ByVal cchOutBuf As Integer, ByVal ppvReserved As IntPtr) As Integer
    End Function
       

    '<DllImport("kernel32.dll", EntryPoint:="FreeLibrary", SetLastError:=True)> _
    'Public Shared Function FreeLibrary(<InAttribute()> ByVal hLibModule As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
    'End Function

    '<DllImport("kernel32.dll", EntryPoint:="LoadLibraryW", SetLastError:=True)> _
    'Public Shared Function LoadLibrary(<InAttribute(), MarshalAs(UnmanagedType.LPWStr)> ByVal lpLibFileName As String) As IntPtr
    'End Function

    '<DllImport("user32.dll", EntryPoint:="LoadStringW", SetLastError:=True)> _
    'Public Shared Function LoadString(<InAttribute()> ByVal hInstance As IntPtr, ByVal uID As UInteger, <Out(), MarshalAs(UnmanagedType.LPWStr)> ByVal lpBuffer As Text.StringBuilder,
    '                                  ByVal cchBufferMax As Integer) As Integer
    'End Function

    <DllImport("Netapi32.dll", EntryPoint:="DsGetDcNameW", SetLastError:=True)> _
    Public Shared Function DsGetDcName(<MarshalAs(UnmanagedType.LPWStr), InAttribute()> ByVal computerName As String, <MarshalAs(UnmanagedType.LPTStr), InAttribute()> ByVal domainName As String,
                                       <InAttribute()> ByVal domainGuid As IntPtr, <MarshalAs(UnmanagedType.LPTStr), InAttribute()> ByVal siteName As String, <InAttribute()> ByVal flags As Integer,
                                       <Out()> ByRef domainControllerInfo As IntPtr) As UInteger
    End Function

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
