using UnityEngine;
using UnityEngine.Rendering;

namespace RoxamiRPCore
{
    public static class RoxamiCommonUtils
    {
        private static Mesh m_FullscreenMesh;
        public static Mesh FullScreenMesh
        {
            get
            {
                if (!m_FullscreenMesh)
                {
                    m_FullscreenMesh = CreateFullscreenMesh();
                }
                return m_FullscreenMesh;
            }
        }
        
        //From urp DeferredLights
        static Mesh CreateFullscreenMesh()
        {
            // TODO reorder for pre&post-transform cache optimisation.
            // Simple full-screen triangle.
            Vector3[] positions =
            {
                new Vector3(-1.0f,  1.0f, 0.0f),
                new Vector3(-1.0f, -3.0f, 0.0f),
                new Vector3(3.0f,  1.0f, 0.0f)
            };

            int[] indices = { 0, 1, 2 };

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt16;
            mesh.vertices = positions;
            mesh.triangles = indices;

            return mesh;
        }
    }
    
    public static class RoxamiShaderConst
    {
        public const string deferredToonShaderName = "Hidden/RoxamiRP/ToonDeferred";
    }
}