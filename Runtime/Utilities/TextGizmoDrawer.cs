using UnityEngine;

namespace CurveMaster.Utilities
{
    /// <summary>
    /// 文字 Gizmo 繪製器 - 在 Scene 視圖中顯示標註文字
    /// </summary>
    public class TextGizmoDrawer : MonoBehaviour
    {
        [Header("文字")]
        [SerializeField] private string text = "";
        [SerializeField] private Color color = Color.white;
        [SerializeField] private Vector3 offset = Vector3.up;
        
        [Header("顯示")]
        [SerializeField] private bool alwaysShow = true;
        [SerializeField] private float fontSize = 14f;
        
        public string Text
        {
            get => text;
            set => text = value;
        }
        
        private void OnDrawGizmos()
        {
            if (alwaysShow)
                DrawText();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!alwaysShow)
                DrawText();
        }
        
        private void DrawText()
        {
            #if UNITY_EDITOR
            string displayText = string.IsNullOrEmpty(text) ? gameObject.name : text;
            Vector3 position = transform.position + offset;
            
            GUIStyle style = new GUIStyle();
            style.normal.textColor = color;
            style.fontSize = Mathf.RoundToInt(fontSize);
            style.alignment = TextAnchor.MiddleCenter;
            
            UnityEditor.Handles.Label(position, displayText, style);
            #endif
        }
    }
}