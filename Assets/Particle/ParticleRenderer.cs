using UnityEngine;

namespace ComputeShaderBvhMeshHit.Sample
{
    [RequireComponent(typeof(ParticleController))]
    public class ParticleRenderer : MonoBehaviour
    {
        public static class ShaderParam
        {
            public static int ParticleBuffer = Shader.PropertyToID("_ParticleBuffer");
            public static int Size = Shader.PropertyToID("_Size");
        }

        public Material material;
        public float size = 1f;
        MaterialPropertyBlock mpb;

        ParticleController particleController;


        void Start()
        {
            particleController = GetComponent<ParticleController>();
            mpb = new MaterialPropertyBlock();
        }


        void Update()
        {
            mpb.SetBuffer(ShaderParam.ParticleBuffer, particleController.particleBuffer);
            mpb.SetFloat(ShaderParam.Size, size);

            var bounds = new Bounds() { size = Vector3.one * 100000f };
            Graphics.DrawProcedural(material, bounds, MeshTopology.Points, 1, particleController.particleCount, properties: mpb);
        }
    }
}