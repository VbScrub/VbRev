Imports System.ComponentModel

Public Class FileSystemItemVM : Implements INotifyPropertyChanged


    Public Property Data As FileSystemItem

    Private _Extension As String = String.Empty

    Private _RenameBoxVisibility As Visibility = Visibility.Collapsed
    Public Property RenameBoxVisibility As Visibility
        Get
            Return _RenameBoxVisibility
        End Get
        Set(value As Visibility)
            _RenameBoxVisibility = value
            OnPropertyChanged("RenameBoxVisibility")
        End Set
    End Property

    Private _NameLblVisibility As Visibility = Visibility.Visible
    Public Property NameLblVisibility As Visibility
        Get
            Return _NameLblVisibility
        End Get
        Set(value As Visibility)
            _NameLblVisibility = value
            OnPropertyChanged("NameLblVisibility")
        End Set
    End Property

    Private _RenameBoxText As String = String.Empty
    Public Property RenameBoxText As String
        Get
            Return _RenameBoxText
        End Get
        Set(value As String)
            _RenameBoxText = value
            OnPropertyChanged("RenameBoxText")
        End Set
    End Property

    Public ReadOnly Property Name As String
        Get
            Return Data.Name
        End Get
    End Property

    Public ReadOnly Property ModifiedDate As String
        Get
            If Me.Data.Type = FileSystemItem.FileItemType.File Then
                Return Data.ModifiedDate.ToShortDateString & " " & Data.ModifiedDate.ToShortTimeString
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public ReadOnly Property CreatedDate As String
        Get
            If Me.Data.Type = FileSystemItem.FileItemType.File Then
                Return Data.CreatedDate.ToShortDateString & " " & Data.CreatedDate.ToShortTimeString
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public ReadOnly Property Size As String
        Get
            If Me.Data.Type = FileSystemItem.FileItemType.File Then
                Return FileHelper.GetFileSizeString(Data.Size)
            Else
                Return String.Empty
            End If
        End Get
    End Property

    Public ReadOnly Property Icon As ImageSource
        Get
            Select Case Data.Type
                Case FileSystemItem.FileItemType.Directory
                    Return New BitmapImage(New Uri("\..\Images\Icons8\folder_16px.png", UriKind.Relative))
                Case FileSystemItem.FileItemType.Drive
                    Return New BitmapImage(New Uri("\..\Images\Icons8\hdd_16px.png", UriKind.Relative))
                Case FileSystemItem.FileItemType.File
                    If Not String.IsNullOrEmpty(_Extension) Then
                        Try
                            Return IconHelper.GetFileExtensionIcon(_Extension)
                        Catch ex As Exception
                            Log.WriteEntry("Error getting icon for extension " & _Extension & " : " & ex.Message, True)
                        End Try
                    End If
            End Select
            Return New BitmapImage(New Uri("\..\Images\Icons8\document_16px.png", UriKind.Relative))
        End Get
    End Property

    Public Sub New()
    End Sub

    Public Sub New(File As FileSystemItem)
        Me.Data = File
        Try
            If File.Type = FileSystemItem.FileItemType.File Then
                _Extension = IO.Path.GetExtension(File.Name)
            End If
        Catch ex As Exception
            Debug.WriteLine("Error getting file extension: " & ex.Message)
        End Try
    End Sub

  
    Public Sub OnPropertyChanged(Name As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(Name))
    End Sub



    Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements System.ComponentModel.INotifyPropertyChanged.PropertyChanged
End Class
