using System;

namespace Vertigo.Presentation._Game.Presentation.GameScreen
{
    public class PresentationError
    {
        public string Message { get; }
        public Exception Cause { get; }

        public PresentationError(string message, Exception cause = null)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("An error needs a visible message.", nameof(message));

            Message = message;
            Cause = cause;
        }
    }
}
