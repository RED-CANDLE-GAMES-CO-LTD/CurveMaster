using UnityEngine;
using UnityEditor;
using CurveMaster.Components;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineRenderer 編輯器
    /// </summary>
    [CustomEditor(typeof(SplineRenderer))]
    public class SplineRendererEditor : UnityEditor.Editor
    {
        private SplineRenderer splineRenderer;
        private SerializedProperty splineManagerProp;
        private SerializedProperty renderResolutionProp;
        private SerializedProperty lineWidthProp;
        private SerializedProperty autoUpdateProp;
        private SerializedProperty lineMaterialProp;
        private SerializedProperty colorGradientProp;

        private void OnEnable()
        {
            splineRenderer = (SplineRenderer)target;
            splineManagerProp = serializedObject.FindProperty("splineManager");
            renderResolutionProp = serializedObject.FindProperty("renderResolution");
            lineWidthProp = serializedObject.FindProperty("lineWidth");
            autoUpdateProp = serializedObject.FindProperty("autoUpdate");
            lineMaterialProp = serializedObject.FindProperty("lineMaterial");
            colorGradientProp = serializedObject.FindProperty("colorGradient");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("曲線渲染設定", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(splineManagerProp, new GUIContent("曲線管理器"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("渲染參數", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(renderResolutionProp, 10, 200, new GUIContent("渲染解析度"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineRenderer.UpdateSplineVisualization();
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(lineWidthProp, 0.01f, 1f, new GUIContent("線條寬度"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineRenderer.SetLineWidth(lineWidthProp.floatValue);
            }

            EditorGUILayout.PropertyField(autoUpdateProp, new GUIContent("自動更新"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("視覺效果", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(lineMaterialProp, new GUIContent("線條材質"));
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorGradientProp, new GUIContent("顏色漸層"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineRenderer.SetColorGradient(colorGradientProp.gradientValue);
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("更新渲染"))
            {
                splineRenderer.UpdateSplineVisualization();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "SplineRenderer 使用 LineRenderer 在 Scene 和 Game 視圖中渲染曲線。\n" +
                "可以調整解析度、寬度和顏色來獲得最佳視覺效果。", 
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}