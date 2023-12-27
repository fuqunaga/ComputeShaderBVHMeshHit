using System;
using UnityEngine;

namespace ComputeShaderBvhMeshHit
{
    public class BvhHelperBehaviour : MonoBehaviour
    {
        public static class ShaderParam
        {
            public static int bvhBuffer = Shader.PropertyToID("bvhBuffer");
            public static int triangleBuffer = Shader.PropertyToID("triangleBuffer");
        }

        public BvhAsset bvhAsset;

        public int gizmoDepth;
        public bool gizmoOnlyLeafNode;

        private GraphicsBuffer _bvhBuffer;
        private GraphicsBuffer _triangleBuffer;


        private void Start()
        {
            (_bvhBuffer, _triangleBuffer) = bvhAsset.CreateBuffers();
        }

        private void OnDestroy()
        {
            _bvhBuffer?.Release();
            _triangleBuffer?.Release();
        }

        private void OnDrawGizmosSelected()
        {
            if (bvhAsset != null) bvhAsset.DrawGizmo(gizmoDepth, gizmoOnlyLeafNode);
        }

        [Obsolete("Use SetBuffersToComputeShader instead")]
        // ReSharper disable once IdentifierTypo
        public void SetBuffersToComputShader(ComputeShader cs, int kernel) => SetBuffersToComputeShader(cs, kernel);

        public void SetBuffersToComputeShader(ComputeShader cs, int kernel)
        {
            cs.SetBuffer(kernel, ShaderParam.bvhBuffer, _bvhBuffer);
            cs.SetBuffer(kernel, ShaderParam.triangleBuffer, _triangleBuffer);
        }
    }
}