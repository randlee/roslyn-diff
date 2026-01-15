' Test fixture: Identical file detection
' Expected: No differences should be detected between Old and New
Namespace TestFixtures
    ''' <summary>
    ''' A simple calculator module for testing identical file comparison.
    ''' </summary>
    Public Module Calculator
        ''' <summary>
        ''' Adds two integers together.
        ''' </summary>
        Public Function Add(a As Integer, b As Integer) As Integer
            Return a + b
        End Function

        ''' <summary>
        ''' Subtracts the second integer from the first.
        ''' </summary>
        Public Function Subtract(a As Integer, b As Integer) As Integer
            Return a - b
        End Function
    End Module
End Namespace
