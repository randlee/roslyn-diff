' Test fixture: Sub addition detection
' Expected: Sub "PrintGoodbye" should be detected as Added
Namespace TestFixtures
    ''' <summary>
    ''' A greeter module for testing Sub addition.
    ''' </summary>
    Public Module Greeter
        ''' <summary>
        ''' Prints a greeting message.
        ''' </summary>
        Public Sub PrintHello(name As String)
            Console.WriteLine("Hello, " & name & "!")
        End Sub

        ''' <summary>
        ''' Prints a farewell message.
        ''' </summary>
        Public Sub PrintGoodbye(name As String)
            Console.WriteLine("Goodbye, " & name & "!")
        End Sub
    End Module
End Namespace
