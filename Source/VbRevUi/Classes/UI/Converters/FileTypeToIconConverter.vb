Public Class FileTypeToIconConverter : Implements IValueConverter


    Public Function Convert(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.Convert
        Dim TypeValue As FileSystemItem.FileItemType = DirectCast(value, FileSystemItem.FileItemType)
        Select Case TypeValue
            Case FileSystemItem.FileItemType.Directory
                Return New BitmapImage(New Uri("\..\Images\folder_16.png", UriKind.Relative))
            Case FileSystemItem.FileItemType.Drive
                Return New BitmapImage(New Uri("\..\Images\harddrive_16.png", UriKind.Relative))
            Case FileSystemItem.FileItemType.File
                Return New BitmapImage(New Uri("\..\Images\document_16.png", UriKind.Relative))
            Case Else
                Return New BitmapImage(New Uri("\..\Images\help_16.png", UriKind.Relative))
        End Select
    End Function

    Public Function ConvertBack(value As Object, targetType As System.Type, parameter As Object, culture As System.Globalization.CultureInfo) As Object Implements System.Windows.Data.IValueConverter.ConvertBack
        Return Nothing
    End Function
End Class