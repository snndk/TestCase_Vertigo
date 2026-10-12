using System;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public abstract class SliceDefinition
    {
        public string Id { get; }

        protected SliceDefinition(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A slice ID is required.", nameof(id));
            Id = id;
        }

        public SliceData CreateData(int zone)
        {
            if (zone < 1)
                throw new ArgumentOutOfRangeException(nameof(zone));
            return CreateDataCore(zone)
                ?? throw new InvalidOperationException("A definition must produce slice data.");
        }

        public RoundSlice CreateBehavior(SliceData data, SliceContext context)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            if (context == null)
                throw new ArgumentNullException(nameof(context));
            if (data.Id != Id)
                throw new ArgumentException("Slice data must match its definition.", nameof(data));
            return CreateBehaviorCore(data, context)
                ?? throw new InvalidOperationException("A definition must produce a slice behavior.");
        }

        protected abstract SliceData CreateDataCore(int zone);
        protected abstract RoundSlice CreateBehaviorCore(SliceData data, SliceContext context);
    }
}
