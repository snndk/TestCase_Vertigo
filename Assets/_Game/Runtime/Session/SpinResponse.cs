using System;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class SpinResponse
    {
        public CommandStatus Status { get; }
        public SpinResult Result { get; }

        private SpinResponse(CommandStatus status, SpinResult result)
        {
            Status = status;
            Result = result;
        }

        internal static SpinResponse Accept(SpinResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            return new SpinResponse(CommandStatus.Accepted, result);
        }

        internal static SpinResponse Reject(CommandStatus status)
        {
            if (status != CommandStatus.NotReady)
                throw new ArgumentOutOfRangeException(nameof(status));

            return new SpinResponse(status, null);
        }
    }
}
