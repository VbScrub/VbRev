Imports VbRevShared.WinApi
Imports System.Net.Sockets
Imports System.Runtime.InteropServices

Public Class NetworkingHelper

    Public Shared Function GetNICs() As List(Of NetworkInterfaceItem)
        Dim NicList As New List(Of NetworkInterfaceItem)
        For Each NIC In Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces
            Try
                If NIC.NetworkInterfaceType = Net.NetworkInformation.NetworkInterfaceType.Ethernet OrElse NIC.NetworkInterfaceType = Net.NetworkInformation.NetworkInterfaceType.Wireless80211 OrElse NIC.NetworkInterfaceType = Net.NetworkInformation.NetworkInterfaceType.Loopback Then
                    Dim NicItem As New NetworkInterfaceItem
                    NicItem.Name = NIC.Name
                    NicItem.Type = NIC.NetworkInterfaceType.ToString
                    NicItem.MacAddress = NIC.GetPhysicalAddress.ToString
                    Dim IpProperties = NIC.GetIPProperties()
                    Dim v4Count As Integer = 0
                    Dim v6Count As Integer = 0
                    For i As Integer = 0 To IpProperties.UnicastAddresses.Count - 1
                        If IpProperties.UnicastAddresses(i).Address.AddressFamily = Net.Sockets.AddressFamily.InterNetwork Then
                            Dim IpString As String = IpProperties.UnicastAddresses(i).Address.ToString
                            If Not String.IsNullOrWhiteSpace(IpString) Then
                                v4Count += 1
                                If v4Count = 1 Then
                                    NicItem.IpAddressesV4 &= IpString
                                Else
                                    NicItem.IpAddressesV4 &= ", " & IpString
                                End If
                            End If
                        ElseIf IpProperties.UnicastAddresses(i).Address.AddressFamily = Net.Sockets.AddressFamily.InterNetworkV6 Then
                            Dim IpString As String = IpProperties.UnicastAddresses(i).Address.ToString
                            If Not String.IsNullOrWhiteSpace(IpString) Then
                                v6Count += 1
                                If v6Count = 1 Then
                                    NicItem.IpAddressesV6 &= IpString
                                Else
                                    NicItem.IpAddressesV6 &= ", " & IpString
                                End If
                            End If
                        End If
                    Next
                    For i As Integer = 0 To IpProperties.DnsAddresses.Count - 1
                        If Not i = 0 Then
                            NicItem.DnsAddresses &= ", "
                        End If
                        NicItem.DnsAddresses &= IpProperties.DnsAddresses(i).ToString
                    Next
                    For i As Integer = 0 To IpProperties.GatewayAddresses.Count - 1
                        If Not i = 0 Then
                            NicItem.DefaultGateway &= ", "
                        End If
                        NicItem.DefaultGateway &= IpProperties.GatewayAddresses(i).Address.ToString
                    Next
                    NicList.Add(NicItem)
                End If
            Catch ex As Exception
                Log.WriteEntry("Error getting NIC properties: " & ex.Message, False)
            End Try
        Next
        Return NicList
    End Function

    Public Shared Function GetTcpListeners() As List(Of ListenerItem)
        Dim BufferLength As UInteger = 0
        Dim Result As UInteger = GetExtendedTcpTable(IntPtr.Zero, BufferLength, True, CUInt(AddressFamily.InterNetwork), TCP_TABLE_CLASS.TcpTableOwnerPidListener, 0)
        Dim ResultsList As New List(Of ListenerItem)
        If Result = ErrorInsufficientBuffer Then
            Dim BufferPtr As IntPtr = Marshal.AllocHGlobal(CInt(BufferLength))
            Try
                Result = GetExtendedTcpTable(BufferPtr, BufferLength, True, CUInt(AddressFamily.InterNetwork), TCP_TABLE_CLASS.TcpTableOwnerPidListener, 0)
                If Result = 0 Then
                    Dim TcpTable As MibTcpTableOwnerPid = PtrToStructCast(Of MibTcpTableOwnerPid)(BufferPtr)
                    If TcpTable.NumberOfEntries > 0 Then
                        Dim CurrentPtr As New IntPtr(CLng(BufferPtr) + Marshal.SizeOf(TcpTable.NumberOfEntries))
                        For i As Integer = 0 To TcpTable.NumberOfEntries - 1
                            Dim TcpRow As MibTcpRow = PtrToStructCast(Of MibTcpRow)(CurrentPtr)
                            Dim Listener As New ListenerItem
                            Dim Ip As New Net.IPAddress(TcpRow.localAddr)
                            If Ip.Equals(Net.IPAddress.Any) Then
                                Listener.IpAddress = "All"
                            Else
                                Listener.IpAddress = Ip.ToString
                            End If
                            Listener.Port = BitConverter.ToUInt16(New Byte() {TcpRow.localPort2, TcpRow.localPort1}, 0)
                            Listener.PID = CInt(TcpRow.owningPid)
                            Try
                                Using Proc As Process = Process.GetProcessById(Listener.PID)
                                    'Extension won't always be .exe but couldn't find any 100% reliable way to get the file extension if
                                    'we are running as low priv account and it looks weird without any extension
                                    Listener.ProcessName = Proc.ProcessName & ".exe"
                                End Using
                            Catch ex As Exception
                                Log.WriteEntry("Error getting process name from PID " & Listener.PID & " : " & ex.Message, False)
                            End Try
                            ResultsList.Add(Listener)
                            CurrentPtr = New IntPtr(CLng(CurrentPtr) + Marshal.SizeOf(GetType(MibTcpRow)))
                        Next
                    End If
                End If
            Finally
                If Not BufferPtr = IntPtr.Zero Then
                    Marshal.FreeHGlobal(BufferPtr)
                End If
            End Try
        Else
            Throw New ComponentModel.Win32Exception
        End If
        ResultsList.Sort()
        Return ResultsList
    End Function


End Class
