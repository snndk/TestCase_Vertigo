using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.Runtime._Game.Runtime.Rounds;
using Vertigo.Unity._Game.Unity.Authoring;

namespace Vertigo.Unity._Game.Unity.Views
{
    public class WheelSliceView : MonoBehaviour
    {
        [SerializeField] private RectTransform motion;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text amount;
        [SerializeField] private Vector2 iconBounds = new Vector2(74, 68);
        public RectTransform Motion => motion;
        public Sprite Icon => icon.sprite;
        public RectTransform IconRect => icon.rectTransform;
        public SliceData Slice { get; private set; }
        public WheelTierAsset Tier { get; private set; }
        public void Bind(SliceData slice, SliceAsset visual, WheelTierAsset tier = null)
        {
            Slice = slice;
            Tier = tier;
            icon.sprite = visual.Icon;
            SpriteLayout.Fit(icon, iconBounds);
            amount.text = visual.GetCaption(slice);
            amount.color = visual.CaptionColor;
            ResetMotion();
        }
        public void KeepUpright(float wheelAngle) => motion.localRotation = Quaternion.Euler(0, 0, -wheelAngle);
        public void ResetMotion() => motion.localScale = Vector3.one;
    }
}
