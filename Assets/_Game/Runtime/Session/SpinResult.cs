using Vertigo.Runtime._Game.Runtime.Rounds;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public class SpinResult
    {
        public OperationId OperationId { get; }
        public Round Round { get; }
        public int WinningIndex { get; }
        public bool IsAutomatic { get; }
        public SliceData WinningSlice => Round.Slices[WinningIndex];

        internal SpinResult(OperationId operationId, Round round, int winningIndex, bool isAutomatic = false)
        {
            OperationId = operationId;
            Round = round;
            WinningIndex = winningIndex;
            IsAutomatic = isAutomatic;
        }
    }
}
