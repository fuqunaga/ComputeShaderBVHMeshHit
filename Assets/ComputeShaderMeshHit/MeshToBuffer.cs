using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;

public class MeshToBuffer : MonoBehaviour
{
    public List<GameObject> targets;

    GraphicsBuffer triangleBuffer;

    public GraphicsBuffer TriangleBuffer
    {
        get
        {
            if (triangleBuffer == null) CreateBuffer();
            return triangleBuffer;
        }
    }



    void OnDestroy()
    {
        if (triangleBuffer != null) triangleBuffer.Dispose();
    }


    void CreateBuffer()
    {
        var meshFilters = targets.SelectMany(target => target.GetComponentsInChildren<MeshFilter>());

        var triangles = meshFilters.SelectMany(mf =>
        {
            var trans = mf.transform;
            var mesh = mf.sharedMesh;

            var worldVertices = mesh.vertices.Select(vtx => trans.TransformPoint(vtx)).ToList();

            var triangles = mesh.triangles;

            return Enumerable.Range(0, triangles.Length / 3).Select(i =>
            {
                var pos0 = worldVertices[triangles[i * 3 + 0]];
                var pos1 = worldVertices[triangles[i * 3 + 1]];
                var pos2 = worldVertices[triangles[i * 3 + 2]];

                var normal = -Vector3.Cross(pos0 - pos1, pos2 - pos1).normalized;

                return new Triangle()
                {
                    poision0 = pos0,
                    poision1 = pos1,
                    poision2 = pos2,
                    normal = normal
                };
            });
        }).ToArray();


        triangleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, triangles.Length, Marshal.SizeOf<Triangle>());
        triangleBuffer.SetData(triangles);
    }
}
