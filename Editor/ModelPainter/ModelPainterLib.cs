using System.Collections.Generic;
using UnityEngine;

namespace RoxamiRPCore.Editor
{
    /// <summary>
    /// 可拖入常用 Prefab 的列表（ScriptableObject）。
    /// 在 Editor 窗口中会显示并可多选使用。
    /// Create via: Assets -> Create -> RoxamiTools -> Prefab Library
    /// </summary>
    [CreateAssetMenu(menuName = "RoxamiTools/Prefab Library", fileName = "PrefabLibrary")]
    public class ModelPainterLib : ScriptableObject
    {
        public List<GameObject> prefabs = new List<GameObject>();
    }
}
