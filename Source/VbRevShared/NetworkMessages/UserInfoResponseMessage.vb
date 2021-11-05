Public Class UserInfoResponseMessage : Inherits NetworkMessage

    Public Property Groups As List(Of GroupSidItem)
    Public Property Sid As String
    Public Property SessionId As Integer


    Public Sub New()
        Me.Type = MessageType.UserInfoResponse
    End Sub

    Public Sub New(UserGroups As List(Of GroupSidItem), UserSid As String, UserSessionId As Integer)
        Me.Type = MessageType.UserInfoResponse
        Me.Groups = UserGroups
        Me.Sid = UserSid
        Me.SessionId = UserSessionId
    End Sub



End Class
