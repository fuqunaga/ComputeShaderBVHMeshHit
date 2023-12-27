using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;


namespace ComputeShaderBvhMeshHit
{
    public class BvhAsset : ScriptableObject
    {
        [FormerlySerializedAs("bvhDatas")] public List<BvhData> bvhDataList;
        public List<Triangle> triangles;


        public (GraphicsBuffer, GraphicsBuffer) CreateBuffers()
        {
            var bvhBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bvhDataList.Count, Marshal.SizeOf<BvhData>());
            bvhBuffer.SetData(bvhDataList);

            var triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, triangles.Count, Marshal.SizeOf<Triangle>());
            triangleBuffer.SetData(triangles);

            return (bvhBuffer, triangleBuffer);
        }


        public void DrawGizmo(int gizmoDepth, bool gizmoOnlyLeafNode = false)
        {
            if (bvhDataList != null)
            {
                DrawBvhGizmo(0, gizmoDepth, gizmoOnlyLeafNode);
            }
        }

        private void DrawBvhGizmo(int idx, int gizmoDepth, bool gizmoOnlyLeafNode, int recursiveCount = 0)
        {
            if (idx < 0 || bvhDataList.Count <= idx) return;

            var data = bvhDataList[idx];

            if (recursiveCount == gizmoDepth)
            {
                if (data.IsLeaf)
                {
                    Gizmos.color = Color.red;
                    for (var i = 0; i < data.triangleCount; ++i)
                    {
                        var tri = triangles[i + data.triangleIdx];
                        Gizmos.DrawLine(tri.pos0, tri.pos1);
                        Gizmos.DrawLine(tri.pos0, tri.pos2);
                        Gizmos.DrawLine(tri.pos1, tri.pos2);
                    }
                }

                if (!gizmoOnlyLeafNode || data.IsLeaf)
                {
                    var bounds = new Bounds() { min = data.min, max = data.max };

                    Gizmos.color = data.IsLeaf ? Color.cyan : Color.green;
                    Gizmos.DrawWireCube(bounds.center, bounds.size);
                }
            }
            else if (!data.IsLeaf)
            {
                DrawBvhGizmo(data.leftIdx, gizmoDepth, gizmoOnlyLeafNode, recursiveCount + 1);
                DrawBvhGizmo(data.rightIdx, gizmoDepth, gizmoOnlyLeafNode, recursiveCount + 1);
            }
        }
    }
}