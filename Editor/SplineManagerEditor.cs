using UnityEngine;
using UnityEditor;
using CurveMaster.Components;
using CurveMaster.Core;
using CurveMaster.Splines;
using System.Collections.Generic;

namespace CurveMaster.Editor
{
    /// <summary>
    /// SplineManager Editor
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
        
        // Bezier curve editing state
        private int selectedHandleIndex = -1;
        private bool symmetricMode = true;
        private bool showHandleInfo = true;

        private void OnEnable()
        {
            splineManager = (SplineManager)target;
            splineTypeProp = serializedObject.FindProperty("splineType");
            autoUpdateProp = serializedObject.FindProperty("autoUpdate");
            splineColorProp = serializedObject.FindProperty("splineColor");
            resolutionProp = serializedObject.FindProperty("resolution");
            autoDetectControlPointsProp = serializedObject.FindProperty("autoDetectControlPoints");
            autoUpdateCursorsProp = serializedObject.FindProperty("autoUpdateCursors");
            
            // Initial refresh control points list and force initialize spline
            if (splineManager.AutoDetectControlPoints)
            {
                splineManager.RefreshControlPointsList();
            }
            
            // Ensure spline has been initialized and updated
            splineManager.ForceInitializeSpline();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spline Settings", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(splineTypeProp, new GUIContent("Spline Type"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                splineManager.SwitchSplineType((SplineType)splineTypeProp.enumValueIndex);
            }

            EditorGUILayout.PropertyField(autoUpdateProp, new GUIContent("Auto Update", "Automatically update spline when control points change"));
            EditorGUILayout.PropertyField(autoUpdateCursorsProp, new GUIContent("Auto Update Cursors", "Automatically update all SplineCursor positions when control points change"));
            EditorGUILayout.PropertyField(splineColorProp, new GUIContent("Spline Color"));
            EditorGUILayout.PropertyField(resolutionProp, new GUIContent("Draw Resolution"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Control Points", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(autoDetectControlPointsProp, new GUIContent("Auto Detect Children"));
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
                EditorGUILayout.HelpBox("Control points will be automatically detected from child objects", MessageType.Info);
                
                // Display detected control points (read-only)
                EditorGUI.BeginDisabledGroup(true);
                EditorGUI.indentLevel++;
                for (int i = 0; i < splineManager.ControlPointTransforms.Count; i++)
                {
                    Transform t = splineManager.ControlPointTransforms[i];
                    EditorGUILayout.ObjectField($"Control Point {i + 1}", t, typeof(Transform), true);
                }
                EditorGUI.indentLevel--;
                EditorGUI.EndDisabledGroup();
            }
            else
            {
                EditorGUILayout.HelpBox("Manual mode - Please manually specify control points", MessageType.Warning);
            }

            EditorGUILayout.Space();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add Control Point"))
            {
                AddControlPoint();
            }
            if (GUILayout.Button("Clear All Control Points"))
            {
                if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear all control points?", "OK", "Cancel"))
                {
                    splineManager.ClearControlPoints();
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // Manual update cursor button
            if (GUILayout.Button("Update All Cursor Positions"))
            {
                splineManager.ForceUpdateAllCursors();
            }

            if (splineManager.Spline != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Spline Info", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"Spline Length: {splineManager.GetLength():F2}");
                EditorGUILayout.LabelField($"Control Point Count: {splineManager.ControlPointTransforms.Count}");
                
                // Bezier curve special controls
                if (splineManager.CurrentType == SplineType.BezierSpline)
                {
                    DrawBezierControls();
                }
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
            
            // If in auto detect mode, refresh the list
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
                    Undo.RecordObject(controlPoint, "Move Control Point");
                    controlPoint.position = newPosition;
                }

                // Draw number labels
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = 14;
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                
                // Background circle
                Handles.color = new Color(0, 0, 0, 0.7f);
                float size = HandleUtility.GetHandleSize(controlPoint.position) * 0.15f;
                Handles.DrawSolidDisc(controlPoint.position + Vector3.up * 0.5f, Camera.current.transform.forward, size);
                
                // Number
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
            
            // Draw Bezier handles for each control point
            var controlPoints = splineManager.ControlPointTransforms;
            for (int i = 0; i < controlPoints.Count; i++)
            {
                Transform controlPoint = controlPoints[i];
                if (controlPoint == null)
                    continue;
                
                Vector3 worldPos = controlPoint.position;
                
                // Draw in handle
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
                        Undo.RecordObject(splineManager, "Adjust Bezier Handle");
                        bezier.SetHandleIn(i, splineManager.transform.InverseTransformPoint(handleInWorld));
                        
                        // If symmetric mode is enabled, mirror adjust out handle
                        if (symmetricMode && i < controlPoints.Count - 1)
                        {
                            bezier.MirrorHandle(i, false);
                        }
                    }
                    
                    // Draw handle sphere
                    Handles.color = new Color(1f, 0.5f, 0f, 1f);
                    float size = HandleUtility.GetHandleSize(handleInWorld) * 0.08f;
                    Handles.SphereHandleCap(0, handleInWorld, Quaternion.identity, size, EventType.Repaint);
                }
                
                // Draw out handle
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
                        Undo.RecordObject(splineManager, "Adjust Bezier Handle");
                        bezier.SetHandleOut(i, splineManager.transform.InverseTransformPoint(handleOutWorld));
                        
                        // If symmetric mode is enabled, mirror adjust in handle
                        if (symmetricMode && i > 0)
                        {
                            bezier.MirrorHandle(i, true);
                        }
                    }
                    
                    // Draw handle sphere
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

            // Draw spline main body
            Handles.color = splineManager.SplineColor;
            
            int segments = splineManager.Resolution * 2; // Increase preview resolution
            Vector3 prevPoint = splineManager.GetWorldPoint(0);
            
            // Use thicker lines
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector3 point = splineManager.GetWorldPoint(t);
                Handles.DrawLine(prevPoint, point, 3f);
                prevPoint = point;
            }

