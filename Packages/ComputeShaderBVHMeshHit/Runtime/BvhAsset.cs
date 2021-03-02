using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;


namespace ComputeShaderBvhMeshHit
{
    public class BvhAsset : ScriptableObject
    {
        public List<BvhData> bvhDatas;
        public List<Triangle> triangles;


        public (GraphicsBuffer, GraphicsBuffer) CreateBuffers()
        {
            var bvhBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bvhDatas.Count, Marshal.SizeOf<BvhData>());
            bvhBuffer.SetData(bvhDatas);

            var triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, triangles.Count, Marshal.SizeOf<Triangle>());
            triangleBuffer.SetData(triangles);

            return (bvhBuffer, triangleBuffer);
        }


        public void DrwaGizmo(int gizmoDepth, bool gizmoOnlyLeafNode = false)
        {
            if (bvhDatas != null)
            {
                DrawBvhGizmo(0, gizmoDepth, gizmoOnlyLeafNode);
            }
        }

        void DrawBvhGizmo(int idx, int gizmoDepth, bool gizmoOnlyLeafNode, int recursiveCount = 0)
        {
            if (idx < 0 || bvhDatas.Count <= idx) return;

            var data = bvhDatas[idx];

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