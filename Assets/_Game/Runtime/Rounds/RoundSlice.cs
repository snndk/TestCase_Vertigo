using System;

namespace Vertigo.Runtime._Game.Runtime.Rounds
{
    public abstract class RoundSlice
    {
        private bool m_Resolved;
        protected SliceContext Context { get; }

        protected RoundSlice(SliceContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void Resolve()
        {
            if (m_Resolved)
                throw new InvalidOperationException("A slice behavior can only be resolved once.");
            Context.EnsureWritable();
            m_Resolved = true;
            ResolveCore();
        }

        protected abstract void ResolveCore();
    }
}
