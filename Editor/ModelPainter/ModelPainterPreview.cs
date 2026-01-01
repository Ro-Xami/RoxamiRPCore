using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

namespace RoxamiRPCore.Editor
{
    /// <summary>
    /// 真实 Mesh + 材质预览，不实例化 GameObject
    /// </summary>
    public static class ModelPainterPreview
    {
        public static void DrawMesh(Mesh mesh, Material[] materials, Matrix4x4 trs)
        {
            if (mesh == null || materials == null || materials.Length == 0)
                return;

            var sceneView = SceneView.currentDrawingSceneView;
            if (!sceneView) return;

            Camera cam = sceneView.camera;
            if (!cam) return;

            int subMeshCount = mesh.subMeshCount;

            for (int i = 0; i < subMeshCount; i++)
            {
                Material mat = materials[Mathf.Min(i, materials.Length - 1)];

                if (mat == null) continue;

                Graphics.DrawMesh(
                    mesh,
                    trs,
                    mat,
                    0,
                    cam,
                    i,
                    null,
                    ShadowCastingMode.Off,
                    false
                );
            }
        }
    }
}
