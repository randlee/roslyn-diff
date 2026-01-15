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
End Namespace
