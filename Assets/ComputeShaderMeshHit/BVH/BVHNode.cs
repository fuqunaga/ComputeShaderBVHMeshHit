using System.Collections.Generic;
using UnityEngine;


public class BvhNode
{
    public Bounds bounds;
    public BvhNode left;
    public BvhNode right;

    public List<int> triangleIndexs;

    public bool IsLeaf => triangleIndexs != null;
}


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