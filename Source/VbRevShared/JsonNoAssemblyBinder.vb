Imports Newtonsoft.Json.Serialization

'Without this we can't use ILMerge to merge the shared DLL into the client and server EXEs because JSON.NET will add the assembly name to each type
'and the assembly name will be different on the client and server so serialization fails

Public Class JsonNoAssemblyBinder : Implements ISerializationBinder

    Public Sub BindToName(serializedType As Type, ByRef assemblyName As String, ByRef typeName As String) Implements ISerializationBinder.BindToName
        assemblyName = Nothing
        typeName = serializedType.FullName
    End Sub

    Public Function BindToType(assemblyName As String, typeName As String) As Type Implements ISerializationBinder.BindToType
        Return Type.GetType(typeName)
    End Function
End Class
