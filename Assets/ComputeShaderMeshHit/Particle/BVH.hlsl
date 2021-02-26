#ifndef BVH_INCLUCDE
#define BVH_INCLUCDE

#define BVH_STACK_SIZE 32

struct BVHData 
{
    float3 min;
    float3 max;

    int leftIdx;
    int rightIdx;

    int triangleIdx; // -1 if data is not leaf
    int triangleCount;
};


StructuredBuffer<BVHData> bvhBuffer;

// http://marupeke296.com/COL_3D_No18_LineAndAABB.html
bool IntersectAABB(float3 origin, float3 ray, BVHData data)
{
    float3 aabbMin = data.min;
    float3 aabbMax = data.max;

    float tNear = 0;
    float tFar = 1;

    for(int axis = 0; axis<3; ++axis)
    {
        float rayOnAxis = ray[axis];
        float originOnAxis = origin[axis];
        float minOnAxis = aabbMin[axis];
        float maxOnAxis = aabbMax[axis];
        if(rayOnAxis == 0)
        {
            if ( originOnAxis < minOnAxis || maxOnAxis < originOnAxis ) return false;
        }
        else
        {
            float t0 = (minOnAxis - originOnAxis) / rayOnAxis;
            float t1 = (maxOnAxis - originOnAxis) / rayOnAxis;

            float tMin = min(t0, t1);
            float tMax = max(t0, t1);

            tNear = max(tNear, tMin);
            tFar  = min(tFar, tMax);

            if (tFar < 0.0 || tNear > tFar) return false;
        }
    }

    return true;
}

// return trinagle idx, count
int2 TraveseBVH(float3 origin, float3 ray)
{
    int stack[BVH_STACK_SIZE];

    int stackIdx = 1;
    stack[0] = 0;

    while(stackIdx)
    {
        int bvhIdx = stack[stackIdx--];

        BVHData data = bvhBuffer[bvhIdx];

        // Branch node
        if (data.triangleIdx < 0)
        {

        }
        // Leaf node
        else
        {

        }

    }
}

#endif // BVH_INCLUCDE