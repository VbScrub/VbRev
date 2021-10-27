Public Class UserSettings

    Private Const StartListenerOnLaunchSettingName As String = "StartListenerOnLaunch"
    Private Const DefaultListenerPortSettingName As String = "DefaultListenerPort"
    Private Const GetFileIconsSettingName As String = "GetFileIcons"
    Private Const FileDownloadLocationSettingName As String = "FileDownloadLocation"
    Private Const NetworkReadTimeoutSecondsSettingName As String = "NetworkReadTimeoutSeconds"
    Private Const OpenFilesAfterDownloadSettingName As String = "OpenFilesAfterDownload"


    Public Shared Property ConfigFilePath As String = IO.Path.Combine(My.Application.Info.DirectoryPath, "VbRev.ini")

    Public Shared Property StartListenerOnLaunch As Boolean
    Public Shared Property DefaultListenerPort As Integer
    Public Shared Property GetFileIcons As Boolean
    Public Shared Property FileDownloadLocation As String = String.Empty
    Public Shared Property NetworkReadTimeoutSeconds As Integer
    Public Shared Property OpenFilesAfterDownload As Boolean
      
    Public Shared Sub SaveSettings()
        Using SettingsFile As New IO.StreamWriter(ConfigFilePath, False, System.Text.Encoding.UTF8)
            SettingsFile.WriteLine("# VbRev UI User Preferences")
            SettingsFile.WriteLine(StartListenerOnLaunchSettingName & "=" & StartListenerOnLaunch.ToString)
            SettingsFile.WriteLine(DefaultListenerPortSettingName & "=" & DefaultListenerPort.ToString)
            SettingsFile.WriteLine(GetFileIconsSettingName & "=" & GetFileIcons.ToString)
            SettingsFile.WriteLine(FileDownloadLocationSettingName & "=" & FileDownloadLocation)
            SettingsFile.WriteLine(NetworkReadTimeoutSecondsSettingName & "=" & NetworkReadTimeoutSeconds)
            SettingsFile.WriteLine(OpenFilesAfterDownloadSettingName & "=" & OpenFilesAfterDownload)
        End Using
    End Sub


    Public Shared Sub LoadSettings()
        ' Default settings
        StartListenerOnLaunch = False
        DefaultListenerPort = 4444
        GetFileIcons = True
        NetworkReadTimeoutSeconds = 30
        OpenFilesAfterDownload = False
        ' Get user settings from file
        If IO.File.Exists(ConfigFilePath) Then
            For Each Line As String In IO.File.ReadAllLines(ConfigFilePath)
                Try
                    If Not String.IsNullOrWhiteSpace(Line) AndAlso Not Line.StartsWith("#") AndAlso Line.Contains("="c) AndAlso Line.Length > Line.IndexOf("="c) Then
                        Dim SettingName As String = Line.Remove(Line.IndexOf("="c))
                        If String.Compare(SettingName, StartListenerOnLaunchSettingName, True) = 0 Then
                            StartListenerOnLaunch = Convert.ToBoolean(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, DefaultListenerPortSettingName, True) = 0 Then
                            DefaultListenerPort = Convert.ToInt32(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, GetFileIconsSettingName, True) = 0 Then
                            GetFileIcons = Convert.ToBoolean(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, FileDownloadLocationSettingName, True) = 0 Then
                            FileDownloadLocation = Line.Substring(Line.IndexOf("="c) + 1)
                        ElseIf String.Compare(SettingName, NetworkReadTimeoutSecondsSettingName, True) = 0 Then
                            NetworkReadTimeoutSeconds = Convert.ToInt32(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, OpenFilesAfterDownloadSettingName, True) = 0 Then
                            OpenFilesAfterDownload = Convert.ToBoolean(Line.Substring(Line.IndexOf("="c) + 1))
                        End If
                    End If
                Catch ex As Exception
                    Log.WriteEntry("Error loading user preference: " & Line & " : " & ex.Message, False)
                End Try
            Next
        End If

    End Sub

End Class
