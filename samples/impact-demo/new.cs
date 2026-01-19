namespace Samples.ImpactDemo;

/// <summary>
/// Payment service for processing transactions.
/// Enhanced with comprehensive logging and validation.
/// </summary>
public class PaymentService
{
    // Renamed private field (non-breaking, but with reflection caveat)
    private string _merchantIdentifier;
    private readonly IPaymentGateway _gateway;
    private int _transactionCount;

    /// <summary>
    /// Gets or sets the payment timeout in seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets the current transaction count.
    /// </summary>
    public int TransactionCount => _transactionCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentService"/> class.
    /// </summary>
    /// <param name="gateway">The payment gateway.</param>
    /// <param name="merchantId">The merchant identifier.</param>
    public PaymentService(IPaymentGateway gateway, string merchantId)
    {
        _gateway = gateway;
        _merchantIdentifier = merchantId;
    }

    /// <summary>
    /// Processes a payment transaction with optional description.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="description">Optional payment description.</param>
    /// <returns>The transaction result.</returns>
    public PaymentResult ProcessPayment(decimal amount, string currency, string description = "")
    {
        ValidateAmount(amount);
        _transactionCount++;

        // Added comment for clarity
        var request = CreatePaymentRequest(amount, currency, description);
        return _gateway.Process(request);
    }

    /// <summary>
    /// Refunds a payment transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID to refund.</param>
    /// <returns>The refund result.</returns>
    public RefundResult RefundPayment(string transactionId)
    {
        return _gateway.Refund(transactionId);
    }

    /// <summary>
    /// Gets transaction details.
    /// </summary>
    internal TransactionDetails GetTransactionDetails(string transactionId)
    {
        return _gateway.GetDetails(transactionId);
    }

    // Breaking: Renamed internal method
    internal void ConfigureMerchantSettings(string merchantId)
    {
        _merchantIdentifier = merchantId;
        LogSettingsUpdate();
    }

    // Parameter renamed (non-breaking, but with named argument caveat)
    private void ValidateAmount(decimal paymentAmount)
    {
        if (paymentAmount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(paymentAmount));
    }

    // Added parameter, updated to match ProcessPayment signature change
    private PaymentRequest CreatePaymentRequest(decimal amount, string currency, string description)
    {
        return new PaymentRequest
        {
            Amount = amount,
            Currency = currency,
            MerchantId = _merchantIdentifier,
            Timestamp = DateTime.UtcNow,
            Description = description
        };
    }

    private void LogSettingsUpdate()
    {
        // Formatting change: added blank line for readability

        Console.WriteLine($"Settings updated for merchant: {_merchantIdentifier}");
    }
}

internal class InternalHelper
{
    // Breaking internal: renamed method
    internal static string FormatCurrencyDisplay(decimal amount, string code)
    {
        return $"{code} {amount:F2}";
    }

    internal static bool ValidateCurrencyCode(string code)
    {
        return code?.Length == 3;
    }
}
