Imports System.Runtime.InteropServices
Imports System.Security.Principal

Public Class UserHelper

    Public Shared Function GetSessionId() As Integer
        Using Proc As Process = Process.GetCurrentProcess
            Return Proc.SessionId
        End Using
    End Function


    Public Shared Function GetGroups() As List(Of GroupSidItem)
        Try
            Using CurrentUser As WindowsIdentity = WindowsIdentity.GetCurrent
                Dim Token As IntPtr = CurrentUser.Token
                Dim RequiredSize As UInteger
                WinApi.GetTokenInformation(Token, WinApi.TOKEN_INFORMATION_CLASS.TokenGroups, Nothing, 0, RequiredSize)
                Dim SidStructSize As Integer = Marshal.SizeOf(GetType(WinApi.SID_AND_ATTRIBUTES))
                Dim GroupsOffset As Long = CLng(Marshal.OffsetOf(GetType(WinApi.TOKEN_GROUPS), "Groups")) 'accounts for varying amount of padding in struct depending on CPU architecture
                Dim GroupList As New List(Of GroupSidItem)
                Dim BufferPtr As IntPtr = IntPtr.Zero
                Try
                    BufferPtr = Marshal.AllocHGlobal(CInt(RequiredSize))
                    If WinApi.GetTokenInformation(Token, WinApi.TOKEN_INFORMATION_CLASS.TokenGroups, BufferPtr, RequiredSize, RequiredSize) Then
                        Dim GroupCount As Integer = Marshal.ReadInt32(BufferPtr)
                        For i As Integer = 0 To GroupCount - 1
                            Dim CurrentSidInfo As WinApi.SID_AND_ATTRIBUTES = WinApi.PtrToStructCast(Of WinApi.SID_AND_ATTRIBUTES)(New IntPtr(BufferPtr.ToInt64 + GroupsOffset + (i * SidStructSize)))
                            If Not (CurrentSidInfo.Attributes And WinApi.SidGroupAttributes.SE_GROUP_LOGON_ID) = WinApi.SidGroupAttributes.SE_GROUP_LOGON_ID Then
                                Dim Sid As New SecurityIdentifier(CurrentSidInfo.Sid)
                                Dim NewGroup As New GroupSidItem(Sid.ToString)
                                Try
                                    NewGroup.Name = WinApi.GetAccountFromSid(Sid).DomainAndUsername
                                Catch ex As Exception
                                    NewGroup.Name = Sid.ToString
                                    Log.WriteEntry("Error translating SID to name (SID " & Sid.ToString & ") : " & ex.Message, False)
                                End Try
                                NewGroup.Description = GetDescriptionFromAttributes(CurrentSidInfo.Attributes)
                                GroupList.Add(NewGroup)
                            End If
                        Next
                        GroupList.Sort()
                        Return GroupList
                    Else
                        Throw New ComponentModel.Win32Exception
                    End If
                Finally
                    If Not BufferPtr = IntPtr.Zero Then
                        Marshal.FreeHGlobal(BufferPtr)
                    End If
                End Try
            End Using
        Catch ex As Exception
            Throw New ApplicationException("Error getting groups from user token: " & ex.Message)
        End Try
    End Function

    Private Shared Function GetDescriptionFromAttributes(Attributes As WinApi.SidGroupAttributes) As String
        Dim Description As New List(Of String)
        If Attributes.HasFlag(WinApi.SidGroupAttributes.SE_GROUP_ENABLED) Then
            Description.Add("Enabled")
        End If
        If Attributes.HasFlag(WinApi.SidGroupAttributes.SE_GROUP_USE_FOR_DENY_ONLY) Then
            Description.Add("Deny only")
        End If
        If Attributes.HasFlag(WinApi.SidGroupAttributes.SE_GROUP_MANDATORY) Then
            Description.Add("Mandatory")
        End If
        Return String.Join(", ", Description)
    End Function

End Class
