Imports Microsoft.Win32

Public Class IconHelper


    Private Shared IconCache As New Dictionary(Of String, BitmapSource) From {{".exe", New BitmapImage(New Uri("\..\Images\window_16.png", UriKind.Relative))},
                                                                             {".lnk", New BitmapImage(New Uri("\..\Images\summary_next_16.png", UriKind.Relative))}}

    Private Shared _DefaultIcon As New BitmapImage(New Uri("\..\Images\Icons8\document_16px.png", UriKind.Relative))

    Public Shared Function GetFileExtensionIcon(Extension As String) As BitmapSource
        If Not UserSettings.GetFileIcons Then
            Return _DefaultIcon
        End If
        If Not Extension.StartsWith(".") Then
            Extension = "." & Extension
        End If
        If IconCache.ContainsKey(Extension) Then
            Return IconCache(Extension)
        End If
        Try
            Using Root As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.ClassesRoot, RegistryView.Default)
                Using ExtensionSubKey = Root.OpenSubKey(Extension)
                    Dim ClassKeyName As String = CStr(ExtensionSubKey.GetValue(String.Empty))
                    Dim ClassKey = Root.OpenSubKey(ClassKeyName & "\DefaultIcon")
                    Try
                        If ClassKey Is Nothing Then
                            Using CurVerKey = Root.OpenSubKey(ClassKeyName & "\CurVer")
                                ClassKeyName = CStr(CurVerKey.GetValue(String.Empty))
                                ClassKey = Root.OpenSubKey(ClassKeyName & "\DefaultIcon")
                            End Using
                        End If

                        Dim IconResource As String = CStr(ClassKey.GetValue(String.Empty))
                        Dim PathParts() As String = IconResource.Split(","c)
                        Dim IconIndex As Integer = 0
                        Dim IconFilePath As String = IconResource
                        If PathParts.Length > 1 Then
                            IconFilePath = PathParts(0)
                            IconIndex = CInt(PathParts(1))
                        End If
                        If IconFilePath.StartsWith("""") Then
                            IconFilePath = IconFilePath.Remove(0, 1)
                        End If
                        If IconFilePath.EndsWith("""") Then
                            IconFilePath = IconFilePath.Remove(IconFilePath.Length - 1)
                        End If

                        Dim LargeIconhandle As IntPtr
                        Dim SmallIconHandle As IntPtr
                        WinApi.ExtractIconEx(IconFilePath, IconIndex, LargeIconhandle, SmallIconHandle, 1)
                        Dim Bitmap As BitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(SmallIconHandle, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions)
                        IconCache.Add(Extension, Bitmap)
                        WinApi.DestroyIcon(SmallIconHandle)
                        WinApi.DestroyIcon(LargeIconhandle)
                        Return Bitmap
                    Finally
                        If Not ClassKey Is Nothing Then
                            ClassKey.Dispose()
                        End If
                    End Try
                End Using
            End Using
        Catch ex As Exception
            Log.WriteEntry("Error getting icon for extension " & Extension & " : " & ex.Message, True)
        End Try
        IconCache.Add(Extension, _DefaultIcon)
        Return IconCache(Extension)
    End Function


End Class
