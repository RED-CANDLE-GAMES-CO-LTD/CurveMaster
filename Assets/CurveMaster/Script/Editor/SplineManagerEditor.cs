using UnityEngine;
using UnityEditor;
using CurveMaster.Components;
using CurveMaster.Core;
using CurveMaster.Splines;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineManager 編輯器
    /// </summary>
    [CustomEditor(typeof(SplineManager))]
    public class SplineManagerEditor : UnityEditor.Editor
    {
        private SplineManager splineManager;
        private SerializedProperty splineTypeProp;
        private SerializedProperty autoUpdateProp;
        private SerializedProperty splineColorProp;
        private SerializedProperty resolutionProp;
        private SerializedProperty autoDetectControlPointsProp;
        private SerializedProperty autoUpdateCursorsProp;

        private void OnEnable()
        {
            splineManager = (SplineManager)target;
            splineTypeProp = serializedObject.FindProperty("splineType");
            autoUpdateProp = serializedObject.FindProperty("autoUpdate");
            splineColorProp = serializedObject.FindProperty("splineColor");
            resolutionProp = serializedObject.FindProperty("resolution");
            autoDetectControlPointsProp = serializedObject.FindProperty("autoDetectControlPoints");
            autoUpdateCursorsProp = serializedObject.FindProperty("autoUpdateCursors");
            
            // 初始重新整理控制點清單
            if (splineManager.AutoDetectControlPoints)
            {
                splineManager.RefreshControlPointsList();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("曲線設定", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(splineTypeProp, new GUIContent("曲線類型"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineManager.SwitchSplineType((SplineType)splineTypeProp.enumValueIndex);
            }

            EditorGUILayout.PropertyField(autoUpdateProp, new GUIContent("自動更新", "控制點變更時自動更新曲線"));
            EditorGUILayout.PropertyField(autoUpdateCursorsProp, new GUIContent("自動更新游標", "控制點變更時自動更新所有 SplineCursor 的位置"));
            EditorGUILayout.PropertyField(splineColorProp, new GUIContent("曲線顏色"));
            EditorGUILayout.PropertyField(resolutionProp, new GUIContent("繪製解析度"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("控制點", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(autoDetectControlPointsProp, new GUIContent("自動偵測子物件"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                if (autoDetectControlPointsProp.boolValue)
                {
                    splineManager.RefreshControlPointsList();
                }
            }
            
            if (splineManager.AutoDetectControlPoints)
            {
                EditorGUILayout.HelpBox("控制點會自動從子物件偵測", MessageType.Info);
                
                // 顯示偵測到的控制點（唯讀）
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                for (int i = 0; i < splineManager.ControlPointTransforms.Count; i++)
                {
                    Transform t = splineManager.ControlPointTransforms[i];
                    EditorGUILayout.ObjectField($"控制點 {i + 1}", t, typeof(Transform), true);
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("手動模式 - 請手動指定控制點", MessageType.Warning);
            }

            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("新增控制點"))
            {
                AddControlPoint();
            }
            if (GUILayout.Button("清除所有控制點"))
            {
                if (EditorUtility.DisplayDialog("確認", "確定要清除所有控制點嗎？", "確定", "取消"))
                {
                    splineManager.ClearControlPoints();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 手動更新 Cursor 按鈕
            if (GUILayout.Button("更新所有游標位置"))
            {
                splineManager.ForceUpdateAllCursors();
            }

            if (splineManager.Spline != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("曲線資訊", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"曲線長度: {splineManager.GetLength():F2}");
                EditorGUILayout.LabelField($"控制點數量: {splineManager.ControlPointTransforms.Count}");
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void AddControlPoint()
        {
            GameObject controlPoint = new GameObject($"ControlPoint_{splineManager.ControlPointTransforms.Count}");
            controlPoint.transform.parent = splineManager.transform;
            controlPoint.transform.localPosition = Vector3.forward * splineManager.ControlPointTransforms.Count * 2;
            
            SplineControlPoint cp = controlPoint.AddComponent<SplineControlPoint>();
            cp.SetParentSpline(splineManager);
            cp.SetIndex(splineManager.ControlPointTransforms.Count);
            
            Selection.activeGameObject = controlPoint;
            
            // 如果是自動偵測模式，重新整理清單
            if (splineManager.AutoDetectControlPoints)
            {
                EditorApplication.delayCall += () => splineManager.RefreshControlPointsList();
            }
        }

        private void OnSceneGUI()
        {
            if (splineManager == null || splineManager.Spline == null)
                return;

            DrawControlPointHandles();
            DrawSplinePreview();
        }

        private void DrawControlPointHandles()
        {
            var controlPoints = splineManager.ControlPointTransforms;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                Transform controlPoint = controlPoints[i];
                
                if (controlPoint == null)
                    continue;

                EditorGUI.BeginChangeCheck();
                Vector3 newPosition = Handles.PositionHandle(controlPoint.position, controlPoint.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(controlPoint, "移動控制點");
                    controlPoint.position = newPosition;
                }

                // 繪製數字標注
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                
                // 背景圓圈
                Handles.color = new Color(0, 0, 0, 0.7f);
                float size = HandleUtility.GetHandleSize(controlPoint.position) * 0.15f;
                Handles.DrawSolidDisc(controlPoint.position + Vector3.up * 0.5f, Camera.current.transform.forward, size);
                
                // 數字
                Handles.Label(controlPoint.position + Vector3.up * 0.5f, (i + 1).ToString(), style);
            }

            if (splineManager.CurrentType == SplineType.BezierSpline)
            {
                DrawBezierHandles();
            }
        }

        private void DrawBezierHandles()
        {
            if (splineManager.Spline == null)
                return;
                
            BezierSpline bezier = splineManager.Spline as BezierSpline;
            if (bezier == null)
                return;
            
            // 繪製每個控制點的貝茲手柄
            var controlPoints = splineManager.ControlPointTransforms;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                Transform controlPoint = controlPoints[i];
                if (controlPoint == null)
                    continue;
                
                Vector3 worldPos = controlPoint.position;
                
                // 繪製入手柄
                if (i > 0)
                {
                    Vector3 handleInLocal = bezier.GetHandleIn(i);
                    Vector3 handleInWorld = splineManager.transform.TransformPoint(handleInLocal);
                    
                    Handles.color = new Color(1f, 0.5f, 0f, 0.7f);
                    Handles.DrawDottedLine(worldPos, handleInWorld, 2f);
                    
                    EditorGUI.BeginChangeCheck();
                    handleInWorld = Handles.PositionHandle(handleInWorld, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(splineManager, "調整貝茲手柄");
                        bezier.SetHandleIn(i, splineManager.transform.InverseTransformPoint(handleInWorld));
                    }
                    
                    // 繪製手柄球體
                    Handles.color = new Color(1f, 0.5f, 0f, 1f);
                    float size = HandleUtility.GetHandleSize(handleInWorld) * 0.08f;
                    Handles.SphereHandleCap(0, handleInWorld, Quaternion.identity, size, EventType.Repaint);
                }
                
                // 繪製出手柄
                if (i < controlPoints.Count - 1)
                {
                    Vector3 handleOutLocal = bezier.GetHandleOut(i);
                    Vector3 handleOutWorld = splineManager.transform.TransformPoint(handleOutLocal);
                    
                    Handles.color = new Color(0f, 0.5f, 1f, 0.7f);
                    Handles.DrawDottedLine(worldPos, handleOutWorld, 2f);
                    
                    EditorGUI.BeginChangeCheck();
                    handleOutWorld = Handles.PositionHandle(handleOutWorld, Quaternion.identity);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(splineManager, "調整貝茲手柄");
                        bezier.SetHandleOut(i, splineManager.transform.InverseTransformPoint(handleOutWorld));
                        
                        // 鏡像對稱調整
                        if (i > 0)
                        {
                            Vector3 mirrorHandle = worldPos + (worldPos - handleOutWorld);
                            bezier.SetHandleIn(i, splineManager.transform.InverseTransformPoint(mirrorHandle));
                        }
                    }
                    
                    // 繪製手柄球體
                    Handles.color = new Color(0f, 0.5f, 1f, 1f);
                    float size = HandleUtility.GetHandleSize(handleOutWorld) * 0.08f;
                    Handles.SphereHandleCap(0, handleOutWorld, Quaternion.identity, size, EventType.Repaint);
                }
            }
        }

        private void DrawSplinePreview()
        {
            if (Application.isPlaying)
                return;

            // 繪製曲線主體
            Handles.color = splineManager.SplineColor;
            
            int segments = splineManager.Resolution * 2; // 提高預覽解析度
            Vector3 prevPoint = splineManager.GetWorldPoint(0);
            
            // 使用較粗的線條
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = splineManager.GetWorldPoint(t);
                Handles.DrawLine(prevPoint, point, 3f);
                prevPoint = point;
            }

            // 繪製方向指示器
            DrawDirectionIndicators();
            
            // 繪製曲線資訊
            DrawSplineInfo();
        }

        private void DrawDirectionIndicators()
        {
            Handles.color = new Color(splineManager.SplineColor.r, splineManager.SplineColor.g, splineManager.SplineColor.b, 0.5f);
            
            int arrowCount = 5;
            for (int i = 1; i < arrowCount; i++)
            {
                float t = i / (float)arrowCount;
                Vector3 position = splineManager.GetWorldPoint(t);
                Vector3 tangent = splineManager.GetWorldTangent(t);
                
                if (tangent.sqrMagnitude > 0.001f)
                {
                    float size = HandleUtility.GetHandleSize(position) * 0.3f;
                    Handles.ArrowHandleCap(0, position, Quaternion.LookRotation(tangent), size, EventType.Repaint);
                }
            }
        }

        private void DrawSplineInfo()
        {
            // 在曲線起點和終點顯示標籤
            Vector3 startPoint = splineManager.GetWorldPoint(0f);
            Vector3 endPoint = splineManager.GetWorldPoint(1f);
            
            Handles.color = Color.green;
            Handles.Label(startPoint + Vector3.up * 0.2f, "起點");
            
            Handles.color = Color.red;
            Handles.Label(endPoint + Vector3.up * 0.2f, "終點");
            
            // 顯示曲線長度
            Vector3 midPoint = splineManager.GetWorldPoint(0.5f);
            Handles.color = Color.white;
            Handles.Label(midPoint + Vector3.up * 0.5f, 
                $"[{splineManager.CurrentType}]\n長度: {splineManager.GetLength():F2} 單位");
        }
    }
}