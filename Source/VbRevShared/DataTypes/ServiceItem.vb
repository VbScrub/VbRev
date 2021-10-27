<Serializable()>
Public Class ServiceItem : Implements IComparable(Of ServiceItem)


    <Flags()>
    Public Enum ServiceType As Integer
        Unknown = 0
        FileSystemDriver = 2
        KernelDriver = 1
        Win32OwnProcess = &H10
        Win32SharedProcess = &H20
        UserOwnprocess = &H50
        UserSharedProcess = &H60
        InteractiveProcess = &H100
    End Enum

    Public Enum ServiceStartupModes As Integer
        Unknown = 0
        Automatic = 2
        Manual = 3
        Disabled = 4
    End Enum

    Public Shared Function GetFriendlyNameForState(State As WinApi.SERVICE_STATES) As String
        Select Case State
            Case WinApi.SERVICE_STATES.SERVICE_CONTINUE_PENDING
                Return "Continue pending"
            Case WinApi.SERVICE_STATES.SERVICE_PAUSE_PENDING
                Return "Pause pending"
            Case WinApi.SERVICE_STATES.SERVICE_PAUSED
                Return "Paused"
            Case WinApi.SERVICE_STATES.SERVICE_RUNNING
                Return "Running"
            Case WinApi.SERVICE_STATES.SERVICE_START_PENDING
                Return "Start pending"
            Case WinApi.SERVICE_STATES.SERVICE_STOP_PENDING
                Return "Stop pending"
            Case WinApi.SERVICE_STATES.SERVICE_STOPPED
                Return "Stopped"
            Case Else
                Return "Unknown"
        End Select
    End Function

    Public Property Name As String
    Public Property DisplayName As String
    Public Property Type As ServiceType
    Public Property StartupType As ServiceStartupModes
    Public Property RunningAs As String
    Public Property BinPath As String
    Public Property CurrentState As String


    Public Function CompareTo(other As ServiceItem) As Integer Implements System.IComparable(Of ServiceItem).CompareTo
        Return String.Compare(Me.DisplayName, other.DisplayName)
    End Function
End Class
