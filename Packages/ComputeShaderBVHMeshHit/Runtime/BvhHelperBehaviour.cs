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

        GraphicsBuffer bvhBuffer;
        GraphicsBuffer triangleBuffer;


        void Start()
        {
            (bvhBuffer, triangleBuffer) = bvhAsset.CreateBuffers();
        }

        void OnDestroy()
        {
            if (bvhBuffer != null) bvhBuffer.Release();
            if (triangleBuffer != null) triangleBuffer.Release();
        }

        void OnDrawGizmosSelected()
        {
            if (bvhAsset != null) bvhAsset.DrwaGizmo(gizmoDepth, gizmoOnlyLeafNode);
        }


        public void SetBuffersToComputShader(ComputeShader cs, int kernel)
        {
            cs.SetBuffer(kernel, ShaderParam.bvhBuffer, bvhBuffer);
            cs.SetBuffer(kernel, ShaderParam.triangleBuffer, triangleBuffer);
        }
    }
}