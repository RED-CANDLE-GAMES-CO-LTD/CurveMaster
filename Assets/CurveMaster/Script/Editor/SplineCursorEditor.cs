#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using CurveMaster.Components;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineCursor Editor
    /// </summary>
    [CustomEditor(typeof(SplineCursor))]
    public class SplineCursorEditor : UnityEditor.Editor
    {
        private SplineCursor cursor;
        private SerializedProperty splineManagerProp;
        private SerializedProperty positionProp;
        private SerializedProperty alignToTangentProp;
        private SerializedProperty autoUpdateProp;

        private void OnEnable()
        {
            cursor = (SplineCursor)target;
            splineManagerProp = serializedObject.FindProperty("splineManager");
            positionProp = serializedObject.FindProperty("position");
            alignToTangentProp = serializedObject.FindProperty("alignToTangent");
            autoUpdateProp = serializedObject.FindProperty("autoUpdate");
            
            // Force initialization and update position
            if (cursor != null)
            {
                cursor.UpdateTransform();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spline Follow Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(splineManagerProp, new GUIContent("Spline Manager"));
            
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(positionProp, 0f, 1f, new GUIContent("Position"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (autoUpdateProp.boolValue)
                {
                    cursor.UpdateTransform();
                }
            }

            EditorGUILayout.PropertyField(alignToTangentProp, new GUIContent("Align To Tangent"));
            EditorGUILayout.PropertyField(autoUpdateProp, new GUIContent("Auto Update"));

            EditorGUILayout.Space();
            
            if (GUILayout.Button("Update Position"))
            {
                cursor.UpdateTransform();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Test", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset to Start"))
            {
                positionProp.floatValue = 0f;
                serializedObject.ApplyModifiedProperties();
                cursor.UpdateTransform();
            }
            if (GUILayout.Button("Move to End"))
            {
                positionProp.floatValue = 1f;
                serializedObject.ApplyModifiedProperties();
                cursor.UpdateTransform();
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }

        private void OnSceneGUI()
        {
            if (cursor == null || cursor.transform == null)
                return;

            Handles.color = Color.cyan;
            float size = HandleUtility.GetHandleSize(cursor.transform.position) * 0.1f;
            Handles.SphereHandleCap(0, cursor.transform.position, Quaternion.identity, size, EventType.Repaint);

            if (cursor.AlignToTangent)
            {
                Handles.color = Color.blue;
                Handles.ArrowHandleCap(0, cursor.transform.position, cursor.transform.rotation, size * 3f, EventType.Repaint);
            }
        }
    }
}
#endif