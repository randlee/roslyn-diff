' Test fixture: Property change detection
' Expected: Property "Description" should be detected as Added, "Price" as Modified
Namespace TestFixtures
    ''' <summary>
    ''' A product class for testing property changes.
    ''' </summary>
    Public Class Product
        Private _name As String
        Private _price As Decimal
        Private _description As String

        ''' <summary>
        ''' Gets or sets the product name.
        ''' </summary>
        Public Property Name As String
            Get
                Return _name
            End Get
            Set(value As String)
                _name = value
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the product price with validation.
        ''' </summary>
        Public Property Price As Decimal
            Get
                Return _price
            End Get
            Set(value As Decimal)
                If value < 0 Then
                    Throw New ArgumentException("Price cannot be negative")
                End If
                _price = value
            End Set
        End Property

        ''' <summary>
        ''' Gets or sets the product description.
        ''' </summary>
        Public Property Description As String
            Get
                Return _description
            End Get
            Set(value As String)
                _description = value
            End Set
        End Property
    End Class
End Namespace
