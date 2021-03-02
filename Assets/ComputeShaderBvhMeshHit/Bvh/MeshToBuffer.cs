using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public class MeshToBuffer : MonoBehaviour
{
    #region Type Define

    public struct TriangleBounds
    {
        public Bounds bounds;
        public int triangleIndex;
    }

    #endregion


    public List<GameObject> targets;

    public int splitTestCount = 1024;

    GraphicsBuffer bvhBuffer;
    GraphicsBuffer triangleBuffer;

#if UNITY_EDITOR
    [Header("Gizmos")]
    public int gizmoBvhDepth;
    public bool drawOnlyLeafNode;

    List<BvhData> bvhDatasForDebug;
    List<Triangle> trianglesForDebug;
#endif

    public GraphicsBuffer BvhBuffer
    {
        get
        {
            if (bvhBuffer == null) CreateBuffer();
            return bvhBuffer;
        }
    }

    public GraphicsBuffer TriangleBuffer
    {
        get
        {
            if (triangleBuffer == null) CreateBuffer();
            return triangleBuffer;
        }
    }


    #region Unity

    void OnDestroy()
    {
        if (triangleBuffer != null) triangleBuffer.Dispose();
        if (bvhBuffer != null) bvhBuffer.Dispose();
    }


    void OnDrawGizmosSelected()
    {
        if (bvhDatasForDebug != null)
        {
            DrawBvhGizmo();
        }
    }

    #endregion



    void CreateBuffer()
    {
        var triangles = CreateTriangles();
        var rootNode = CreateBvh(triangles);
        var (bvhDatas, triangleIndexes) = CreatteBvhDatas(rootNode);


        bvhBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, bvhDatas.Count, Marshal.SizeOf<BvhData>());
        bvhBuffer.SetData(bvhDatas);

        var sortedTriangles = triangleIndexes.Select(idx => triangles[idx]).ToList();

        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, sortedTriangles.Count, Marshal.SizeOf<Triangle>());
        triangleBuffer.SetData(sortedTriangles);

#if UNITY_EDITOR
        bvhDatasForDebug = bvhDatas;
        trianglesForDebug = sortedTriangles;
