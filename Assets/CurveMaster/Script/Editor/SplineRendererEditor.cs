#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CurveMaster.Components;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineRenderer Editor
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
            EditorGUILayout.LabelField("Spline Rendering Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(splineManagerProp, new GUIContent("Spline Manager"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Render Parameters", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.IntSlider(renderResolutionProp, 10, 200, new GUIContent("Render Resolution"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineRenderer.UpdateSplineVisualization();
            }
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(lineWidthProp, 0.01f, 1f, new GUIContent("Line Width"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineRenderer.SetLineWidth(lineWidthProp.floatValue);
            }

            EditorGUILayout.PropertyField(autoUpdateProp, new GUIContent("Auto Update"));
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual Effects", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(lineMaterialProp, new GUIContent("Line Material"));
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(colorGradientProp, new GUIContent("Color Gradient"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                // For Unity 2021.3 compatibility, we apply changes through serialization
                // instead of using gradientValue which was added in Unity 2022.1
                splineRenderer.UpdateSplineVisualization();
            }

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Update Render"))
            {
                splineRenderer.UpdateSplineVisualization();
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "SplineRenderer uses LineRenderer to render curves in Scene and Game views.\n" +
                "Adjust resolution, width and color for best visual results.", 
                MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif