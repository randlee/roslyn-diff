' Test fixture: Property change detection
' Expected: Property "Description" should be detected as Added, "Price" as Modified
Namespace TestFixtures
    ''' <summary>
    ''' A product class for testing property changes.
    ''' </summary>
    Public Class Product
        Private _name As String
        Private _price As Decimal

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
        ''' Gets or sets the product price.
        ''' </summary>
        Public Property Price As Decimal
            Get
                Return _price
            End Get
            Set(value As Decimal)
                _price = value
            End Set
        End Property
    End Class
End Namespace
