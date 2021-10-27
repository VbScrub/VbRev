
Public Class ProcessMemoryReader : Implements IDisposable

    Private _TargetProcess As Process = Nothing
    Private _TargetProcessHandle As IntPtr = IntPtr.Zero

    Public Sub New(ByVal ProcessToRead As Process)
        If ProcessToRead Is Nothing Then
            Throw New ArgumentNullException("ProcessToRead")
        End If
        _TargetProcess = ProcessToRead
        Me.Open()
    End Sub

    Public Function Read(ByVal MemoryAddress As IntPtr, ByVal Count As Integer) As Byte()
        If _TargetProcessHandle = IntPtr.Zero Then
            Open()
        End If
        Dim Bytes(Count - 1) As Byte
        Dim Result As Boolean = WinApi.ReadProcessMemory(_TargetProcessHandle, MemoryAddress, Bytes, CUInt(Count), 0)
        If Result Then
            Return Bytes
        Else
            Return Nothing
        End If
    End Function

    Public Sub Open()
        If _TargetProcess Is Nothing Then
            Throw New ApplicationException("Process not found")
        End If
        If _TargetProcessHandle = IntPtr.Zero Then
            _TargetProcessHandle = WinApi.OpenProcess(WinApi.ProcessAccess.VMRead Or WinApi.ProcessAccess.QueryInformation, True, CUInt(_TargetProcess.Id))
            If _TargetProcessHandle = IntPtr.Zero Then
                Throw New ApplicationException("Unable to open process for memory reading. The last error reported was: " & New System.ComponentModel.Win32Exception().Message)
            End If
        Else
            Throw New ApplicationException("A handle to the process has already been obtained, " & _
                                           "close the existing handle by calling the Close method before calling Open again")
        End If
    End Sub

    Public Sub Close()
        If Not _TargetProcessHandle = IntPtr.Zero Then
            Dim Result As Boolean = WinApi.CloseHandle(_TargetProcessHandle)
            If Not Result Then
                Throw New ApplicationException("Unable to close process handle. The last error reported was: " & _
                                               New System.ComponentModel.Win32Exception().Message)
            End If
            _TargetProcessHandle = IntPtr.Zero
        End If
    End Sub



#Region "IDisposable Support"

    Private disposedValue As Boolean

    Protected Overridable Sub Dispose(ByVal disposing As Boolean)
        If Not Me.disposedValue Then
            If Not _TargetProcessHandle = IntPtr.Zero Then
                Try
                    WinApi.CloseHandle(_TargetProcessHandle)
                Catch ex As Exception
                    Debug.WriteLine("Error closing handle - " & ex.Message)
                End Try
            End If
        End If
        Me.disposedValue = True
    End Sub

    Protected Overrides Sub Finalize()
        Dispose(False)
        MyBase.Finalize()
    End Sub

    ''' <summary>
    ''' Releases resources and closes any process handles that are still open
    ''' </summary>
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

#End Region


End Class


