Public Class TabPage : Inherits UserControl

    Public Event SendingServerRequest(Message As String)
    Public Event SendingServerRequestFinished(Message As String, SwitchToTab As MainWindow.Tabs?)

    Protected Sub RaiseSendingServerRequestEvent(Message As String)
        RaiseEvent SendingServerRequest(Message)
    End Sub

    Protected Sub RaiseSendingServerRequestFinishedEvent(Message As String, SwitchToTab As MainWindow.Tabs?)
        RaiseEvent SendingServerRequestFinished(Message, SwitchToTab)
    End Sub

    Protected Sub RaiseSendingServerRequestFinishedEvent(Message As String)
        RaiseSendingServerRequestFinishedEvent(Message, Nothing)
    End Sub

End Class
