using System;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public abstract class SliceDefinition
    {
        // Identifies reusable content. A position in a round is identified by its index.
        public string Id { get; }

        protected SliceDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A slice ID is required.", nameof(id));

            Id = id;
        }

        public RoundSlice CreateSlice(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone));

            return CreateSliceCore(zone)
                ?? throw new InvalidOperationException("A definition must produce a slice.");
        }

        protected abstract RoundSlice CreateSliceCore(int zone);
    }
}
