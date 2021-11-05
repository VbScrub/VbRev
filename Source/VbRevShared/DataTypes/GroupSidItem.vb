Public Class GroupSidItem : Implements IComparable(Of GroupSidItem)

    Public Property Sid As String
    Public Property Name As String = String.Empty
    Public Property Description As String = String.Empty

    Public Sub New(SidString As String)
        Me.Sid = SidString
    End Sub

    Public Function CompareTo(other As GroupSidItem) As Integer Implements IComparable(Of GroupSidItem).CompareTo
        Return String.Compare(Me.Name, other.Name, True)
    End Function
End Class
