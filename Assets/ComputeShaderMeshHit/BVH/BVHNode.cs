using System.Collections.Generic;
using UnityEngine;


public struct TriangleBounds
{
    public Bounds bounds;
    public int triangleIndex;
}

public class BVHNode
{
    public Bounds bounds;
    public BVHNode left;
    public BVHNode right;

    public List<int> triangleIndexs;

    public bool IsLeaf => triangleIndexs != null;
}


public struct BVHData
{
    public Vector3 min;
    public Vector3 max;

    public int leftIdx;
    public int rightIdx;

    public int triangleIdx; // -1 if data is not leaf
    public int triangleCount;
}