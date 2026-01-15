' Test fixture: Module addition detection
' Expected: Module "StringHelper" should be detected as Added
Namespace TestFixtures
    ''' <summary>
    ''' A simple math helper module.
    ''' </summary>
    Public Module MathHelper
        Public Function Add(a As Integer, b As Integer) As Integer
            Return a + b
        End Function
    End Module

    ''' <summary>
    ''' A string helper module that was added in this version.
    ''' </summary>
    Public Module StringHelper
        Public Function Reverse(input As String) As String
            Return New String(input.Reverse().ToArray())
        End Function
    End Module
End Namespace