#endif
    }



    List<Triangle> CreateTriangles()
    {
        var meshFilters = targets.SelectMany(target => target.GetComponentsInChildren<MeshFilter>());

        return meshFilters.SelectMany(mf =>
        {
            var mesh = mf.sharedMesh;
            var triangles = mesh.triangles;

            var trans = mf.transform;
            var worldVertices = mesh.vertices.Select(vtx => trans.TransformPoint(vtx)).ToList();

            return Enumerable.Range(0, triangles.Length / 3).Select(i =>
            {
                var pos0 = worldVertices[triangles[i * 3 + 0]];
                var pos1 = worldVertices[triangles[i * 3 + 1]];
                var pos2 = worldVertices[triangles[i * 3 + 2]];

                var normal = -Vector3.Cross(pos0 - pos1, pos2 - pos1).normalized;

                return new Triangle()
                {
                    pos0 = pos0,
                    pos1 = pos1,
                    pos2 = pos2,
                    normal = normal
                };
            });
        }).ToList();
    }


    //List<TriangleBounds> triBounds;

    BvhNode CreateBvh(List<Triangle> triangles)
    {
        BvhNode rootNode;

        var triBoundsArray = new NativeArray<TriangleBounds>(triangles.Count, Allocator.Temp);
        {
            for (var i = 0; i < triangles.Count; ++i)
            {
                var tri = triangles[i];
                var min = Vector3.Min(Vector3.Min(tri.pos0, tri.pos1), tri.pos2);
                var max = Vector3.Max(Vector3.Max(tri.pos0, tri.pos1), tri.pos2);

                var triBounds = new TriangleBounds()
                {
                    bounds = new Bounds() { min = min, max = max },
                    triangleIndex = i
                };

                triBoundsArray[i] = triBounds;
            }

            rootNode = CreateBvhRecursive(triBoundsArray);
            //triBounds = triBoundsArray.ToList();
        }
        triBoundsArray.Dispose();

        return rootNode;
    }



    private BvhNode CreateBvhRecursive(NativeSlice<TriangleBounds> triangleBoundsArray, int recursiveCount = 0)
    {
        static BvhNode CreateBvhNodeLeaf(NativeSlice<TriangleBounds> triangleBoundsArray)
        {
            return new BvhNode()
            {
                bounds = CalcBounds(triangleBoundsArray),
                triangleIndexs = triangleBoundsArray.Select(n => n.triangleIndex).ToList()
            };
        }


        // Find smallest cost split
        // Select Axis  0 = X, 1 = Y, 2 = Z
        var bestSplit = 0f;
        var bestAxis = -1;

        if (triangleBoundsArray.Length >= 4)
        {
            var (totalBounds, minCost) = CalcBoundsAndSAH(triangleBoundsArray);
            var size = totalBounds.size;

            var leftBuf = new NativeArray<TriangleBounds>(triangleBoundsArray.Length, Allocator.Temp);
            var rightBuf = new NativeArray<TriangleBounds>(triangleBoundsArray.Length, Allocator.Temp);

            for (var axis = 0; axis < 3; ++axis)
            {
                if (size[axis] < 0.001) continue;

                var step = size[axis] / (splitTestCount / (recursiveCount + 1));

                var stepStart = totalBounds.min[axis] + step;
                var stepEnd = totalBounds.max[axis] - step;


                for (var testSplit = stepStart; testSplit < stepEnd; testSplit += step)
                {
                    var (left, right) = SplitLR(triangleBoundsArray, axis, testSplit, ref leftBuf, ref rightBuf);

                    if (left.Length <= 1 || right.Length <= 1) continue;

                    var (_, costLeft) = CalcBoundsAndSAH(left);
                    var (_, costRight) = CalcBoundsAndSAH(right);

                    var cost = costLeft + costRight;

                    if (cost < minCost)
                    {
                        minCost = cost;
                        bestAxis = axis;
                        bestSplit = testSplit;
                    }
                }
            }

            rightBuf.Dispose();
            leftBuf.Dispose();
        }


        BvhNode ret;

        // Not Split
        if (bestAxis < 0)
        {
            ret = CreateBvhNodeLeaf(triangleBoundsArray);
        }
        // Calc child
        else
        {
            var leftBuf = new NativeArray<TriangleBounds>(triangleBoundsArray.Length, Allocator.Temp);
            var rightBuf = new NativeArray<TriangleBounds>(triangleBoundsArray.Length, Allocator.Temp);
            {
                var (left, right) = SplitLR(triangleBoundsArray, bestAxis, bestSplit, ref leftBuf, ref rightBuf);

                var leftNode = CreateBvhRecursive(left, recursiveCount + 1);
                var rightNode = CreateBvhRecursive(right, recursiveCount + 1);

                var bounds = leftNode.bounds;
                bounds.Encapsulate(rightNode.bounds);

                ret = new BvhNode()
                {
                    bounds = bounds,
                    left = leftNode,
                    right = rightNode
                };
            }
            rightBuf.Dispose();
            leftBuf.Dispose();
        }

        return ret;
    }


    static Bounds CalcBounds(NativeSlice<TriangleBounds> triangleBoundsArray)
    {
        var min = Vector3.one * float.MaxValue;
        var max = Vector3.one * float.MinValue;

        for (var i = 0; i < triangleBoundsArray.Length; ++i)
        {
            var bounds = triangleBoundsArray[i].bounds;
            min = Vector3.Min(min, bounds.min);
            max = Vector3.Max(max, bounds.max);
        }

        return new Bounds() { min = min, max = max };
    }

    // SAH(Surface Area Heuristics)
    // the current bbox has a cost of (number of triangles) * surfaceArea of C = N * SA
    (Bounds, float) CalcBoundsAndSAH(NativeSlice<TriangleBounds> triangleBoundsArray)
    {
        var bounds = CalcBounds(triangleBoundsArray);

        var size = bounds.size;
        var sah = triangleBoundsArray.Length * (size.x * size.y + size.x * size.y + size.y * size.z);

        return (bounds, sah);
    }


    (NativeSlice<TriangleBounds> left, NativeSlice<TriangleBounds> right) SplitLR(NativeSlice<TriangleBounds> triBoundsArray, int axis, float split, ref NativeArray<TriangleBounds> leftBuf, ref NativeArray<TriangleBounds> rightBuf)
    {
        var leftCount = 0;
        var rightCount = 0;

        for (var i = 0; i < triBoundsArray.Length; ++i)
        {
            var tb = triBoundsArray[i];

            if (tb.bounds.center[axis] < split)
            {
                leftBuf[leftCount++] = tb;
            }
            else
            {
                rightBuf[rightCount++] = tb;
            }
        }

        return (leftBuf.Slice(0, leftCount), rightBuf.Slice(0, rightCount));
    }



    private (List<BvhData>, List<int>) CreatteBvhDatas(BvhNode node)
    {
        var datas = new List<BvhData>();
        var triangleIndexes = new List<int>();

        CreatteBvhDatasRecursive(node, datas, triangleIndexes);

        return (datas, triangleIndexes);

    }

    void CreatteBvhDatasRecursive(BvhNode node, List<BvhData> datas, List<int> triangleIndexes)
    {
        var data = new BvhData()
        {
            min = node.bounds.min,
            max = node.bounds.max,
            leftIdx = -1,
            rightIdx = -1,
            triangleIdx = -1,
            triangleCount = 0
        };

        if (node.IsLeaf)
        {
            var idx = triangleIndexes.Count;
            triangleIndexes.AddRange(node.triangleIndexs);

            data.triangleIdx = idx;
            data.triangleCount = node.triangleIndexs.Count;

            datas.Add(data);
        }
        else
        {
            data.triangleIdx = -1;

            var dataIdx = datas.Count;
            datas.Add(default); // reserve my data idx

            data.leftIdx = datas.Count;
            CreatteBvhDatasRecursive(node.left, datas, triangleIndexes);

            data.rightIdx = datas.Count;
            CreatteBvhDatasRecursive(node.right, datas, triangleIndexes);

            datas[dataIdx] = data;
        }
    }


    #region Debug

    void DrawBvhGizmo(int idx = 0, int recursiveCount = 0)
    {
        if (idx < 0 || bvhDatasForDebug.Count <= idx) return;

        var data = bvhDatasForDebug[idx];

        if (recursiveCount == gizmoBvhDepth)
        {
            if (data.IsLeaf)
            {
                Gizmos.color = Color.red;
                for (var i = 0; i < data.triangleCount; ++i)
                {
                    var tri = trianglesForDebug[i + data.triangleIdx];
                    Gizmos.DrawLine(tri.pos0, tri.pos1);
                    Gizmos.DrawLine(tri.pos0, tri.pos2);
                    Gizmos.DrawLine(tri.pos1, tri.pos2);
                }
            }

            if (!drawOnlyLeafNode || data.IsLeaf)
            {
                var bounds = new Bounds() { min = data.min, max = data.max };

                Gizmos.color = data.IsLeaf ? Color.cyan : Color.green;
                Gizmos.DrawWireCube(bounds.center, bounds.size);
            }
        }
        else if (!data.IsLeaf)
        {
            DrawBvhGizmo(data.leftIdx, recursiveCount + 1);
            DrawBvhGizmo(data.rightIdx, recursiveCount + 1);
        }
    }

    #endregion
}
