using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor
{
    public class GUIStyles
    {
        public static GUIStyle RegionNameStyle { get; private set; }
        public static GUIStyle RegionStyle { get; private set; }
        public static GUIStyle SettingsWindowGrid { get; private set; }
        public static GUIStyle LargeButtonStyle { get; private set; }

        private static Color _regionBackgroundColour = new Color(0.21f, 0.21f, 0.21f);

        static GUIStyles()
        {
            RefreshStyles();
        }

        public static void RefreshStyles()
        {
            RegionNameStyle = new GUIStyle()
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,

            };
            RegionNameStyle.normal.textColor = Color.white;

            RegionStyle = new GUIStyle()
            {
                fontSize = 12,
                imagePosition = ImagePosition.ImageAbove,
                fontStyle = FontStyle.Normal,
                fixedWidth = 0,
                fixedHeight = 0,
                margin = new RectOffset(5, 0, 5, 5),
                padding = new RectOffset(15, 15, 10, 10),
                border = new RectOffset(6, 6, 6, 6),
                normal = new GUIStyleState
                {
                    background = CreateFlatTexture(_regionBackgroundColour)
                }
            };

            SettingsWindowGrid = new GUIStyle()
            {
                margin = new RectOffset(10, 10, 5, 5),
                padding = new RectOffset(10, 10, 5, 5),
                fixedWidth = 100,
            };

            SettingsWindowGrid.normal.textColor = Color.white;

            LargeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                margin = new RectOffset(100, 100, 0, 0),
                padding = new RectOffset(10, 10, 20, 20)
            };
        }

        private static Texture2D CreateFlatTexture(Color color)
        {
            int size = 12;
            Texture2D texture = new Texture2D(size, size);
            float radius = size / 3f;
            Color borderColor = Color.black;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2, size / 2));
                    float alpha = distance < radius ? 1f : 0f;

                    // Add border when near the radius threshold
                    if (distance >= radius - 1f && distance < radius)
                        texture.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, alpha));
                    else
                        texture.SetPixel(x, y, new Color(color.r, color.g, color.b, alpha));
                }
            }

            texture.Apply();
            texture.wrapMode = TextureWrapMode.Clamp;
            return texture;
        }
    }
}
