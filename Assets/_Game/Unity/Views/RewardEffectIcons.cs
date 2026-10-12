using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    internal class RewardEffectIcons
    {
        private const float m_GlowStartScale = 0.8f;
        private readonly Image m_Main;
        private Image[] m_Particles;
        private Image m_Source;
        private bool m_SourceWasEnabled;
        public IReadOnlyList<Image> Particles => m_Particles;
        public Image Glow { get; private set; }
        public Vector3 Scale { get; private set; }

        public RewardEffectIcons(Image main)
        {
            m_Main = main;
        }

        public void Prepare(WheelSliceView slice, int particleCount, Sprite glowSprite, Color glowColor)
        {
            EnsureIcons(particleCount, glowSprite);
            HideSource(slice);
            Configure(m_Main, slice, Scale);
            foreach (var particle in m_Particles)
                Configure(particle, slice, Vector3.zero);
            PrepareGlow(slice, glowSprite, glowColor);
        }

        public void Reset()
        {
            if (m_Source != null)
                m_Source.enabled = m_SourceWasEnabled;
            m_Source = null;
            m_Main.gameObject.SetActive(false);
            if (Glow != null)
                Glow.gameObject.SetActive(false);
            if (m_Particles != null)
                foreach (var particle in m_Particles)
                    particle.gameObject.SetActive(false);
        }

        private void EnsureIcons(int particleCount, Sprite glowSprite)
        {
            if (m_Particles == null)
            {
                m_Particles = new Image[particleCount];
                for (var index = 0; index < particleCount; index++)
                    m_Particles[index] = CreateIcon("ui_image_reward_burst_" + index + "_value");
            }
            if (Glow == null && glowSprite != null)
            {
                Glow = CreateIcon("ui_image_reward_glow_value");
                Glow.transform.SetSiblingIndex(m_Main.transform.GetSiblingIndex());
            }
        }

        private Image CreateIcon(string name)
        {
            var image = Object.Instantiate(m_Main, m_Main.transform.parent);
            image.name = name;
            image.raycastTarget = false;
            image.maskable = false;
            return image;
        }

        private void HideSource(WheelSliceView slice)
        {
            m_Source = slice.IconRect.GetComponent<Image>();
            m_SourceWasEnabled = m_Source.enabled;
            m_Source.enabled = false;
            var sourceScale = slice.IconRect.lossyScale;
            var parentScale = m_Main.transform.parent.lossyScale;
            Scale = new Vector3(sourceScale.x / parentScale.x, sourceScale.y / parentScale.y, 1f);
        }

        private void PrepareGlow(WheelSliceView slice, Sprite sprite, Color color)
        {
            if (Glow == null)
                return;
            Configure(Glow, slice, Scale * m_GlowStartScale);
            Glow.sprite = sprite;
            var size = Mathf.Max(slice.IconRect.rect.width, slice.IconRect.rect.height);
            Glow.rectTransform.sizeDelta = Vector2.one * size;
            Glow.color = color;
        }

        private static void Configure(Image image, WheelSliceView slice, Vector3 scale)
        {
            image.sprite = slice.Icon;
            image.rectTransform.sizeDelta = slice.IconRect.rect.size;
            image.rectTransform.position = slice.IconRect.TransformPoint(slice.IconRect.rect.center);
            image.rectTransform.rotation = Quaternion.identity;
            image.rectTransform.localScale = scale;
            image.gameObject.SetActive(true);
        }
    }
}
