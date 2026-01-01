using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RoxamiRPCore.Editor
{
    public class ModelPainterWindow : EditorWindow
    {
        // ---------------------------
        // Prefab library
        // ---------------------------
        public ModelPainterLib modelPainterLib;
        private List<bool> prefabSelections = new List<bool>();

        // ---------------------------
        // Brush settings
        // ---------------------------
        int count = 10;
        float radius = 2f;

        Vector3 offset = Vector3.zero;
        Vector3 randomOffset = Vector3.zero;

        Vector3 rotation = Vector3.zero;
        Vector3 randomRotation = Vector3.zero;

        Vector3 scale = Vector3.one;
        Vector3 randomScale = Vector3.zero;

        int randomSeed = 1234; // 随机种子，保证预览和最终一致

        LayerMask collisionMask = ~0; // 可碰撞层

        enum PaintMode { CircleBrush, Line, Rectangle }
        PaintMode paintMode = PaintMode.CircleBrush;

        // ---------------------------
        // Internal state
        // ---------------------------
        List<GameObject> previewObjects = new List<GameObject>();
        List<TransformData> previewData = new List<TransformData>();

        Vector3 pointA, pointB;
        bool hasPointA = false;

        [MenuItem("RoxamiTools/Model Painter")]
        public static void Open() => GetWindow<ModelPainterWindow>("Model Painter");

        private void OnEnable() => SceneView.duringSceneGui += DuringSceneGUI;
        private void OnDisable()
        {
            SceneView.duringSceneGui -= DuringSceneGUI;
            ClearPreview();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Model Painter Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Ctrl + 左键: 生成模型或点\n" +
                "Ctrl + 右键: 修改随机种子\n" + 
                "Ctrl + 滚轮: 修改笔刷大小\n" + 
                "Circle/Rectangle/Line 模式: 点击场景生成点（预览），点击 Generate 按钮生成正式对象",
                MessageType.Info);

            // Prefab library
            var newLib = (ModelPainterLib)EditorGUILayout.ObjectField("Prefab Library", modelPainterLib, typeof(ModelPainterLib), false);
            if (newLib != modelPainterLib)
            {
                modelPainterLib = newLib;
                InitPrefabSelection();
            }

            if (modelPainterLib == null || modelPainterLib.prefabs.Count == 0)
            {
                EditorGUILayout.HelpBox("请指定一个 PrefabLibrary，并添加至少一个 Prefab。", MessageType.Warning);
                return;
            }

            DrawPrefabSelector();

            EditorGUILayout.Space(10);
            count = EditorGUILayout.IntSlider("Count", count, 1, 200);
            radius = EditorGUILayout.FloatField("Radius", radius);

            paintMode = (PaintMode)EditorGUILayout.EnumPopup("Mode", paintMode);
            collisionMask = LayerMaskField("Collision Layer", collisionMask);

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Offset / Random", EditorStyles.boldLabel);
            offset = EditorGUILayout.Vector3Field("Offset", offset);
            randomOffset = EditorGUILayout.Vector3Field("Random Offset", randomOffset);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Rotation / Random", EditorStyles.boldLabel);
            rotation = EditorGUILayout.Vector3Field("Rotation", rotation);
            randomRotation = EditorGUILayout.Vector3Field("Random Rotation", randomRotation);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Scale / Random", EditorStyles.boldLabel);
            scale = EditorGUILayout.Vector3Field("Scale", scale);
            randomScale = EditorGUILayout.Vector3Field("Random Scale", randomScale);

            EditorGUILayout.Space(20);
            if (GUILayout.Button("Generate"))
            {
                GenerateFinal();
            }
            if (GUILayout.Button("Clear Preview"))
            {
                ClearPreview();
            }
        }

        // ---------------------------
        // Prefab Selection
        // ---------------------------
        void InitPrefabSelection()
        {
            prefabSelections.Clear();
            if (modelPainterLib == null) return;
            foreach (var _ in modelPainterLib.prefabs)
                prefabSelections.Add(false);
        }

        void DrawPrefabSelector()
        {
            EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);
            for (int i = 0; i < modelPainterLib.prefabs.Count; i++)
            {
                if (modelPainterLib.prefabs[i] == null)
                {
                    EditorGUILayout.HelpBox("PrefabLibrary 中存在空引用，请检查！", MessageType.Warning);
                    continue;
                }
                EditorGUILayout.BeginHorizontal();
                prefabSelections[i] = EditorGUILayout.Toggle(prefabSelections[i], GUILayout.Width(20));
                EditorGUILayout.ObjectField(modelPainterLib.prefabs[i], typeof(GameObject), false);
                EditorGUILayout.EndHorizontal();
            }
        }

        List<GameObject> GetSelectedPrefabs()
        {
            List<GameObject> list = new List<GameObject>();
            if (modelPainterLib == null) return list;
            for (int i = 0; i < modelPainterLib.prefabs.Count; i++)
                if (prefabSelections.Count > i && prefabSelections[i] && modelPainterLib.prefabs[i] != null)
                    list.Add(modelPainterLib.prefabs[i]);
            return list;
        }

        // ---------------------------
        // Scene GUI
        // ---------------------------
        void DuringSceneGUI(SceneView sceneView)
        {
            Event e = Event.current;
            var selectedPrefabs = GetSelectedPrefabs();
            if (selectedPrefabs.Count == 0) { ClearPreview(); return; }

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, collisionMask.value))
            {
                ClearPreview();
                return;
            }

            // ---------------------------
            // Ctrl + 滚轮修改笔刷半径
            // ---------------------------
            if (paintMode == PaintMode.CircleBrush && e.control && e.type == EventType.ScrollWheel)
            {
                float delta = -e.delta.y * 0.1f; // 放大缩小速度，可调
                radius = Mathf.Max(0.1f, radius + delta);
                e.Use();
            }

            // Draw brush
            Handles.color = Color.green;
            if (paintMode == PaintMode.CircleBrush)
                Handles.DrawWireDisc(hit.point, hit.normal, radius);

            // 设置点（Line/Rectangle模式）
            if (paintMode == PaintMode.Line || paintMode == PaintMode.Rectangle)
            {
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    if (!hasPointA) { pointA = hit.point; hasPointA = true; }
                    else { pointB = hit.point; hasPointA = false; }
                }
            }

            // Ctrl+Left生成正式模型
            if (e.type == EventType.MouseDown && e.button == 0 && e.control)
            {
                previewData = GeneratePreviewData(hit.point);
                CreateFinal(previewData, selectedPrefabs);
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 1 && e.control)
            {
                randomSeed += 1;
            }
            else
            {
                // 仅预览
                previewData = GeneratePreviewData(hit.point);
                ClearPreview();
                CreatePreview(previewData, selectedPrefabs);
            }

            sceneView.Repaint();
        }


        struct TransformData { public Vector3 pos; public Quaternion rot; public Vector3 scale; }

        List<TransformData> GeneratePreviewData(Vector3 center)
        {
            Random.InitState(randomSeed);
            List<TransformData> list = new List<TransformData>();

            if (paintMode == PaintMode.CircleBrush)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 r = Random.insideUnitCircle * radius;
                    Vector3 pos = center + new Vector3(r.x, 0, r.y);
                    list.Add(MakeTransformData(pos));
                }
            }
            else if (paintMode == PaintMode.Line && !hasPointA)
            {
                for (int i = 0; i < count; i++)
                {
                    float t = Random.value;
                    Vector3 pos = Vector3.Lerp(pointA, pointB, t);
                    list.Add(MakeTransformData(pos));
                }
            }
            else if (paintMode == PaintMode.Rectangle && !hasPointA)
            {
                Vector3 centerRect = (pointA + pointB) * 0.5f;
                Vector3 size = new Vector3(Mathf.Abs(pointA.x - pointB.x), 0, Mathf.Abs(pointA.z - pointB.z));
                for (int i = 0; i < count; i++)
                {
                    float rx = Random.Range(-size.x / 2, size.x / 2);
                    float rz = Random.Range(-size.z / 2, size.z / 2);
                    Vector3 pos = centerRect + new Vector3(rx, 0, rz);
                    list.Add(MakeTransformData(pos));
                }
            }

            return list;
        }

        TransformData MakeTransformData(Vector3 pos)
        {
            Vector3 finalPos = pos + offset + new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                Random.Range(-randomOffset.y, randomOffset.y),
                Random.Range(-randomOffset.z, randomOffset.z));

            Quaternion rot = Quaternion.Euler(rotation + new Vector3(
                Random.Range(-randomRotation.x, randomRotation.x),
                Random.Range(-randomRotation.y, randomRotation.y),
                Random.Range(-randomRotation.z, randomRotation.z)));

            Vector3 finalScale = scale + new Vector3(
                Random.Range(-randomScale.x, randomScale.x),
                Random.Range(-randomScale.y, randomScale.y),
                Random.Range(-randomScale.z, randomScale.z));

            return new TransformData { pos = finalPos, rot = rot, scale = finalScale };
        }

        void CreatePreview(List<TransformData> datas, List<GameObject> availablePrefabs)
        {
            foreach (var d in datas)
            {
                GameObject prefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                obj.hideFlags = HideFlags.HideAndDontSave;
                obj.transform.position = d.pos;
                obj.transform.rotation = d.rot;
                obj.transform.localScale = d.scale;
                previewObjects.Add(obj);
            }
        }

        void ClearPreview()
        {
            foreach (var obj in previewObjects)
                if (obj != null) DestroyImmediate(obj);
            previewObjects.Clear();
        }

        void CreateFinal(List<TransformData> datas, List<GameObject> availablePrefabs)
        {
            Undo.IncrementCurrentGroup();
            foreach (var d in datas)
            {
                GameObject prefab = availablePrefabs[Random.Range(0, availablePrefabs.Count)];
                GameObject obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                obj.transform.position = d.pos;
                obj.transform.rotation = d.rot;
                obj.transform.localScale = d.scale;
                Undo.RegisterCreatedObjectUndo(obj, "Paint Model");
            }
        }

        void GenerateFinal()
        {
            var selectedPrefabs = GetSelectedPrefabs();
            if (selectedPrefabs.Count == 0) return;
            CreateFinal(previewData, selectedPrefabs);
            ClearPreview();
            previewData.Clear();
        }

        // LayerMask helper
        static LayerMask LayerMaskField(string label, LayerMask selected)
        {
            var layers = UnityEditorInternal.InternalEditorUtility.layers;
            int mask = 0;
            for (int i = 0; i < layers.Length; i++)
                if ((selected.value & (1 << LayerMask.NameToLayer(layers[i]))) != 0)
                    mask |= (1 << i);

            int newMask = EditorGUILayout.MaskField(label, mask, layers);
            if (newMask == mask) return selected;

            int changedMask = 0;
            for (int i = 0; i < layers.Length; i++)
                if ((newMask & (1 << i)) != 0)
                    changedMask |= (1 << LayerMask.NameToLayer(layers[i]));
            selected.value = changedMask;
            return selected;
        }
    }
}


