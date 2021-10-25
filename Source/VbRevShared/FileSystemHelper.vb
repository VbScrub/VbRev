﻿Public Class FileSystemHelper

    Public Shared Function GetFileSizeString(ByVal SizeInBytes As Int64) As String
        Try
            Dim Count As Integer = 0
            Dim Suffix As String = String.Empty
            Dim Result As Double = SizeInBytes
            Dim ShowDecimal As Boolean = False
            Do Until Int(Result) < 1024
                Count += 1
                Result = Result / 1024
            Loop

            Select Case Count
                Case 0
                    Suffix = " B"
                Case 1 'KiloBytes
                    Suffix = " KB"
                Case 2 'MegaBytes
                    ShowDecimal = True
                    Suffix = " MB"
                Case 3 'GigaBytes
                    ShowDecimal = True
                    Suffix = " GB"
                Case 4 'TeraBytes
                    ShowDecimal = True
                    Suffix = " TB"
                Case Else
                    Return SizeInBytes & " B"
            End Select

            If ShowDecimal Then
                Return Result.ToString("N") & Suffix
            Else
                Return CInt(Result).ToString & Suffix
            End If
        Catch ex As Exception
            Log.WriteEntry("Error getting file size string for " & SizeInBytes & " : " & ex.Message, True)
            Return SizeInBytes & " B"
        End Try
    End Function

End Class
