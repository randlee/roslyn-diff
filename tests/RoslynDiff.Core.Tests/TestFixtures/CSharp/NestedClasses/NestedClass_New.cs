// Test fixture: Nested class modification detection
// Expected: Nested class "Builder" should be detected as Modified
//           Method "WithTimeout" added to nested class
namespace TestFixtures;

/// <summary>
/// A container class with a nested builder pattern for testing nested class changes.
/// </summary>
public class ConnectionOptions
{
    public string Host { get; init; } = "localhost";
    public int Port { get; init; } = 8080;
    public int Timeout { get; init; } = 30;

    /// <summary>
    /// Builder class for creating ConnectionOptions instances.
    /// </summary>
    public class Builder
    {
        private string _host = "localhost";
        private int _port = 8080;
        private int _timeout = 30;

        /// <summary>
        /// Sets the host.
        /// </summary>
        public Builder WithHost(string host)
        {
            _host = host;
            return this;
        }

        /// <summary>
        /// Sets the port.
        /// </summary>
        public Builder WithPort(int port)
        {
            _port = port;
            return this;
        }

        /// <summary>
        /// Sets the timeout in seconds.
        /// </summary>
        public Builder WithTimeout(int timeout)
        {
            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Builds the ConnectionOptions instance.
        /// </summary>
        public ConnectionOptions Build()
        {
            return new ConnectionOptions
            {
                Host = _host,
                Port = _port,
                Timeout = _timeout
            };
        }
    }
}
