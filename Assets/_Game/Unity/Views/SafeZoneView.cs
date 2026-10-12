using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Rules;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class SafeZoneView : MonoBehaviour
    {
        [SerializeField] private TMP_Text kind;
        [SerializeField] private TMP_Text zone;
        [SerializeField] private TMP_Text start;
        [SerializeField] private TMP_Text remaining;
        [SerializeField] private Image icon;
        [SerializeField] private Sprite silverIcon;
        [SerializeField] private Sprite goldIcon;
        [SerializeField] private RectTransform track;
        [SerializeField] private Image fill;
        [SerializeField] private RectTransform tip;
        [SerializeField] private Image tipImage;
        [SerializeField] private Image glow;
        [SerializeField] private Image[] particles;
        private int m_Target;
        private int m_Zone;
        private float m_Progress;
        private float m_TargetProgress;
        private Sequence m_Transition;

        public void Show(SafeZoneProgress state)
        {
            var current = state.CurrentZone;
            if (m_Zone == current)
                return;
            var target = state.TargetZone;
            var gold = state.Tier == WheelTier.Gold;
            var color = gold ? new Color(1, .74f, .18f) : new Color(.38f, .8f, 1);
            var animate = m_Target == target && current == m_Zone + 1;
            m_Target = target;
            m_Zone = current;
            kind.text = gold ? "GOLDEN SPIN" : "SAFE SPIN";
            kind.color = gold ? new Color(1, .78f, .25f) : new Color(.55f, .85f, 1);
            zone.text = "ZONE " + target;
            start.text = "ZONE " + state.FirstZone;
            var count = state.RemainingZones;
            remaining.text = count == 0 ? "SAFE ZONE REACHED" : count + (count == 1 ? " ZONE TO GO" : " ZONES TO GO");
            icon.sprite = gold ? goldIcon : silverIcon;
            fill.color = tipImage.color = color;
            glow.color = new Color(color.r, color.g, color.b, .55f);
            Cancel();
            m_TargetProgress = state.Progress;
            if (!animate || !isActiveAndEnabled)
            {
                Apply(m_TargetProgress);
                return;
            }
            m_Transition = DOTween.Sequence().SetUpdate(true);
            m_Transition.Append(DOTween.To(() => m_Progress, Apply, m_TargetProgress, .5f).SetEase(Ease.OutCubic));
            for (var index = 0; index < Mathf.Min(10, particles.Length); index++)
            {
                var particle = particles[index];
                var rect = particle.rectTransform;
                var delay = index * .05f;
                var offset = new Vector3(Random.Range(-34f, 34f), Random.Range(15f, 50f), 0);
                m_Transition.InsertCallback(delay, () =>
                {
                    rect.position = tip.position;
                    rect.localScale = Vector3.one * .35f;
                    particle.color = color;
                });
                m_Transition.Insert(delay, DOTween.To(() => rect.localPosition,
                    value => rect.localPosition = value, offset, .35f).SetRelative().SetEase(Ease.OutQuad));
                m_Transition.Insert(delay, DOTween.To(() => particle.color.a,
                    value => particle.color = new Color(color.r, color.g, color.b, value), 0, .35f));
            }
        }

        private void Apply(float value)
        {
            m_Progress = Mathf.Clamp01(value);
            var height = track.rect.height * m_Progress;
            fill.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            tip.anchoredPosition = new Vector2(0, height);
        }

        private void Cancel()
        {
            m_Transition?.Kill();
            m_Transition = null;
            foreach (var particle in particles)
                particle.color = new Color(1, 1, 1, 0);
        }

        private void OnDisable()
        {
            Cancel();
            // A cancelled visual operation must still represent the latest runtime snapshot.
            if (m_Zone > 0)
                Apply(m_TargetProgress);
        }
    }
}
