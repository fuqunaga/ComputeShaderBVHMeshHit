using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;


namespace ComputeShaderBvhMeshHit.Sample
{
    [RequireComponent(typeof(BvhHelperBehaviour))]
    public class ParticleController : MonoBehaviour
    {
        static class ShaderParam
        {
            public static string KernelUpdate = "Update";
            public static int particleBuffer = Shader.PropertyToID("particleBuffer");
            public static int spawnBoundsMin = Shader.PropertyToID("spawnBoundsMin");
            public static int spawnBoundsMax = Shader.PropertyToID("spawnBoundsMax");
            public static int bounceRate = Shader.PropertyToID("bounceRate");
            public static int gravigy = Shader.PropertyToID("gravity");
            public static int damping = Shader.PropertyToID("damping");
            public static int time = Shader.PropertyToID("time");
            public static int deltaTime = Shader.PropertyToID("deltaTime");
        }


        public ComputeShader computeShader;
        public int particleCount = 10000;

        [Range(0f, 1f)]
        public float bounceRate = 0.5f;
        public Bounds spawnBounds = new Bounds() { size = Vector3.one * 10 };
        public float gravity;
        public float damping = 0.9f;

        [Header("BVH Gizmos")]
        public int bvhGizmoDepth = 0;
        public bool bvhGizmoOnlyLeafNode;

        public GraphicsBuffer particleBuffer { get; protected set; }


        BvhHelperBehaviour bvhHelper;


        #region Unity

        void Start()
        {
            bvhHelper = GetComponent<BvhHelperBehaviour>();
            CreateParticleBuffer();
        }

        void OnDestroy()
        {
            if (particleBuffer != null) particleBuffer.Dispose();
        }

        void Update()
        {
            DispatchParticle();
        }

        #endregion


        void CreateParticleBuffer()
        {
            particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, Marshal.SizeOf<Particle>());

            var datas = new NativeArray<Particle>(particleCount, Allocator.Temp);

            for (var i = 0; i < particleCount; ++i)
            {
                var randomVector = new Vector3(Random.value, Random.value, Random.value);
                var pos = Vector3.Scale(randomVector, spawnBounds.size) + spawnBounds.min;


                datas[i] = new Particle()
                {
                    poision = pos,
                    color = Color.Lerp(Color.gray, Color.white, Random.value),
                    randState = (uint)i + 1 // not allow 0
                };
            }

            particleBuffer.SetData(datas);

            datas.Dispose();
        }

        private void DispatchParticle()
        {
            var kernel = computeShader.FindKernel(ShaderParam.KernelUpdate);

            bvhHelper.SetBuffersToComputShader(computeShader, kernel);

            computeShader.SetBuffer(kernel, ShaderParam.particleBuffer, particleBuffer);
            computeShader.SetVector(ShaderParam.spawnBoundsMin, spawnBounds.min);
            computeShader.SetVector(ShaderParam.spawnBoundsMax, spawnBounds.max);
            computeShader.SetFloat(ShaderParam.bounceRate, bounceRate);
            computeShader.SetFloat(ShaderParam.gravigy, gravity);
            computeShader.SetFloat(ShaderParam.damping, damping);
            computeShader.SetFloat(ShaderParam.time, Time.time);
            computeShader.SetFloat(ShaderParam.deltaTime, Time.deltaTime);

            computeShader.GetKernelThreadGroupSizes(kernel, out var x, out var _, out var _);
            computeShader.Dispatch(kernel, Mathf.CeilToInt((float)particleCount / x), 1, 1);
        }
    }
}