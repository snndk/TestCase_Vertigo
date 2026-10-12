using System;

namespace Vertigo.Presentation._Game.Presentation.ViewContracts
{
    public class VisualResult
    {
        public static VisualResult Completed { get; } = new VisualResult(VisualStatus.Completed, null);
        public static VisualResult Cancelled { get; } = new VisualResult(VisualStatus.Cancelled, null);

        public VisualStatus Status { get; }
        public Exception Error { get; }

        private VisualResult(VisualStatus status, Exception error)
        {
            Status = status;
            Error = error;
        }

        public static VisualResult Failed(Exception error)
        {
            if (error == null)
                throw new ArgumentNullException(nameof(error));

            return new VisualResult(VisualStatus.Failed, error);
        }
    }
}
