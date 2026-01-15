' Test fixture: Function removal detection
' Expected: Function "Divide" should be detected as Removed
Namespace TestFixtures
    ''' <summary>
    ''' A calculator module for testing Function removal.
    ''' </summary>
    Public Module Calculator
        ''' <summary>
        ''' Multiplies two numbers.
        ''' </summary>
        Public Function Multiply(a As Integer, b As Integer) As Integer
            Return a * b
        End Function

        ''' <summary>
        ''' Divides two numbers.
        ''' </summary>
        Public Function Divide(a As Double, b As Double) As Double
            If b = 0 Then
                Throw New DivideByZeroException()
            End If
            Return a / b
        End Function
    End Module
End Namespace
