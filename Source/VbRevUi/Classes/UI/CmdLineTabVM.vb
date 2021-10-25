Imports System.ComponentModel

Public Class CmdLineTabVM : Implements INotifyPropertyChanged, IEquatable(Of CmdLineTabVM)


    Public Event DataReceived(Data As String)
    Public Event Closed(Status As String)

    Public Property Data As CmdLineClient
    Public Property Id As Guid

    Private _Title As String = String.Empty
    Public Property Title As String
        Get
            Return _Title
        End Get
        Set(value As String)
            _Title = value
            OnPropertyChanged("Title")
        End Set
    End Property

    Public ReadOnly Property Icon As ImageSource
        Get
            If Data.IsRunning Then
                Return New BitmapImage(New Uri("\..\Images\window_16.png", UriKind.Relative))
            Else
                Return New BitmapImage(New Uri("\..\Images\window_close_16.png", UriKind.Relative))
            End If
        End Get
    End Property


    Public Sub New(Client As CmdLineClient, TabTitle As String)
        Me.Id = Guid.NewGuid
        Me.Data = Client
        Me.Title = TabTitle
        AddHandler Data.DataReceived, AddressOf CmdClient_DataReceived
        AddHandler Data.Closed, AddressOf CmdCLient_Closed
    End Sub

    Private Sub CmdClient_DataReceived(Data As String)
        Try
            If Application.Current.Dispatcher.CheckAccess() Then
                RaiseEvent DataReceived(Data)
            Else
                Application.Current.Dispatcher.Invoke(New Action(Of String)(AddressOf CmdClient_DataReceived), Data)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Sub OnPropertyChanged(Name As String)
        Try
            If Application.Current.Dispatcher.CheckAccess() Then
                RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(Name))
            Else
                Application.Current.Dispatcher.Invoke(New Action(Of String)(AddressOf OnPropertyChanged), Name)
            End If
        Catch ex As Exception
            Log.WriteEntry("Error in OnPropertyChanegd: " & ex.Message, False)
        End Try
    End Sub


    Private Sub CmdCLient_Closed(Status As String)
        Try
            If Application.Current.Dispatcher.CheckAccess() Then
                OnPropertyChanged("Icon")
                RaiseEvent Closed(Status)
            Else
                Application.Current.Dispatcher.Invoke(New Action(Of String)(AddressOf CmdCLient_Closed), Status)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged



    Public Function Equals1(other As CmdLineTabVM) As Boolean Implements System.IEquatable(Of CmdLineTabVM).Equals
        If other Is Nothing Then
            Return False
        End If
        Return other.Id = Me.Id
    End Function


End Class
