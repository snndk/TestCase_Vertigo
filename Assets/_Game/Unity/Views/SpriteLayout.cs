using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.Unity._Game.Unity.Views
{
    public static class SpriteLayout
    {
        public static void Fit(Image image, Vector2 bounds)
        {
            var size = image.sprite.rect.size;
            var scale = Mathf.Min(bounds.x / size.x, bounds.y / size.y);
            image.rectTransform.sizeDelta = size * scale;
        }
    }
}
