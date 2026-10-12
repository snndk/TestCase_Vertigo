using System;
using DG.Tweening;
using Vertigo.Presentation._Game.Presentation.ViewContracts;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class TweenOperation
    {
        private Tween m_Tween;
        private Action<VisualResult> m_Completed;
        public bool IsPlaying => m_Tween != null;

        public void Play(Tween tween, Action<VisualResult> completed)
        {
            if (tween == null)
                throw new ArgumentNullException(nameof(tween));
            if (m_Tween != null)
                throw new InvalidOperationException("A visual operation is already playing.");
            m_Tween = tween;
            m_Completed = completed ?? throw new ArgumentNullException(nameof(completed));
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

            tween.timeScale = speed;
            if (normalSpeedTail <= 0f)
                return;

            tween.onUpdate += RestoreTailSpeed;
            void RestoreTailSpeed()
            {
                if (tween.Elapsed(false) < tailStart)
                    return;
                tween.timeScale = 1f;
                tween.onUpdate -= RestoreTailSpeed;
            }
        }

        private void Finish(Tween tween, VisualResult result)
        {
            if (!ReferenceEquals(m_Tween, tween))
                return;
            var completed = m_Completed;
            m_Tween = null;
            m_Completed = null;
            completed(result);
        }
    }
}
