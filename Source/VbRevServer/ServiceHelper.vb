Imports Microsoft.Win32
Imports System.Runtime.InteropServices

Public Class ServiceHelper

    Public Shared Function GetServices(FromRegistry As Boolean) As List(Of ServiceItem)
        If FromRegistry Then
            Return GetServicesFromRegistry()
        Else
            Return GetServicesFromScm()
        End If
    End Function

    Private Shared Function GetServicesFromRegistry() As List(Of ServiceItem)
        Log.WriteEntry("Getting services from registry", False)
        Dim Services As New List(Of ServiceItem)
        Using RootReg As RegistryKey = RegistryKey.OpenBaseKey(Microsoft.Win32.RegistryHive.LocalMachine, Microsoft.Win32.RegistryView.Registry64)
            Using ServicesKey As RegistryKey = RootReg.OpenSubKey("System\CurrentControlSet\Services")
                Dim ServiceNames() As String = ServicesKey.GetSubKeyNames
                Log.WriteEntry("Found " & ServiceNames.Length & " service subkeys in registry", True)
                For Each SubKeyName As String In ServiceNames
                    Try
                        Log.WriteEntry("Inspecting " & SubKeyName, True)
                        Using ServiceKey As RegistryKey = ServicesKey.OpenSubKey(SubKeyName)
                            Dim ServiceType As ServiceItem.ServiceType = DirectCast(ServiceKey.GetValue("Type", 0), ServiceItem.ServiceType)
                            If Not ServiceType = ServiceItem.ServiceType.Unknown AndAlso ((ServiceType And ServiceItem.ServiceType.Win32OwnProcess) = ServiceItem.ServiceType.Win32OwnProcess OrElse
                                    (ServiceType And ServiceItem.ServiceType.Win32SharedProcess) = ServiceItem.ServiceType.Win32SharedProcess OrElse
                                    (ServiceType And ServiceItem.ServiceType.UserOwnprocess) = ServiceItem.ServiceType.UserOwnprocess OrElse
                                    (ServiceType And ServiceItem.ServiceType.UserSharedProcess) = ServiceItem.ServiceType.UserSharedProcess) Then
                                Log.WriteEntry("Found valid service type", True)
                                Dim Service As New ServiceItem
                                Service.Name = SubKeyName
                                Service.DisplayName = CStr(ServiceKey.GetValue("DisplayName", String.Empty))
                                Try
                                    If Service.DisplayName.StartsWith("@"c) Then
                                        Dim ResourceString As String = FileHelper.GetResourceString(Service.DisplayName)
                                        If Not String.IsNullOrEmpty(ResourceString) Then
                                            Service.DisplayName = ResourceString
                                        End If
                                    End If
                                Catch ex As Exception
                                    Log.WriteEntry("Error getting value for resource string " & Service.DisplayName & " : " & ex.Message, False)
                                End Try
                                If String.IsNullOrWhiteSpace(Service.DisplayName) Then
                                    Service.DisplayName = Service.Name
                                End If
                                Service.BinPath = CStr(ServiceKey.GetValue("ImagePath", String.Empty))
                                Service.RunningAs = CStr(ServiceKey.GetValue("ObjectName", String.Empty))
                                Service.StartupType = DirectCast(ServiceKey.GetValue("Start", 0), ServiceItem.ServiceStartupModes)
                                Service.Type = DirectCast(ServiceKey.GetValue("Type", 0), ServiceItem.ServiceType)
                                Dim ScmHandle As IntPtr = IntPtr.Zero
                                Dim SvcHandle As IntPtr = IntPtr.Zero
                                Try
                                    ScmHandle = GetServiceControlManagerHandle(WinApi.ServiceManagerRights.SC_MANAGER_CONNECT)
                                    SvcHandle = WinApi.OpenService(ScmHandle, Service.Name, WinApi.ServiceRights.SERVICE_QUERY_STATUS)
                                    Service.CurrentState = ServiceItem.GetFriendlyNameForState(GetServiceState(SvcHandle))
                                Catch ex As Exception
                                    Log.WriteEntry("Error querying current state for service " & Service.Name & " : " & ex.Message, False)
                                Finally
                                    If Not SvcHandle = IntPtr.Zero Then
                                        WinApi.CloseServiceHandle(SvcHandle)
                                    End If
                                    If Not ScmHandle = IntPtr.Zero Then
                                        WinApi.CloseServiceHandle(ScmHandle)
                                    End If
                                End Try
                                Services.Add(Service)
                            End If
                        End Using
                    Catch ex As Exception
                        Log.WriteEntry("Error getting service details from registry key " & SubKeyName & " : " & ex.Message, False)
                    End Try
                Next
            End Using
        End Using
        Return Services
    End Function

    Private Shared Function GetServiceState(ServiceHandle As IntPtr) As WinApi.SERVICE_STATES
        Dim BytesNeeded As UInteger = 0
        WinApi.QueryServiceStatusEx(ServiceHandle, 0, Nothing, 0, BytesNeeded)
        Dim BufferPtr As IntPtr = Marshal.AllocHGlobal(CInt(BytesNeeded))
        Try
            If Not WinApi.QueryServiceStatusEx(ServiceHandle, 0, BufferPtr, BytesNeeded, 0) Then
                Throw New System.ComponentModel.Win32Exception()
            End If
            Dim ServiceStatusDetails As WinApi.SERVICE_STATUS_PROCESS
            ServiceStatusDetails = DirectCast(Marshal.PtrToStructure(BufferPtr, GetType(WinApi.SERVICE_STATUS_PROCESS)), WinApi.SERVICE_STATUS_PROCESS)
            Return ServiceStatusDetails.dwCurrentState
        Finally
            If Not BufferPtr = IntPtr.Zero Then
                Marshal.FreeHGlobal(BufferPtr)
            End If
        End Try
    End Function

    Private Shared Function GetServicesFromScm() As List(Of ServiceItem)
        Log.WriteEntry("Getting services from SCM API", False)
        Dim Services As New List(Of ServiceItem)
        Dim ScmHandle As IntPtr = IntPtr.Zero
        Dim ServicesPtr As IntPtr = IntPtr.Zero
        Try
            ScmHandle = GetServiceControlManagerHandle(WinApi.ServiceManagerRights.SC_MANAGER_ENUMERATE_SERVICE)
            Dim BytesNeeded As UInteger = 0
            WinApi.EnumServicesStatusEx(ScmHandle, WinApi.SC_ENUM_PROCESS_INFO, WinApi.SERVICE_WIN32, WinApi.SERVICE_STATE_ALL, IntPtr.Zero, 0, BytesNeeded, 0, Nothing, Nothing)
            ServicesPtr = Marshal.AllocHGlobal(CInt(BytesNeeded))
            Dim ServicesFound As UInteger = 0
            WinApi.EnumServicesStatusEx(ScmHandle, WinApi.SC_ENUM_PROCESS_INFO, WinApi.SERVICE_WIN32, WinApi.SERVICE_STATE_ALL, ServicesPtr, BytesNeeded, 0, ServicesFound, Nothing, Nothing)
            Log.WriteEntry(ServicesFound.ToString & " services found", False)
            Dim CurrentAddress As Int64 = ServicesPtr.ToInt64
            Dim ServiceStructureSize As Integer = Marshal.SizeOf(GetType(WinApi.ENUM_SERVICE_STATUS_PROCESS))
            For i As Integer = 0 To CInt(ServicesFound - 1)
                Dim CurrentService As WinApi.ENUM_SERVICE_STATUS_PROCESS = DirectCast(Marshal.PtrToStructure(New IntPtr(CurrentAddress), GetType(WinApi.ENUM_SERVICE_STATUS_PROCESS)), WinApi.ENUM_SERVICE_STATUS_PROCESS)
                If String.IsNullOrEmpty(CurrentService.lpServiceName) Then
                    Log.WriteEntry("Skipping service as service name as empty", True)
                    Continue For
                End If
                Dim Service As New ServiceItem
                Service.Name = CurrentService.lpServiceName
                Log.WriteEntry("Querying service " & Service.Name, True)
                Service.DisplayName = CurrentService.lpDisplayName
                Service.CurrentState = ServiceItem.GetFriendlyNameForState(CurrentService.ServiceStatusProcess.dwCurrentState)
                Dim CurrentServiceHandle As IntPtr = IntPtr.Zero
                Try
                    CurrentServiceHandle = WinApi.OpenService(ScmHandle, Service.Name, WinApi.ServiceRights.SERVICE_QUERY_CONFIG)
                    If CurrentServiceHandle = IntPtr.Zero Then
                        Throw New System.ComponentModel.Win32Exception()
                    End If
                    Dim CurrentServiceConfig As New WinApi.QUERY_SERVICE_CONFIG
                    Dim RequiredBytes As UInteger
                    WinApi.QueryServiceConfig(CurrentServiceHandle, Nothing, 0, RequiredBytes)
                    Dim ConfigPointer As IntPtr = Marshal.AllocHGlobal(CInt(RequiredBytes))
                    Try
                        If Not WinApi.QueryServiceConfig(CurrentServiceHandle, ConfigPointer, RequiredBytes, RequiredBytes) Then
                            Throw New System.ComponentModel.Win32Exception()
                        End If
                        Marshal.PtrToStructure(ConfigPointer, CurrentServiceConfig)
                        Service.StartupType = CType(CurrentServiceConfig.dwStartType, ServiceItem.ServiceStartupModes)
                        Service.RunningAs = CurrentServiceConfig.lpServiceStartName
                        Service.BinPath = CurrentServiceConfig.lpBinaryPathName
                    Finally
                        Marshal.FreeHGlobal(ConfigPointer)
                    End Try
                Catch ex As Exception
                    Log.WriteEntry("Error querying service " & Service.Name & " : " & ex.Message, False)
                Finally
                    If Not CurrentServiceHandle = IntPtr.Zero Then
                        WinApi.CloseServiceHandle(CurrentServiceHandle)
                    End If
                End Try
                Services.Add(Service)
                CurrentAddress += ServiceStructureSize
            Next
        Finally
            If Not ServicesPtr = IntPtr.Zero Then
                Marshal.FreeHGlobal(ServicesPtr)
            End If
            If Not ScmHandle = IntPtr.Zero Then
                WinApi.CloseServiceHandle(ScmHandle)
            End If
        End Try
        Return Services
    End Function

    Private Shared Function GetServiceControlManagerHandle(ByVal Access As UInteger) As IntPtr
        Dim ScmPtr As IntPtr = WinApi.OpenSCManager(Nothing, Nothing, Access)
        If ScmPtr = IntPtr.Zero Then
            Throw New ApplicationException("Unable to connect to service manager. The last error that was reported was: " & New System.ComponentModel.Win32Exception().Message)
        End If
        Return ScmPtr
    End Function

    Public Shared Sub StartService(ServiceName As String)
        Dim ScmHandle As IntPtr
        Dim ServiceHandle As IntPtr
        Try
            ScmHandle = GetServiceControlManagerHandle(WinApi.ServiceManagerRights.SC_MANAGER_CONNECT)
            ServiceHandle = WinApi.OpenService(ScmHandle, ServiceName, WinApi.ServiceRights.SERVICE_START)
            If ServiceHandle = IntPtr.Zero Then
                Throw New ComponentModel.Win32Exception
            End If
            If Not WinApi.StartService(ServiceHandle, 0, Nothing) Then
                Throw New ComponentModel.Win32Exception
            End If
        Finally
            If Not ServiceHandle = IntPtr.Zero Then
                WinApi.CloseServiceHandle(ServiceHandle)
            End If
            If Not ScmHandle = IntPtr.Zero Then
                WinApi.CloseServiceHandle(ScmHandle)
            End If
        End Try

    End Sub

    Public Shared Sub StopService(ServiceName As String)
        Dim ScmHandle As IntPtr
        Dim ServiceHandle As IntPtr
        Try
            ScmHandle = GetServiceControlManagerHandle(WinApi.ServiceManagerRights.SC_MANAGER_CONNECT)
            ServiceHandle = WinApi.OpenService(ScmHandle, ServiceName, WinApi.ServiceRights.SERVICE_STOP)
            If ServiceHandle = IntPtr.Zero Then
                Throw New ComponentModel.Win32Exception
            End If
            If Not WinApi.ControlService(ServiceHandle, WinApi.SERVICE_CONTROL_CODES.SERVICE_CONTROL_STOP, New WinApi.SERVICE_STATUS_PROCESS) Then
                Throw New ComponentModel.Win32Exception
            End If
        Finally
            If Not ServiceHandle = IntPtr.Zero Then
                WinApi.CloseServiceHandle(ServiceHandle)
            End If
            If Not ScmHandle = IntPtr.Zero Then
                WinApi.CloseServiceHandle(ScmHandle)
            End If
        End Try
    End Sub


End Class
