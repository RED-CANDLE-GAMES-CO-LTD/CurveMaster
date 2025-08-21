using UnityEngine;
using UnityEditor;
using CurveMaster.Components;

namespace CurveMaster.Editor
{
    /// <summary>
    /// 曲線追蹤管理器編輯器
    /// </summary>
    [CustomEditor(typeof(SplineTrackerManager))]
    public class SplineTrackerManagerEditor : UnityEditor.Editor
    {
        private SerializedProperty enableGlobalTracking;
        private SerializedProperty globalSpeedMultiplier;
        private SerializedProperty trackingSetups;
        private SerializedProperty autoEnableShapeKeeper;
        private SerializedProperty defaultShapeMode;
        private SerializedProperty showDebugInfo;
        private SerializedProperty visualizeConnections;
        
        private SplineTrackerManager manager;
        private bool showQuickSetup = false;
        private GameObject[] quickTargets = new GameObject[2];
        
        private void OnEnable()
        {
            manager = (SplineTrackerManager)target;
            
            enableGlobalTracking = serializedObject.FindProperty("enableGlobalTracking");
            globalSpeedMultiplier = serializedObject.FindProperty("globalSpeedMultiplier");
            trackingSetups = serializedObject.FindProperty("trackingSetups");
            autoEnableShapeKeeper = serializedObject.FindProperty("autoEnableShapeKeeper");
            defaultShapeMode = serializedObject.FindProperty("defaultShapeMode");
            showDebugInfo = serializedObject.FindProperty("showDebugInfo");
            visualizeConnections = serializedObject.FindProperty("visualizeConnections");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Title
            EditorGUILayout.LabelField("Spline Tracker Manager", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // 全域設定
            DrawGlobalSettings();
            EditorGUILayout.Space();
            
            // 快速設定工具
            DrawQuickSetupTools();
            EditorGUILayout.Space();
            
            // 追蹤設定清單
            DrawTrackingSetups();
            EditorGUILayout.Space();
            
            // 形狀維持設定
            DrawShapeKeeperSettings();
            EditorGUILayout.Space();
            
            // 偵錯設定
            DrawDebugSettings();
            EditorGUILayout.Space();
            
            // 操作按鈕
            DrawActionButtons();
            
            serializedObject.ApplyModifiedProperties();
            
            // 即時更新
            if (GUI.changed && Application.isPlaying)
            {
                manager.ApplyAllSetups();
            }
        }
        
        private void DrawGlobalSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(enableGlobalTracking, new GUIContent("Enable Global Tracking"));
            
            EditorGUI.BeginDisabledGroup(!enableGlobalTracking.boolValue);
            EditorGUILayout.PropertyField(globalSpeedMultiplier, new GUIContent("Speed Multiplier"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawQuickSetupTools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            showQuickSetup = EditorGUILayout.Foldout(showQuickSetup, "Quick Setup Tools", true);
            
            if (showQuickSetup)
            {
                EditorGUILayout.Space(5);
                
                // Endpoint tracking setup
                EditorGUILayout.LabelField("Endpoint Tracking Setup", EditorStyles.miniBoldLabel);
                quickTargets[0] = (GameObject)EditorGUILayout.ObjectField(
                    "Start Target", quickTargets[0], typeof(GameObject), true);
                quickTargets[1] = (GameObject)EditorGUILayout.ObjectField(
                    "End Target", quickTargets[1], typeof(GameObject), true);
                
                if (GUILayout.Button("Setup Endpoint Tracking"))
                {
                    SetupEndPointTracking();
                }
                
                EditorGUILayout.Space(5);
                
                // Auto detect scene objects
                if (GUILayout.Button("Auto Detect Scene Targets"))
                {
                    AutoDetectTargets();
                }
                
                // Clear settings
                if (GUILayout.Button("Clear All Tracking", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("Confirm", "Are you sure you want to clear all tracking settings?", "Yes", "Cancel"))
                    {
                        manager.ClearAllSetups();
                        EditorUtility.SetDirty(target);
                    }
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTrackingSetups()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Tracking Settings", EditorStyles.boldLabel);
            
            if (trackingSetups.arraySize == 0)
            {
                EditorGUILayout.HelpBox("No tracking relationships configured", MessageType.Info);
            }
            else
            {
                for (int i = 0; i < trackingSetups.arraySize; i++)
                {
                    DrawTrackingSetupItem(trackingSetups.GetArrayElementAtIndex(i), i);
                }
            }
            
            EditorGUILayout.Space(5);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("+ Add Tracking Setup"))
            {
                trackingSetups.InsertArrayElementAtIndex(trackingSetups.arraySize);
                var newElement = trackingSetups.GetArrayElementAtIndex(trackingSetups.arraySize - 1);
                InitializeTrackingSetup(newElement);
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawTrackingSetupItem(SerializedProperty setup, int index)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            EditorGUILayout.BeginHorizontal();
            
            // 摺疊標題
            var name = setup.FindPropertyRelative("name");
            var enabled = setup.FindPropertyRelative("enabled");
            
            enabled.boolValue = EditorGUILayout.Toggle(enabled.boolValue, GUILayout.Width(20));
            
            setup.isExpanded = EditorGUILayout.Foldout(setup.isExpanded, name.stringValue, true);
            
            // 刪除按鈕
            if (GUILayout.Button("×", GUILayout.Width(20)))
            {
                trackingSetups.DeleteArrayElementAtIndex(index);
                return;
            }
            
            EditorGUILayout.EndHorizontal();
            
            if (setup.isExpanded)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(name, new GUIContent("Name"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("controlPoint"), new GUIContent("Control Point"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("targetObject"), new GUIContent("Target Object"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("trackingMode"), new GUIContent("Tracking Mode"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("trackingSpeed"), new GUIContent("Tracking Speed"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("offset"), new GUIContent("Offset"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawShapeKeeperSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Shape Keeping", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(autoEnableShapeKeeper, new GUIContent("Auto Enable Shape Keeper"));
            
            if (autoEnableShapeKeeper.boolValue)
            {
                EditorGUILayout.PropertyField(defaultShapeMode, new GUIContent("Default Shape Mode"));
            }
            
            // 檢查是否有 ShapeKeeper
            var shapeKeeper = manager.GetComponent<SplineShapeKeeper>();
            if (shapeKeeper == null && autoEnableShapeKeeper.boolValue)
            {
                EditorGUILayout.HelpBox("SplineShapeKeeper component will be added automatically at runtime", MessageType.Info);
                
                if (GUILayout.Button("Add ShapeKeeper Now"))
                {
                    shapeKeeper = manager.gameObject.AddComponent<SplineShapeKeeper>();
                    EditorUtility.SetDirty(manager.gameObject);
                }
            }
            else if (shapeKeeper != null)
            {
                EditorGUILayout.HelpBox("SplineShapeKeeper component already exists", MessageType.None);
                
                if (GUILayout.Button("Re-detect Trackers"))
                {
                    shapeKeeper.DetectAndConfigureTrackers();
                    EditorUtility.SetDirty(shapeKeeper);
                }
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawDebugSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(showDebugInfo, new GUIContent("Show Debug Info"));
            EditorGUILayout.PropertyField(visualizeConnections, new GUIContent("Visualize Connections"));
            
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(manager.GetTrackingStatus(), MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("Apply All Settings", GUILayout.Height(30)))
            {
                manager.ApplyAllSetups();
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Reset All Speeds", GUILayout.Height(30)))
            {
                manager.SetAllTrackingSpeeds(5f);
                EditorUtility.SetDirty(target);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // Batch mode settings buttons
            if (GUILayout.Button("Set All to Direct"))
            {
                manager.SetAllTrackingModes(SplineTargetTracker.TrackingMode.Direct);
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Set All to Smooth"))
            {
                manager.SetAllTrackingModes(SplineTargetTracker.TrackingMode.Smooth);
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("Set All to Spring"))
            {
                manager.SetAllTrackingModes(SplineTargetTracker.TrackingMode.Spring);
                EditorUtility.SetDirty(target);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndVertical();
        }
        
        private void SetupEndPointTracking()
        {
            if (quickTargets[0] == null && quickTargets[1] == null)
            {
                EditorUtility.DisplayDialog("Error", "Please set at least one target object", "OK");
                return;
            }
            
            var splineManager = manager.GetComponent<SplineManager>();
            var controlPoints = splineManager.ControlPointTransforms;
            
            if (controlPoints.Count < 2)
            {
                EditorUtility.DisplayDialog("Error", "At least two control points required", "OK");
                return;
            }
            
            // 清除現有設定
            manager.ClearAllSetups();
            
            // 設定起點
            if (quickTargets[0] != null)
            {
                manager.AddTrackingSetup(controlPoints[0], quickTargets[0].transform);
            }
            
            // 設定終點
            if (quickTargets[1] != null)
            {
                manager.AddTrackingSetup(controlPoints[controlPoints.Count - 1], quickTargets[1].transform);
            }
            
            EditorUtility.SetDirty(target);
        }
        
        private void AutoDetectTargets()
        {
            // 找尋場景中可能的目標
            var allObjects = FindObjectsOfType<GameObject>();
            var potentialTargets = new System.Collections.Generic.List<GameObject>();
            
            foreach (var obj in allObjects)
            {
                // 排除曲線系統本身的物件
                if (obj.GetComponentInParent<SplineManager>() != null)
                    continue;
                
                // 找尋有特定標籤或名稱的物件
                if (obj.name.ToLower().Contains("player") || 
                    obj.name.ToLower().Contains("enemy") ||
                    obj.name.ToLower().Contains("target") ||
                    obj.tag == "Player" ||
                    obj.tag == "Enemy")
                {
                    potentialTargets.Add(obj);
                }
            }
            
            if (potentialTargets.Count > 0)
            {
                manager.AutoMatchTargets(potentialTargets.ToArray());
                EditorUtility.SetDirty(target);
                Debug.Log($"Auto-matched {potentialTargets.Count} targets");
            }
            else
            {
                EditorUtility.DisplayDialog("Info", "No suitable target objects found", "OK");
            }
        }
        
        private void InitializeTrackingSetup(SerializedProperty setup)
        {
            setup.FindPropertyRelative("name").stringValue = "New Tracking Setup";
            setup.FindPropertyRelative("enabled").boolValue = true;
            setup.FindPropertyRelative("trackingSpeed").floatValue = 5f;
            setup.FindPropertyRelative("trackingMode").enumValueIndex = (int)SplineTargetTracker.TrackingMode.Smooth;
        }
    }
}