            // Draw direction indicators
            DrawDirectionIndicators();
            
            // Draw spline info
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
            // Show labels at spline start and end points
            Vector3 startPoint = splineManager.GetWorldPoint(0f);
            Vector3 endPoint = splineManager.GetWorldPoint(1f);
            
            Handles.color = Color.green;
            Handles.Label(startPoint + Vector3.up * 0.2f, "Start");
            
            Handles.color = Color.red;
            Handles.Label(endPoint + Vector3.up * 0.2f, "End");
            
            // Show spline length
            Vector3 midPoint = splineManager.GetWorldPoint(0.5f);
            Handles.color = Color.white;
            Handles.Label(midPoint + Vector3.up * 0.5f, 
                $"[{splineManager.CurrentType}]\nLength: {splineManager.GetLength():F2} units");
        }
        
        private void DrawBezierControls()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bezier Curve Control", EditorStyles.boldLabel);
            
            BezierSpline bezier = splineManager.Spline as BezierSpline;
            if (bezier == null)
                return;
            
            // Global controls
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reset All Handles"))
            {
                Undo.RecordObject(splineManager, "Reset Bezier Handles");
                bezier.ResetAllHandles();
                EditorUtility.SetDirty(splineManager);
            }
            
            symmetricMode = GUILayout.Toggle(symmetricMode, "Symmetric Mode", "Button");
            showHandleInfo = GUILayout.Toggle(showHandleInfo, "Show Parameters", "Button");
            EditorGUILayout.EndHorizontal();
            
            // Show handle parameters for each control point
            if (showHandleInfo)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Handle Parameters", EditorStyles.boldLabel);
                
                for (int i = 0; i < splineManager.ControlPointTransforms.Count; i++)
                {
                    Transform controlPoint = splineManager.ControlPointTransforms[i];
                    if (controlPoint == null)
                        continue;
                    
                    EditorGUILayout.BeginVertical("box");
                    
                    // Control point title
                    EditorGUILayout.BeginHorizontal();
                    bool isExpanded = selectedHandleIndex == i;
                    if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(20)))
                    {
                        selectedHandleIndex = isExpanded ? -1 : i;
                    }
                    EditorGUILayout.LabelField($"Control Point {i + 1}: {controlPoint.name}");
                    
                    if (GUILayout.Button("Reset", GUILayout.Width(50)))
                    {
                        Undo.RecordObject(splineManager, $"Reset Control Point {i + 1} Handles");
                        bezier.ResetHandles(i);
                        EditorUtility.SetDirty(splineManager);
                    }
                    EditorGUILayout.EndHorizontal();
                    
                    // Expanded handle parameters
                    if (selectedHandleIndex == i)
                    {
                        EditorGUI.indentLevel++;
                        
                        // In handle
                        if (i > 0)
                        {
                            EditorGUILayout.LabelField("In Handle (Orange)", EditorStyles.miniBoldLabel);
                            Vector3 handleInLocal = bezier.GetHandleIn(i);
                            Vector3 handleInWorld = splineManager.transform.TransformPoint(handleInLocal);
                            
                            EditorGUI.BeginChangeCheck();
                            Vector3 newHandleInWorld = EditorGUILayout.Vector3Field("World Position", handleInWorld);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(splineManager, "Adjust In Handle");
                                bezier.SetHandleIn(i, splineManager.transform.InverseTransformPoint(newHandleInWorld));
                                
                                if (symmetricMode)
                                {
                                    bezier.MirrorHandle(i, false);
                                }
                                EditorUtility.SetDirty(splineManager);
                            }
                            
                            Vector3 offset = handleInLocal - splineManager.transform.InverseTransformPoint(controlPoint.position);
                            EditorGUILayout.Vector3Field("Relative Offset", offset);
                            EditorGUILayout.FloatField("Distance", offset.magnitude);
                        }
                        
                        // Out handle
                        if (i < splineManager.ControlPointTransforms.Count - 1)
                        {
                            EditorGUILayout.LabelField("Out Handle (Blue)", EditorStyles.miniBoldLabel);
                            Vector3 handleOutLocal = bezier.GetHandleOut(i);
                            Vector3 handleOutWorld = splineManager.transform.TransformPoint(handleOutLocal);
                            
                            EditorGUI.BeginChangeCheck();
                            Vector3 newHandleOutWorld = EditorGUILayout.Vector3Field("World Position", handleOutWorld);
                            if (EditorGUI.EndChangeCheck())
                            {
                                Undo.RecordObject(splineManager, "Adjust Out Handle");
                                bezier.SetHandleOut(i, splineManager.transform.InverseTransformPoint(newHandleOutWorld));
                                
                                if (symmetricMode && i > 0)
                                {
                                    bezier.MirrorHandle(i, true);
                                }
                                EditorUtility.SetDirty(splineManager);
                            }
                            
                            Vector3 offset = handleOutLocal - splineManager.transform.InverseTransformPoint(controlPoint.position);
                            EditorGUILayout.Vector3Field("Relative Offset", offset);
                            EditorGUILayout.FloatField("Distance", offset.magnitude);
                        }
                        
                        // Symmetry operation buttons
                        if (i > 0 && i < splineManager.ControlPointTransforms.Count - 1)
                        {
                            EditorGUILayout.BeginHorizontal();
                            if (GUILayout.Button("Force Symmetric"))
                            {
                                Undo.RecordObject(splineManager, "Force Symmetric Handles");
                                bezier.SetHandleSymmetric(i, true);
                                EditorUtility.SetDirty(splineManager);
                            }
                            EditorGUILayout.EndHorizontal();
                        }
                        
                        EditorGUI.indentLevel--;
                    }
                    
                    EditorGUILayout.EndVertical();
                }
            }
        }
    }
}