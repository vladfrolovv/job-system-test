using UnityEngine;
using UnityEngine.UI;

namespace Utils.Extensions
{
    public static class UIExtensions
    {
        public static void SetAlpha(this Image image, float alpha)
        {
            Color color = image.color;
            image.color = new Color(color.r, color.g, color.b, alpha);
        }
    }
}