using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;


namespace ComputeShaderBvhMeshHit
{
    public static class BvhBuilder
    {
        #region Type Define

        public struct TriangleBounds
        {
            public Bounds bounds;
            public int triangleIndex;
        }

        public class BvhNode
        {
            public Bounds bounds;
            public BvhNode left;
            public BvhNode right;

            public List<int> triangleIndexs;

            public bool IsLeaf => triangleIndexs != null;
        }

        #endregion


        public static (List<BvhData> bvhDatas, List<Triangle> triangles) BuildBvh(GameObject rootMeshObject, int splitCount)
        {
            var triangles = CreateTriangles(rootMeshObject);
            var rootNode = CreateBvh(triangles, splitCount);

            var (bvhDatas, triangleIndexes) = CreatteBvhDatas(rootNode);
            var sortedTriangles = triangleIndexes.Select(idx => triangles[idx]).ToList();

            return (bvhDatas, sortedTriangles);
        }

        static List<Triangle> CreateTriangles(GameObject rootMeshObject)
        {
            var meshFilters = rootMeshObject.GetComponentsInChildren<MeshFilter>();

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


        
        static BvhNode CreateBvh(List<Triangle> triangles, int splitCount)
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

                rootNode = CreateBvhRecursive(triBoundsArray, splitCount);
                //triBounds = triBoundsArray.ToList();
            }
            triBoundsArray.Dispose();

            return rootNode;
        }



        static BvhNode CreateBvhRecursive(NativeSlice<TriangleBounds> triangleBoundsArray, int splitCount, int recursiveCount = 0)
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

                    var step = size[axis] / (splitCount / (recursiveCount + 1));

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

                    var leftNode = CreateBvhRecursive(left, splitCount, recursiveCount + 1);
                    var rightNode = CreateBvhRecursive(right, splitCount, recursiveCount + 1);

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
        static (Bounds, float) CalcBoundsAndSAH(NativeSlice<TriangleBounds> triangleBoundsArray)
        {
            var bounds = CalcBounds(triangleBoundsArray);

            var size = bounds.size;
            var sah = triangleBoundsArray.Length * (size.x * size.y + size.x * size.y + size.y * size.z);

            return (bounds, sah);
        }


        static (NativeSlice<TriangleBounds> left, NativeSlice<TriangleBounds> right) SplitLR(NativeSlice<TriangleBounds> triBoundsArray, int axis, float split, ref NativeArray<TriangleBounds> leftBuf, ref NativeArray<TriangleBounds> rightBuf)
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



        static (List<BvhData>, List<int>) CreatteBvhDatas(BvhNode node)
        {
            var datas = new List<BvhData>();
            var triangleIndexes = new List<int>();

            CreatteBvhDatasRecursive(node, datas, triangleIndexes);

            return (datas, triangleIndexes);

        }

        static void CreatteBvhDatasRecursive(BvhNode node, List<BvhData> datas, List<int> triangleIndexes)
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
    }
}