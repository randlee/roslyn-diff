' Test fixture: Class modification detection
' Expected: Class "Person" should be detected as Modified
Namespace TestFixtures
    ''' <summary>
    ''' A person class for testing class modification.
    ''' </summary>
    Public Class Person
        Private _name As String

        ''' <summary>
        ''' Creates a new Person instance.
        ''' </summary>
        Public Sub New(name As String)
            _name = name
        End Sub

        ''' <summary>
        ''' Gets the person's name.
        ''' </summary>
        Public Function GetName() As String
            Return _name
        End Function
    End Class
End Namespace
