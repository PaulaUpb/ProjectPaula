using System;

namespace ProjectPaula.Util
{
    /// <summary>
    /// A utility class that not only throws exceptions on the server
    /// but is also used to report the error messages back to the client.
    /// </summary>
    public class ErrorReporter : IDisposable
    {
        private readonly Action<string> _errorMessageSetter;
        private bool _hasThrown = false;

        public ErrorReporter(Action<string> errorMessageSetter)
        {
            _errorMessageSetter = errorMessageSetter;
        }

        public void Dispose()
        {
            if (!_hasThrown)
            {
                // No exception has been thrown
                // => Clear error message
                _errorMessageSetter(null);
            }
        }

        public void SetMessage(string message)
        {
            _errorMessageSetter(message);
            _hasThrown = true;
        }

        public void Throw(string message) => Throw(new ReportedException(message));

        public void Throw(Exception exception) => Throw(exception, exception.Message);

        public void Throw(Exception exception, string displayMessage)
        {
            _errorMessageSetter(displayMessage);
            _hasThrown = true;
            throw exception;
        }
    }

    public class ReportedException : Exception
    {
        public ReportedException(string message) : base(message) { }
        public ReportedException(string message, Exception innerException) : base(message, innerException) { }
    }
}
