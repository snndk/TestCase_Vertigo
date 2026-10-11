using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class SpinResult
    {
        public OperationId OperationId { get; }
        public Round Round { get; }
        public int WinningIndex { get; }
        public RoundSlice WinningSlice => Round.Slices[WinningIndex];

        internal SpinResult(OperationId operationId, Round round, int winningIndex)
        {
            OperationId = operationId;
            Round = round;
            WinningIndex = winningIndex;
        }
    }
}
