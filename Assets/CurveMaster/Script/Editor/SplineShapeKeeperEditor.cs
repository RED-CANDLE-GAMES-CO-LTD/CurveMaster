using UnityEngine;
using UnityEditor;
using CurveMaster.Components;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineShapeKeeper 編輯器 - 極簡介面設計
    /// </summary>
    [CustomEditor(typeof(SplineShapeKeeper))]
    public class SplineShapeKeeperEditor : UnityEditor.Editor
    {
        private SerializedProperty shapeMode;
        private SerializedProperty preservationMode;
        private SerializedProperty elasticity;
        private SerializedProperty smoothness;
        private SerializedProperty shapeFidelity;
        private SerializedProperty compressionResponse;
        private SerializedProperty updateRate;
        private SerializedProperty snapOnEnable;
        
        private void OnEnable()
        {
            shapeMode = serializedObject.FindProperty("shapeMode");
            preservationMode = serializedObject.FindProperty("preservationMode");
            elasticity = serializedObject.FindProperty("elasticity");
            smoothness = serializedObject.FindProperty("smoothness");
            shapeFidelity = serializedObject.FindProperty("shapeFidelity");
            compressionResponse = serializedObject.FindProperty("compressionResponse");
            updateRate = serializedObject.FindProperty("updateRate");
            snapOnEnable = serializedObject.FindProperty("snapOnEnable");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Simple description
            EditorGUILayout.HelpBox(
                "Automatically maintains curve shape. Control points with trackers are fixed, others adjust automatically.", 
                MessageType.None);
            
            EditorGUILayout.Space();
            
            // Main settings
            EditorGUILayout.PropertyField(shapeMode);
            EditorGUILayout.PropertyField(preservationMode);
            EditorGUILayout.PropertyField(snapOnEnable);
            
            EditorGUILayout.Space();
            
            // 參數
            if (shapeMode.enumValueIndex == (int)SplineShapeKeeper.ShapeMode.Elastic)
            {
                EditorGUILayout.PropertyField(elasticity);
            }
            
            EditorGUILayout.PropertyField(smoothness);
            EditorGUILayout.PropertyField(shapeFidelity);
            
            if (preservationMode.enumValueIndex == (int)SplineShapeKeeper.ShapePreservation.ElasticBend)
            {
                EditorGUILayout.PropertyField(compressionResponse);
            }
            
            serializedObject.ApplyModifiedProperties();
        }
    }
}