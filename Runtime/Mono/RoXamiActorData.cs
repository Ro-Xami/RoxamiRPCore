using System;
using UnityEngine;

namespace RoxamiRPCore
{
    [ExecuteInEditMode]
    public class RoxamiActorData : MonoBehaviour
    {
        private static readonly int faceDirParamsID = Shader.PropertyToID("_ActorFaceDirParams");
        
        [SerializeField] Transform faceTransform;
        [SerializeField] private Material faceMaterial;

        private void OnEnable()
        {
            GetData();
        }

        private void OnValidate()
        {
            GetData();
        }

        private void Update()
        {
            if (faceTransform && faceMaterial)
            {
                faceMaterial.SetVector(faceDirParamsID, 
                    new Vector4(faceTransform.forward.x, faceTransform.forward.z, 
                    faceTransform.right.x, faceTransform.right.z));
            }
        }

        void GetData()
        {
            
        }
    }
}