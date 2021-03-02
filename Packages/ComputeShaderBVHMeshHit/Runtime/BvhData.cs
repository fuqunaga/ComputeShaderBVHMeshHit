using UnityEngine;


namespace ComputeShaderBvhMeshHit
{
    [System.Serializable]
    public struct BvhData
    {
        public Vector3 min;
        public Vector3 max;

        public int leftIdx;
        public int rightIdx;

        public int triangleIdx; // -1 if data is not leaf
        public int triangleCount;

        public bool IsLeaf => triangleIdx >= 0;
    }
}