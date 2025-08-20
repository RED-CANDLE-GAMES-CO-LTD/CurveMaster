using UnityEngine;
using UnityEditor;
using CurveMaster.Components;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineCursor 編輯器
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
            
            // 強制初始化並更新位置
            if (cursor != null)
            {
                cursor.UpdateTransform();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("曲線跟隨設定", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(splineManagerProp, new GUIContent("曲線管理器"));
            
            EditorGUILayout.Space();
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.Slider(positionProp, 0f, 1f, new GUIContent("位置"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (autoUpdateProp.boolValue)
                {
                    cursor.UpdateTransform();
                }
            }

            EditorGUILayout.PropertyField(alignToTangentProp, new GUIContent("對齊切線"));
            EditorGUILayout.PropertyField(autoUpdateProp, new GUIContent("自動更新"));

            EditorGUILayout.Space();
            
            if (GUILayout.Button("更新位置"))
            {
                cursor.UpdateTransform();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("動畫測試", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("重置到起點"))
            {
                positionProp.floatValue = 0f;
                serializedObject.ApplyModifiedProperties();
                cursor.UpdateTransform();
            }
            if (GUILayout.Button("移到終點"))
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