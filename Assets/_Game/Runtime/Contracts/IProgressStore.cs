using Vertigo.Runtime._Game.Runtime.Rewards;

namespace Vertigo.Runtime._Game.Runtime.Contracts
{
    public interface IProgressStore
    {
        /// <summary>
        /// Returns a non-null immutable snapshot. A missing save may yield empty progress;
        /// corrupt or unreadable data must be reported, never silently replaced with empty progress.
        /// </summary>
        RewardCollection Load();

        /// <summary>
        /// Atomically replaces the entire snapshot. True means committed; false means an expected
        /// write failure with the previous snapshot intact. Unexpected exceptions propagate and
        /// must also leave the previous snapshot intact. Never report failure after committing.
        /// </summary>
        bool TrySave(RewardCollection progress);
    }
}
