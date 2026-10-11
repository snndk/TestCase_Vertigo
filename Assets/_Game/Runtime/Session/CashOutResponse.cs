using System;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class CashOutResponse
    {
        public CommandStatus Status { get; }
        public CashOutResult Result { get; }

        private CashOutResponse(CommandStatus status, CashOutResult result)
        {
            Status = status;
            Result = result;
        }

        internal static CashOutResponse Accept(CashOutResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            return new CashOutResponse(CommandStatus.Accepted, result);
        }

        internal static CashOutResponse Reject(CommandStatus status)
        {
            if (status != CommandStatus.NotReady && status != CommandStatus.UnsafeZone &&
                status != CommandStatus.SaveFailed)
                throw new ArgumentOutOfRangeException(nameof(status));

            return new CashOutResponse(status, null);
        }
    }
}
