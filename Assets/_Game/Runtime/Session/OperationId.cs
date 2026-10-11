using System;

namespace Vertigo.Runtime._Game.Runtime.Session
{
    public readonly struct OperationId : IEquatable<OperationId>
    {
        public Guid RunId { get; }
        public long Sequence { get; }

        public OperationId(Guid runId, long sequence)
        {
            if (runId == Guid.Empty)
                throw new ArgumentException("A run ID is required.", nameof(runId));
            if (sequence <= 0)
                throw new ArgumentOutOfRangeException(nameof(sequence));

            RunId = runId;
            Sequence = sequence;
        }

        public bool Equals(OperationId other)
        {
            return RunId == other.RunId && Sequence == other.Sequence;
        }

        public override bool Equals(object obj)
        {
            return obj is OperationId other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (RunId.GetHashCode() * 397) ^ Sequence.GetHashCode();
            }
        }
    }
}
