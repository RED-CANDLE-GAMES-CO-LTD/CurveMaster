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
            
            // 標題
            EditorGUILayout.LabelField("曲線追蹤管理器", EditorStyles.boldLabel);
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
            EditorGUILayout.LabelField("全域設定", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(enableGlobalTracking, new GUIContent("啟用全域追蹤"));
            
            EditorGUI.BeginDisabledGroup(!enableGlobalTracking.boolValue);
            EditorGUILayout.PropertyField(globalSpeedMultiplier, new GUIContent("速度倍率"));
            EditorGUI.EndDisabledGroup();
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawQuickSetupTools()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            showQuickSetup = EditorGUILayout.Foldout(showQuickSetup, "快速設定工具", true);
            
            if (showQuickSetup)
            {
                EditorGUILayout.Space(5);
                
                // 兩端追蹤設定
                EditorGUILayout.LabelField("兩端追蹤設定", EditorStyles.miniBoldLabel);
                quickTargets[0] = (GameObject)EditorGUILayout.ObjectField(
                    "起點目標", quickTargets[0], typeof(GameObject), true);
                quickTargets[1] = (GameObject)EditorGUILayout.ObjectField(
                    "終點目標", quickTargets[1], typeof(GameObject), true);
                
                if (GUILayout.Button("設定兩端追蹤"))
                {
                    SetupEndPointTracking();
                }
                
                EditorGUILayout.Space(5);
                
                // 自動偵測場景物件
                if (GUILayout.Button("自動偵測場景目標"))
                {
                    AutoDetectTargets();
                }
                
                // 清除設定
                if (GUILayout.Button("清除所有追蹤", GUILayout.Height(25)))
                {
                    if (EditorUtility.DisplayDialog("確認", "確定要清除所有追蹤設定嗎？", "確定", "取消"))
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
            EditorGUILayout.LabelField("追蹤設定", EditorStyles.boldLabel);
            
            if (trackingSetups.arraySize == 0)
            {
                EditorGUILayout.HelpBox("尚未設定任何追蹤關係", MessageType.Info);
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
            if (GUILayout.Button("+ 新增追蹤設定"))
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
                
                EditorGUILayout.PropertyField(name, new GUIContent("名稱"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("controlPoint"), new GUIContent("控制點"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("targetObject"), new GUIContent("追蹤目標"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("trackingMode"), new GUIContent("追蹤模式"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("trackingSpeed"), new GUIContent("追蹤速度"));
                EditorGUILayout.PropertyField(setup.FindPropertyRelative("offset"), new GUIContent("偏移"));
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawShapeKeeperSettings()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("形狀維持", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(autoEnableShapeKeeper, new GUIContent("自動啟用形狀維持"));
            
            if (autoEnableShapeKeeper.boolValue)
            {
                EditorGUILayout.PropertyField(defaultShapeMode, new GUIContent("預設形狀模式"));
            }
            
            // 檢查是否有 ShapeKeeper
            var shapeKeeper = manager.GetComponent<SplineShapeKeeper>();
            if (shapeKeeper == null && autoEnableShapeKeeper.boolValue)
            {
                EditorGUILayout.HelpBox("將在執行時自動加入 SplineShapeKeeper 元件", MessageType.Info);
                
                if (GUILayout.Button("立即加入 ShapeKeeper"))
                {
                    shapeKeeper = manager.gameObject.AddComponent<SplineShapeKeeper>();
                    EditorUtility.SetDirty(manager.gameObject);
                }
            }
            else if (shapeKeeper != null)
            {
                EditorGUILayout.HelpBox("已有 SplineShapeKeeper 元件", MessageType.None);
                
                if (GUILayout.Button("重新偵測追蹤器"))
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
            EditorGUILayout.LabelField("偵錯", EditorStyles.boldLabel);
            
            EditorGUILayout.PropertyField(showDebugInfo, new GUIContent("顯示偵錯資訊"));
            EditorGUILayout.PropertyField(visualizeConnections, new GUIContent("視覺化連線"));
            
            if (Application.isPlaying)
            {
                EditorGUILayout.HelpBox(manager.GetTrackingStatus(), MessageType.None);
            }
            
            EditorGUILayout.EndVertical();
        }
        
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("操作", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("套用所有設定", GUILayout.Height(30)))
            {
                manager.ApplyAllSetups();
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("重設所有速度", GUILayout.Height(30)))
            {
                manager.SetAllTrackingSpeeds(5f);
                EditorUtility.SetDirty(target);
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            
            // 批次設定模式按鈕
            if (GUILayout.Button("全部設為 Direct"))
            {
                manager.SetAllTrackingModes(SplineTargetTracker.TrackingMode.Direct);
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("全部設為 Smooth"))
            {
                manager.SetAllTrackingModes(SplineTargetTracker.TrackingMode.Smooth);
                EditorUtility.SetDirty(target);
            }
            
            if (GUILayout.Button("全部設為 Spring"))
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
                EditorUtility.DisplayDialog("錯誤", "請至少設定一個目標物件", "確定");
                return;
            }
            
            var splineManager = manager.GetComponent<SplineManager>();
            var controlPoints = splineManager.ControlPointTransforms;
            
            if (controlPoints.Count < 2)
            {
                EditorUtility.DisplayDialog("錯誤", "需要至少兩個控制點", "確定");
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
                Debug.Log($"自動配對了 {potentialTargets.Count} 個目標");
            }
            else
            {
                EditorUtility.DisplayDialog("提示", "未找到合適的目標物件", "確定");
            }
        }
        
        private void InitializeTrackingSetup(SerializedProperty setup)
        {
            setup.FindPropertyRelative("name").stringValue = "新追蹤設定";
            setup.FindPropertyRelative("enabled").boolValue = true;
            setup.FindPropertyRelative("trackingSpeed").floatValue = 5f;
            setup.FindPropertyRelative("trackingMode").enumValueIndex = (int)SplineTargetTracker.TrackingMode.Smooth;
        }
    }
}