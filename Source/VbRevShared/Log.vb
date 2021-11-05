Public Class Log

    Public Shared Property Enabled As Boolean
    Public Shared Property VerboseEnabled As Boolean

    <DebuggerStepThrough()>
    Public Shared Sub WriteEntry(Message As String, VerboseOnly As Boolean)
        'Debug.WriteLine(System.Threading.Thread.CurrentThread.Name & " - " & Message)
        If Enabled Then
            If VerboseOnly AndAlso Not VerboseEnabled Then
                Exit Sub
            End If
            Console.WriteLine(System.Threading.Thread.CurrentThread.Name & " - " & Message)
        End If
    End Sub

End Class
