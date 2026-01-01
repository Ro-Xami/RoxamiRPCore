using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

namespace RoxamiRPCore.Editor
{
    /// <summary>
    /// 组件移除工具 - 编辑器窗口
    /// </summary>
    public class ComponentRemover : EditorWindow
    {
        // 组件类型枚举
        [Flags]
        public enum ComponentType
        {
            None = 0,
            MissingScript = 1 << 0,
            MeshRenderer = 1 << 1,
            MeshFilter = 1 << 2,
            BoxCollider = 1 << 3,
            SphereCollider = 1 << 4,
            CapsuleCollider = 1 << 5,
            Rigidbody = 1 << 6,
            Animator = 1 << 7,
            AudioSource = 1 << 8,
            ParticleSystem = 1 << 9,
            Light = 1 << 10,
            Camera = 1 << 11,
            Canvas = 1 << 12,
            CanvasRenderer = 1 << 13,
            RectTransform = 1 << 14,
            Image = 1 << 15,
            Text = 1 << 16,
            Button = 1 << 17,
            Slider = 1 << 18,
            Scrollbar = 1 << 19,
            Dropdown = 1 << 20,
            InputField = 1 << 21,
            Toggle = 1 << 22,
            ScrollRect = 1 << 23,
            MeshCollider = 1 << 24,
            All = ~0
        }

        // 当前选择的组件类型
        private ComponentType selectedComponents = ComponentType.None;
        
        // 多选预制体列表
        private List<GameObject> selectedPrefabs = new List<GameObject>();
        
        // 滚动位置
        private Vector2 scrollPosition;
        private Vector2 prefabListScrollPosition;
        
        // 是否包含子对象
        private bool includeChildren = true;
        
        // 是否显示详细信息
        private bool showDetails = false;
        
        // 移除统计
        private int removedCount = 0;
        private int processedObjectsCount = 0;
        private int processedPrefabsCount = 0;

