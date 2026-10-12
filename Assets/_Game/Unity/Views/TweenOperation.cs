using System;
using DG.Tweening;
using Vertigo.Presentation._Game.Presentation.ViewContracts;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class TweenOperation
    {
        private Tween m_Tween;
        private Action<VisualResult> m_Completed;
        private float m_TailStart;
        public bool IsPlaying => m_Tween != null;

        public void Play(Tween tween, Action<VisualResult> completed)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));
            if (completed == null)
                throw new ArgumentNullException(nameof(completed));
            if (m_Tween != null)
                throw new InvalidOperationException("A visual operation is already playing.");
            m_Tween = tween;
            m_Completed = completed;
            tween.SetUpdate(true);
            tween.OnComplete(() => Finish(tween, VisualResult.Completed));
            tween.OnKill(() => Finish(tween, VisualResult.Cancelled));
        }

        public void Cancel()
        {
            var tween = m_Tween;
            if (tween == null)
                return;
            tween.Kill(false);
            Finish(tween, VisualResult.Cancelled);
        }

        public void Accelerate(float speed, float normalSpeedTail = 0f)
        {
            var tween = m_Tween;
            if (tween == null)
                return;
            var tailStart = tween.Duration(false) - normalSpeedTail;
            if (tween.Elapsed(false) >= tailStart)
                return;

            ClearTailSpeedCallback();
            tween.timeScale = speed;
            if (normalSpeedTail > 0f)
                PreserveNormalSpeedTail(tailStart);
        }

        private void PreserveNormalSpeedTail(float tailStart)
        {
            m_TailStart = tailStart;
            m_Tween.onUpdate += RestoreTailSpeed;
        }

        private void RestoreTailSpeed()
        {
            if (m_Tween == null || m_Tween.Elapsed(false) < m_TailStart)
                return;
            m_Tween.timeScale = 1f;
            ClearTailSpeedCallback();
        }

        private void ClearTailSpeedCallback()
        {
            if (m_Tween != null)
                m_Tween.onUpdate -= RestoreTailSpeed;
            m_TailStart = 0f;
        }

        private void Finish(Tween tween, VisualResult result)
        {
            if (!ReferenceEquals(m_Tween, tween))
                return;
            var completed = m_Completed;
            ClearTailSpeedCallback();
            m_Tween = null;
            m_Completed = null;
            completed(result);
        }
    }
}
