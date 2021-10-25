<Serializable()>
Public Class NetworkInterfaceItem

    Public Property Name As String
    Public Property IpAddressesV4 As String = String.Empty
    Public Property IpAddressesV6 As String = String.Empty
    Public Property DefaultGateway As String
    Public Property DnsAddresses As String = String.Empty
    Public Property MacAddress As String
    Public Property Type As String

End Class
