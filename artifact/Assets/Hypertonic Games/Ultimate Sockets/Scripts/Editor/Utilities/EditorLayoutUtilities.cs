using UnityEditor;
using UnityEngine;

namespace Hypertonic.Modules.UltimateSockets.Editor
{
    public class EditorLayoutUtilities : MonoBehaviour
    {
        private static Color _regionBackgroundColour = new Color(0.83f, 0.83f, 0.83f);
        private static Color _defaultGUIBackgroundColour = Color.black;

        public static void DrawTopOfSection(string sectionTitle)
        {
            _defaultGUIBackgroundColour = GUI.backgroundColor;

            GUI.backgroundColor = _regionBackgroundColour;

            // This is needed as sometimes the background colour is not restored correctly when switching between scenes.
            GUIStyles.RefreshStyles();
            EditorGUILayout.Space();
            GUILayout.BeginVertical(GUIStyles.RegionStyle);
            EditorGUILayout.LabelField(sectionTitle, GUIStyles.RegionNameStyle);
            EditorGUILayout.Space();
        }


        public static void DrawBottomOfSection()
        {
            GUILayout.EndVertical();

            GUI.backgroundColor = _defaultGUIBackgroundColour;
        }
    }
}
