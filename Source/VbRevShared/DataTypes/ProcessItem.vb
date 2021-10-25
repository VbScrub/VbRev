<Serializable()>
Public Class ProcessItem : Implements IComparable(Of ProcessItem)


    Public Property FileName As String
    Public Property FileLocation As String
    Public Property CommandLine As String
    Public Property PID As Integer
    Public Property RunningAsUser As String
    Public Property SessionId As Integer

    Public Sub New()
    End Sub

    Public Sub New(FileName As String)
        Me.FileName = FileName
    End Sub

    Public Function CompareTo(other As ProcessItem) As Integer Implements System.IComparable(Of ProcessItem).CompareTo
        Return String.Compare(Me.FileName, other.FileName)
    End Function
End Class