        // 添加菜单项
        [MenuItem("RoxamiTools/组件移除工具")]
        public static void ShowWindow()
        {
            GetWindow<ComponentRemover>("组件移除工具");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // 标题
            EditorGUILayout.LabelField("组件移除工具", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("选择要移除的组件类型，然后选择多个预制体进行批量移除操作。", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 组件选择区域
            DrawComponentSelection();
            
            EditorGUILayout.Space(10);
            
            // 预制体多选区域
            DrawMultiPrefabSelection();
            
            EditorGUILayout.Space(10);
            
            // 选项区域
            DrawOptions();
            
            EditorGUILayout.Space(20);
            
            // 操作按钮
            DrawActionButtons();
            
            EditorGUILayout.Space(10);
            
            // 统计信息
            DrawStatistics();
        }

        /// <summary>
        /// 绘制组件选择区域
        /// </summary>
        private void DrawComponentSelection()
        {
            EditorGUILayout.LabelField("选择要移除的组件类型:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            
            // 获取所有枚举值（排除 None 和 All）
            var componentTypes = Enum.GetValues(typeof(ComponentType))
                .Cast<ComponentType>()
                .Where(ct => ct != ComponentType.None && ct != ComponentType.All)
                .ToList();
            
            // 计算每行显示的列数
            int columns = 3;
            int currentColumn = 0;
            
            EditorGUILayout.BeginHorizontal();
            
            foreach (var componentType in componentTypes)
            {
                if (currentColumn >= columns)
                {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    currentColumn = 0;
                }
                
                bool isSelected = (selectedComponents & componentType) == componentType;
                bool newSelection = EditorGUILayout.ToggleLeft(componentType.ToString(), isSelected, GUILayout.Width(150));
                
                if (newSelection != isSelected)
                {
                    if (newSelection)
                        selectedComponents |= componentType;
                    else
                        selectedComponents &= ~componentType;
                }
                
                currentColumn++;
            }
            
            // 填充剩余空间
            while (currentColumn < columns)
            {
                EditorGUILayout.LabelField("", GUILayout.Width(150));
                currentColumn++;
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.EndScrollView();
            
            // 快速选择按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全选", GUILayout.Width(80)))
            {
                selectedComponents = ComponentType.All;
            }
            if (GUILayout.Button("清空", GUILayout.Width(80)))
            {
                selectedComponents = ComponentType.None;
            }
            if (GUILayout.Button("选择常用", GUILayout.Width(80)))
            {
                selectedComponents = ComponentType.MissingScript | ComponentType.MeshRenderer | 
                                    ComponentType.MeshFilter | ComponentType.BoxCollider;
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制多选预制体区域
        /// </summary>
        private void DrawMultiPrefabSelection()
        {
            EditorGUILayout.LabelField("选择多个预制体:", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            
            if (GUILayout.Button("从场景选择多个", GUILayout.Width(120)))
            {
                AddSelectedObjectsFromScene();
            }
            
            if (GUILayout.Button("清空列表", GUILayout.Width(80)))
            {
                selectedPrefabs.Clear();
            }
            
            if (GUILayout.Button("添加预制体", GUILayout.Width(80)))
            {
                AddPrefabManually();
            }
            
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            
            // 显示已选择的预制体列表
            EditorGUILayout.LabelField($"已选择 {selectedPrefabs.Count} 个预制体:");
            
            if (selectedPrefabs.Count > 0)
            {
                prefabListScrollPosition = EditorGUILayout.BeginScrollView(prefabListScrollPosition, GUILayout.Height(150));
                
                for (int i = 0; i < selectedPrefabs.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    
                    // 显示预制体信息
                    GameObject prefab = selectedPrefabs[i];
                    EditorGUILayout.ObjectField($"预制体 {i + 1}", prefab, typeof(GameObject), false);
                    
                    // 移除按钮
                    if (GUILayout.Button("移除", GUILayout.Width(60)))
                    {
                        selectedPrefabs.RemoveAt(i);
                        i--; // 调整索引
                        continue;
                    }
                    
                    EditorGUILayout.EndHorizontal();
                }
                
                EditorGUILayout.EndScrollView();
                
                // 显示统计信息
                int prefabCount = selectedPrefabs.Count;
                int nonPrefabCount = selectedPrefabs.Count(p => PrefabUtility.GetPrefabAssetType(p) == PrefabAssetType.NotAPrefab);
                
                if (nonPrefabCount > 0)
                {
                    EditorGUILayout.HelpBox($"警告: {nonPrefabCount} 个对象不是预制体！", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.HelpBox($"已选择 {prefabCount} 个有效预制体", MessageType.Info);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("请选择至少一个预制体\n\n点击'从场景选择多个'按钮选择场景中的对象\n或点击'添加预制体'按钮手动添加", MessageType.None);
            }
        }

        /// <summary>
        /// 绘制选项区域
        /// </summary>
        private void DrawOptions()
        {
            EditorGUILayout.LabelField("选项:", EditorStyles.boldLabel);
            
            includeChildren = EditorGUILayout.Toggle("包含子对象", includeChildren);
            showDetails = EditorGUILayout.Toggle("显示详细信息", showDetails);
            
            if (showDetails)
            {
                EditorGUILayout.HelpBox($"当前选择的组件: {selectedComponents}", MessageType.Info);
            }
        }

        /// <summary>
        /// 绘制操作按钮
        /// </summary>
        private void DrawActionButtons()
        {
            EditorGUILayout.BeginHorizontal();
            
            GUI.enabled = selectedComponents != ComponentType.None && selectedPrefabs.Count > 0;
            
            if (GUILayout.Button("批量移除组件", GUILayout.Height(40)))
            {
                RemoveComponents();
            }
            
            if (GUILayout.Button("预览移除", GUILayout.Height(40)))
            {
                PreviewRemoval();
            }
            
            GUI.enabled = true;
            
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 绘制统计信息
        /// </summary>
        private void DrawStatistics()
        {
            EditorGUILayout.LabelField("统计信息:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"上次移除组件数量: {removedCount}");
            EditorGUILayout.LabelField($"上次处理对象数量: {processedObjectsCount}");
            EditorGUILayout.LabelField($"上次处理预制体数量: {processedPrefabsCount}");
        }

        /// <summary>
        /// 从场景中选择多个对象
        /// </summary>
        private void AddSelectedObjectsFromScene()
        {
            GameObject[] selectedObjects = Selection.gameObjects;
            if (selectedObjects == null || selectedObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("提示", "请先在场景中选择一个或多个游戏对象", "确定");
                return;
            }
            
            int addedCount = 0;
            foreach (GameObject obj in selectedObjects)
            {
                if (!selectedPrefabs.Contains(obj))
                {
                    selectedPrefabs.Add(obj);
                    addedCount++;
                }
            }
            
            EditorUtility.DisplayDialog("完成", $"已添加 {addedCount} 个对象到列表", "确定");
        }
        
        /// <summary>
        /// 手动添加预制体
        /// </summary>
        private void AddPrefabManually()
        {
            string path = EditorUtility.OpenFilePanel("选择预制体", "Assets", "prefab");
            if (!string.IsNullOrEmpty(path))
            {
                // 将路径转换为相对路径
                if (path.StartsWith(Application.dataPath))
                {
                    path = "Assets" + path.Substring(Application.dataPath.Length);
                }
                
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null)
                {
                    if (!selectedPrefabs.Contains(prefab))
                    {
                        selectedPrefabs.Add(prefab);
                        EditorUtility.DisplayDialog("完成", $"已添加预制体: {prefab.name}", "确定");
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "该预制体已在列表中", "确定");
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("错误", "无法加载预制体", "确定");
                }
            }
        }
        
        /// <summary>
        /// 移除组件
        /// </summary>
        private void RemoveComponents()
        {
            if (selectedPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择至少一个预制体", "确定");
                return;
            }
            
            if (selectedComponents == ComponentType.None)
            {
                EditorUtility.DisplayDialog("错误", "请先选择要移除的组件类型", "确定");
                return;
            }
            
            // 过滤出有效的预制体
            List<GameObject> validPrefabs = selectedPrefabs
                .Where(p => p != null && PrefabUtility.GetPrefabAssetType(p) != PrefabAssetType.NotAPrefab)
                .ToList();
                
            if (validPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有有效的预制体可供处理", "确定");
                return;
            }
            
            // 确认对话框
            if (!EditorUtility.DisplayDialog("确认", 
                $"确定要从 {validPrefabs.Count} 个预制体中移除选中的组件吗？\n\n" +
                $"包含子对象: {includeChildren}\n" +
                $"组件类型: {selectedComponents}", 
                "确定", "取消"))
            {
                return;
            }
            
            removedCount = 0;
            processedObjectsCount = 0;
            processedPrefabsCount = validPrefabs.Count;
            
            // 处理每个预制体
            foreach (GameObject prefab in validPrefabs)
            {
                RemoveComponentsFromPrefab(prefab);
            }
            
            // 刷新编辑器
            AssetDatabase.Refresh();
            
            // 显示完成消息
            EditorUtility.DisplayDialog("完成", 
                $"批量组件移除完成！\n" +
                $"处理预制体数量: {processedPrefabsCount}\n" +
                $"处理对象数量: {processedObjectsCount}\n" +
                $"移除组件数量: {removedCount}", 
                "确定");
            
            // 刷新界面
            Repaint();
        }
        
        /// <summary>
        /// 从单个预制体移除组件
        /// </summary>
        private void RemoveComponentsFromPrefab(GameObject prefab)
        {
            // 开始记录撤销操作
            Undo.RegisterCompleteObjectUndo(prefab, "移除组件");
            
            // 收集要处理的对象
            List<GameObject> objectsToProcess = new List<GameObject>();
            objectsToProcess.Add(prefab);
            
            if (includeChildren)
            {
                objectsToProcess.AddRange(prefab.GetComponentsInChildren<Transform>(true)
                    .Select(t => t.gameObject)
                    .Distinct());
            }
            
            processedObjectsCount += objectsToProcess.Count;
            
            // 处理每个对象
            foreach (GameObject obj in objectsToProcess)
            {
                RemoveComponentsFromObject(obj);
            }
            
            // 保存预制体
            if (PrefabUtility.IsPartOfAnyPrefab(prefab))
            {
                PrefabUtility.SavePrefabAsset(prefab);
            }
            
            // 刷新编辑器
            EditorUtility.SetDirty(prefab);
        }

        /// <summary>
        /// 从单个对象移除组件
        /// </summary>
        private void RemoveComponentsFromObject(GameObject obj)
        {
            // 处理 Missing Scripts
            if ((selectedComponents & ComponentType.MissingScript) == ComponentType.MissingScript)
            {
                RemoveMissingScripts(obj);
            }
            
            // 处理其他组件类型
            foreach (ComponentType componentType in Enum.GetValues(typeof(ComponentType)))
            {
                if (componentType == ComponentType.None || 
                    componentType == ComponentType.All || 
                    componentType == ComponentType.MissingScript)
                    continue;
                
                if ((selectedComponents & componentType) == componentType)
                {
                    RemoveComponentByType(obj, componentType);
                }
            }
        }

        /// <summary>
        /// 移除缺失的脚本
        /// </summary>
        private void RemoveMissingScripts(GameObject obj)
        {
            // 使用 Unity 2018.3+ 的方法移除缺失的脚本
            int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
            
            if (missingScriptCount > 0)
            {
                // 记录撤销操作
                Undo.RegisterCompleteObjectUndo(obj, "移除缺失脚本");
                
                // 移除缺失的脚本
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                removedCount += missingScriptCount;
                
                // 刷新对象
                EditorUtility.SetDirty(obj);
            }
        }

        /// <summary>
        /// 根据组件类型移除组件
        /// </summary>
        private void RemoveComponentByType(GameObject obj, ComponentType componentType)
        {
            Type typeToRemove = GetComponentType(componentType);
            if (typeToRemove == null)
                return;
            
            Component[] components = obj.GetComponents(typeToRemove);
            foreach (Component component in components)
            {
                if (component != null)
                {
                    Undo.DestroyObjectImmediate(component);
                    removedCount++;
                }
            }
            
            // 如果是包含子对象的情况，已经在上层处理了
        }

        /// <summary>
        /// 根据枚举值获取组件类型
        /// </summary>
        private Type GetComponentType(ComponentType componentType)
        {
            switch (componentType)
            {
                case ComponentType.MeshRenderer: return typeof(MeshRenderer);
                case ComponentType.MeshFilter: return typeof(MeshFilter);
                case ComponentType.BoxCollider: return typeof(BoxCollider);
                case ComponentType.SphereCollider: return typeof(SphereCollider);
                case ComponentType.CapsuleCollider: return typeof(CapsuleCollider);
                case ComponentType.Rigidbody: return typeof(Rigidbody);
                case ComponentType.Animator: return typeof(Animator);
                case ComponentType.AudioSource: return typeof(AudioSource);
                case ComponentType.ParticleSystem: return typeof(ParticleSystem);
                case ComponentType.Light: return typeof(Light);
                case ComponentType.Camera: return typeof(Camera);
                case ComponentType.Canvas: return typeof(Canvas);
                case ComponentType.CanvasRenderer: return typeof(CanvasRenderer);
                case ComponentType.RectTransform: return typeof(RectTransform);
                case ComponentType.Image: return typeof(UnityEngine.UI.Image);
                case ComponentType.Text: return typeof(UnityEngine.UI.Text);
                case ComponentType.Button: return typeof(UnityEngine.UI.Button);
                case ComponentType.Slider: return typeof(UnityEngine.UI.Slider);
                case ComponentType.Scrollbar: return typeof(UnityEngine.UI.Scrollbar);
                case ComponentType.Dropdown: return typeof(UnityEngine.UI.Dropdown);
                case ComponentType.InputField: return typeof(UnityEngine.UI.InputField);
                case ComponentType.Toggle: return typeof(UnityEngine.UI.Toggle);
                case ComponentType.ScrollRect: return typeof(UnityEngine.UI.ScrollRect);
                default: return null;
            }
        }

        /// <summary>
        /// 预览移除操作
        /// </summary>
        private void PreviewRemoval()
        {
            if (selectedPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "请先选择至少一个预制体", "确定");
                return;
            }
            
            if (selectedComponents == ComponentType.None)
            {
                EditorUtility.DisplayDialog("错误", "请先选择要移除的组件类型", "确定");
                return;
            }
            
            // 过滤出有效的预制体
            List<GameObject> validPrefabs = selectedPrefabs
                .Where(p => p != null && PrefabUtility.GetPrefabAssetType(p) != PrefabAssetType.NotAPrefab)
                .ToList();
                
            if (validPrefabs.Count == 0)
            {
                EditorUtility.DisplayDialog("错误", "没有有效的预制体可供预览", "确定");
                return;
            }
            
            int previewRemovedCount = 0;
            int previewProcessedCount = 0;
            int previewPrefabCount = validPrefabs.Count;
            
            // 计算预览数量
            foreach (GameObject prefab in validPrefabs)
            {
                // 收集要处理的对象
                List<GameObject> objectsToProcess = new List<GameObject>();
                objectsToProcess.Add(prefab);
                
                if (includeChildren)
                {
                    objectsToProcess.AddRange(prefab.GetComponentsInChildren<Transform>(true)
                        .Select(t => t.gameObject)
                        .Distinct());
                }
                
                previewProcessedCount += objectsToProcess.Count;
                
                // 计算每个对象的组件数量
                foreach (GameObject obj in objectsToProcess)
                {
                    // 处理 Missing Scripts
                    if ((selectedComponents & ComponentType.MissingScript) == ComponentType.MissingScript)
                    {
                        int missingScriptCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);
                        previewRemovedCount += missingScriptCount;
                    }
                    
                    // 处理其他组件
                    foreach (ComponentType componentType in Enum.GetValues(typeof(ComponentType)))
                    {
                        if (componentType == ComponentType.None || 
                            componentType == ComponentType.All || 
                            componentType == ComponentType.MissingScript)
                            continue;
                        
                        if ((selectedComponents & componentType) == componentType)
                        {
                            Type typeToRemove = GetComponentType(componentType);
                            if (typeToRemove != null)
                            {
                                Component[] components = obj.GetComponents(typeToRemove);
                                previewRemovedCount += components.Length;
                            }
                        }
                    }
                }
            }
            
            // 显示预览结果
            EditorUtility.DisplayDialog("预览结果", 
                $"预览移除统计:\n\n" +
                $"预制体数量: {previewPrefabCount}\n" +
                $"处理对象数量: {previewProcessedCount}\n" +
                $"预计移除组件数量: {previewRemovedCount}\n\n" +
                $"组件类型: {selectedComponents}", 
                "确定");
        }
    }
}


