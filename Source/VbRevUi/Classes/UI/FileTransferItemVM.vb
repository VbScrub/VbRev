Imports System.ComponentModel
Imports System.Threading

Public Class FileTransferItemVM : Implements INotifyPropertyChanged

    Public Property Data As FileTransferItem

    Public ReadOnly Property Name As String
        Get
            Return Data.FileName
        End Get
    End Property

    Private _ProgressDetails As String = "Waiting for other transfers to complete"
    Public Property ProgressDetails As String
        Get
            Return _ProgressDetails
        End Get
        Set(value As String)
            _ProgressDetails = value
            OnPropertyChanged("ProgressDetails")
        End Set
    End Property

    Public ReadOnly Property StatusText As String
        Get
            Select Case Me.Data.CurrentState
                Case FileTransferItem.ProgressState.Queued
                    Return "Queued"
                Case FileTransferItem.ProgressState.Transferring
                    Return "Transferring..."
                Case FileTransferItem.ProgressState.Failed
                    Return "Failed"
                Case FileTransferItem.ProgressState.Complete
                    Return "Complete"
                Case FileTransferItem.ProgressState.Cancelled
                    Return "Cancelled"
                Case Else
                    Return "Unknown"
            End Select
        End Get
    End Property

    Private _ProgressBarValue As Integer = 0
    Public Property ProgressBarValue As Integer
        Get
            Return _ProgressBarValue
        End Get
        Set(value As Integer)
            _ProgressBarValue = value
            OnPropertyChanged("ProgressBarValue")
        End Set
    End Property

    Private _FailBackgroundVisibility As Visibility = Visibility.Collapsed
    Public Property FailBackgroundVisibility As Visibility
        Get
            Return _FailBackgroundVisibility
        End Get
        Set(value As Visibility)
            _FailBackgroundVisibility = value
            OnPropertyChanged("FailBackgroundVisibility")
        End Set
    End Property

    Public Sub New(TransferData As FileTransferItem)
        Me.Data = TransferData
        AddHandler Me.Data.ProgressUpdated, AddressOf Data_ProgressUpdated
    End Sub

    Public Sub OnPropertyChanged(Name As String)
        Application.Current.Dispatcher.Invoke(New Action(Of String)(AddressOf UiOnPropertyChanged), Name)
    End Sub

    Private Sub UiOnPropertyChanged(Name As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(Name))
    End Sub

    Public Sub Data_ProgressUpdated(State As FileTransferItem.ProgressState, ProgressPercent As Integer, StateDetails As String)
        If State = FileTransferItem.ProgressState.Failed OrElse State = FileTransferItem.ProgressState.Cancelled Then
            FailBackgroundVisibility = Visibility.Visible
        End If
        OnPropertyChanged("StatusText")
        If Not String.IsNullOrEmpty(StateDetails) Then
            Me.ProgressDetails = StateDetails
        End If
        Me.ProgressBarValue = ProgressPercent
    End Sub

    Public Event PropertyChanged(sender As Object, e As System.ComponentModel.PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged


End Class
