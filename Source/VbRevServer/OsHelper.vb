Imports VbRevShared.WinApi
Imports System.Runtime.InteropServices

Public Class OsHelper

    Public Shared Function GetOsInfo() As OsInfoResponseMessage
        Dim OsInfo As New OsInfoResponseMessage
        OsInfo.Is64Bit = Environment.Is64BitOperatingSystem
        Try
            OsInfo.MachineNetBiosDomainName = OsHelper.GetComputerDomainName
            Try
                OsInfo.MachineDnsDomainName = GetFqdnFromNetbiosDomainName(OsInfo.MachineNetBiosDomainName)
            Catch ex As Exception
                Log.WriteEntry("Error getting DNS domain name from NetBIOS name: " & ex.Message, False)
            End Try
        Catch ex As Exception
            OsInfo.MachineNetBiosDomainName = "Error: " & ex.Message
        End Try
        Try
            Dim OsVersion = GetOsVersion()
            OsInfo.OsName = OsVersion.Item1
            OsInfo.OsVersion = OsVersion.Item2
        Catch ex As Exception
            OsInfo.OsName = "Error: " & ex.Message
        End Try
        Return OsInfo
    End Function

    Private Shared Function GetOsVersion() As Tuple(Of String, String)
        Dim Name As String = String.Empty
        Dim VersionNumber As String = String.Empty
        Using HKLM = Microsoft.Win32.RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Default)
            Using SubKey = HKLM.OpenSubKey("Software\Microsoft\Windows NT\CurrentVersion")
                Name = CStr(SubKey.GetValue("ProductName", String.Empty))
                Dim ReleaseId As String = CStr(SubKey.GetValue("ReleaseId", String.Empty))
                If ReleaseId = String.Empty Then
                    VersionNumber = CStr(SubKey.GetValue("CurrentVersion", String.Empty))
                Else
                    VersionNumber = ReleaseId
                End If
                Return New Tuple(Of String, String)(Name, VersionNumber)
            End Using
        End Using
    End Function

    Private Shared Function GetComputerDomainName() As String
        Dim NamePtr As IntPtr
        Dim DomainInfo As NETSETUP_JOIN_STATUS
        Dim Result As Integer = NetGetJoinInformation(Nothing, NamePtr, DomainInfo)
        If Not Result = 0 Then
            Throw New ComponentModel.Win32Exception
        End If
        Dim NetbiosDomaiName As String = Marshal.PtrToStringAuto(NamePtr)
        NetApiBufferFree(NamePtr)
        Return NetbiosDomaiName
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


    Public Shared Function GetFqdnFromNetbiosDomainName(ByVal NetbiosDomain As String) As String
        Log.WriteEntry("Getting DNS domain name for " & NetbiosDomain, False)
        Dim DcInfoPtr As New IntPtr

        Dim Flags As WinApi.DsGetDcNameFlags = WinApi.DsGetDcNameFlags.DS_DIRECTORY_SERVICE_PREFERRED Or WinApi.DsGetDcNameFlags.DS_IS_FLAT_NAME Or WinApi.DsGetDcNameFlags.DS_RETURN_DNS_NAME
        Try
            Dim Result As UInteger = WinApi.DsGetDcName(Nothing, NetbiosDomain, Nothing, Nothing, Flags, DcInfoPtr)
            If Result = 0 Then
                Dim DcInfo As WinApi.DomainControllerInfo = WinApi.PtrToStructCast(Of WinApi.DomainControllerInfo)(DcInfoPtr)
                Log.WriteEntry("DC: " & DcInfo.DomainControllerName, False)
                Log.WriteEntry("Domain: " & DcInfo.DomainName, False)
                Return DcInfo.DomainName
            End If
            Throw New System.ComponentModel.Win32Exception(CInt(Result))
        Finally
            If Not DcInfoPtr = IntPtr.Zero Then
                WinApi.NetApiBufferFree(DcInfoPtr)
            End If
        End Try
    End Function

End Class
