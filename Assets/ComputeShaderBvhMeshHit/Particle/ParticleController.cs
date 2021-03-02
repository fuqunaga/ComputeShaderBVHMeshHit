using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    static class ShaderParam
    {
        public static string KernelUpdate = "Update";
        public static int particleBuffer = Shader.PropertyToID("particleBuffer");
        public static int BvhBuffer = Shader.PropertyToID("BvhBuffer");
        public static int triangleBuffer = Shader.PropertyToID("triangleBuffer");
        public static int spawnBoundsMin = Shader.PropertyToID("spawnBoundsMin");
        public static int spawnBoundsMax = Shader.PropertyToID("spawnBoundsMax");
        public static int bounceRate = Shader.PropertyToID("bounceRate");
        public static int gravigy = Shader.PropertyToID("gravity");
        public static int time = Shader.PropertyToID("time");
        public static int deltaTime = Shader.PropertyToID("deltaTime");
    }


    public ComputeShader computeShader;
    public MeshToBuffer meshToBuffer;


    public int particleCount = 10000;

    [Range(0f, 1f)]
    public float bounceRate = 0.5f;
    public Bounds bounds = new Bounds() { size = Vector3.one * 100 };



    public float gravity;

    public GraphicsBuffer particleBuffer { get; protected set; }


    void Start()
    {
        CreateBuffer();
    }

    void OnDestroy()
    {
        if (particleBuffer != null) particleBuffer.Dispose();
    }

    void Update()
    {
        DispatchParticle();
    }

    void CreateBuffer()
    {
        particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, Marshal.SizeOf<Particle>());

        var datas = new NativeArray<Particle>(particleCount, Allocator.Temp);

        for (var i = 0; i < particleCount; ++i)
        {
            var randomVector = new Vector3(Random.value, Random.value, Random.value);
            var pos = Vector3.Scale(randomVector, bounds.size) + bounds.min;


            datas[i] = new Particle()
            {
                poision = pos,
                color = Color.white
            };
        }

        particleBuffer.SetData(datas);

        datas.Dispose();
    }

    private void DispatchParticle()
    {
        var kernel = computeShader.FindKernel(ShaderParam.KernelUpdate);

        computeShader.SetBuffer(kernel, ShaderParam.particleBuffer, particleBuffer);
        computeShader.SetBuffer(kernel, ShaderParam.BvhBuffer, meshToBuffer.BvhBuffer);
        computeShader.SetBuffer(kernel, ShaderParam.triangleBuffer, meshToBuffer.TriangleBuffer);
        computeShader.SetVector(ShaderParam.spawnBoundsMin, bounds.min);
        computeShader.SetVector(ShaderParam.spawnBoundsMax, bounds.max);
        computeShader.SetFloat(ShaderParam.bounceRate, bounceRate);
        computeShader.SetFloat(ShaderParam.gravigy, gravity);
        computeShader.SetFloat(ShaderParam.time, Time.time);
        computeShader.SetFloat(ShaderParam.deltaTime, Time.deltaTime);  

        computeShader.GetKernelThreadGroupSizes(kernel, out var x, out var _, out var _);
        computeShader.Dispatch(kernel, Mathf.CeilToInt((float)particleCount / x), 1, 1);
    }
}
