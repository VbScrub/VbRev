<Serializable()>
Public Class ListenerItem : Implements IComparable(Of ListenerItem)

    Public Property IpAddress As String
    Public Property Port As Integer
    Public Property PID As Integer
    Public Property ProcessName As String

    Public Function CompareTo(other As ListenerItem) As Integer Implements IComparable(Of ListenerItem).CompareTo
        Return Me.Port.CompareTo(other.Port)
    End Function
End Class
