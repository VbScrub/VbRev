Public Class UserSettings

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
            SettingsFile.WriteLine("StartListenerOnLaunch=" & StartListenerOnLaunch.ToString)
            SettingsFile.WriteLine("DefaultListenerPort=" & DefaultListenerPort.ToString)
            SettingsFile.WriteLine("GetFileIcons=" & GetFileIcons.ToString)
            SettingsFile.WriteLine("FileDownloadLocation=" & FileDownloadLocation)
            SettingsFile.WriteLine("NetworkReadTimeoutSeconds=" & NetworkReadTimeoutSeconds)
            SettingsFile.WriteLine("OpenFilesAfterDownload=" & OpenFilesAfterDownload)
        End Using
    End Sub


    Public Shared Sub LoadSettings()
        StartListenerOnLaunch = False
        DefaultListenerPort = 4444
        GetFileIcons = True
        NetworkReadTimeoutSeconds = 10
        OpenFilesAfterDownload = False
        If IO.File.Exists(ConfigFilePath) Then
            For Each Line As String In IO.File.ReadAllLines(ConfigFilePath)
                Try
                    If Not String.IsNullOrWhiteSpace(Line) AndAlso Not Line.StartsWith("#") AndAlso Line.Contains("="c) AndAlso Line.Length > Line.IndexOf("="c) Then
                        Dim SettingName As String = Line.Remove(Line.IndexOf("="c))
                        If String.Compare(SettingName, "StartListenerOnLaunch", True) = 0 Then
                            StartListenerOnLaunch = Convert.ToBoolean(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, "DefaultListenerPort", True) = 0 Then
                            DefaultListenerPort = Convert.ToInt32(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, "GetFileIcons", True) = 0 Then
                            GetFileIcons = Convert.ToBoolean(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, "FileDownloadLocation", True) = 0 Then
                            FileDownloadLocation = Line.Substring(Line.IndexOf("="c) + 1)
                        ElseIf String.Compare(SettingName, "NetworkReadTimeoutSeconds", True) = 0 Then
                            NetworkReadTimeoutSeconds = Convert.ToInt32(Line.Substring(Line.IndexOf("="c) + 1))
                        ElseIf String.Compare(SettingName, "OpenFilesAfterDownload", True) = 0 Then
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
