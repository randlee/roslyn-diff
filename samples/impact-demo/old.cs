namespace Samples.ImpactDemo;

/// <summary>
/// Payment service for processing transactions.
/// </summary>
public class PaymentService
{
    private string _merchantId;
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
        _merchantId = merchantId;
    }

    /// <summary>
    /// Processes a payment transaction.
    /// </summary>
    /// <param name="amount">The payment amount.</param>
    /// <param name="currency">The currency code.</param>
    /// <returns>The transaction result.</returns>
    public PaymentResult ProcessPayment(decimal amount, string currency)
    {
        ValidateAmount(amount);
        _transactionCount++;

        var request = CreatePaymentRequest(amount, currency);
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

    internal void UpdateMerchantSettings(string merchantId)
    {
        _merchantId = merchantId;
        LogSettingsUpdate();
    }

    private void ValidateAmount(decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));
    }

    private PaymentRequest CreatePaymentRequest(decimal amount, string currency)
    {
        return new PaymentRequest
        {
            Amount = amount,
            Currency = currency,
            MerchantId = _merchantId,
            Timestamp = DateTime.UtcNow
        };
    }

    private void LogSettingsUpdate()
    {
        Console.WriteLine($"Settings updated for merchant: {_merchantId}");
    }
}

internal class InternalHelper
{
    internal static string FormatCurrency(decimal amount, string code)
    {
        return $"{code} {amount:F2}";
    }

    internal static bool ValidateCurrencyCode(string code)
    {
        return code?.Length == 3;
    }
}
