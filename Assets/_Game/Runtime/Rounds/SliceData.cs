using System;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public class SliceData
    {
        public string Id { get; }

        public SliceData(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A slice ID is required.", nameof(id));
            Id = id;
        }
    }
}
