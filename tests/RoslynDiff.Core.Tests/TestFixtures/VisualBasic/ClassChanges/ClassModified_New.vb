' Test fixture: Class modification detection
' Expected: Class "Person" should be detected as Modified
Namespace TestFixtures
    ''' <summary>
    ''' A person class for testing class modification.
    ''' </summary>
    Public Class Person
        Private _name As String
        Private _age As Integer

        ''' <summary>
        ''' Creates a new Person instance.
        ''' </summary>
        Public Sub New(name As String, age As Integer)
            _name = name
            _age = age
        End Sub

        ''' <summary>
        ''' Gets the person's name.
        ''' </summary>
        Public Function GetName() As String
            Return _name
        End Function

        ''' <summary>
        ''' Gets the person's age.
        ''' </summary>
        Public Function GetAge() As Integer
            Return _age
        End Function
    End Class
End Namespace
