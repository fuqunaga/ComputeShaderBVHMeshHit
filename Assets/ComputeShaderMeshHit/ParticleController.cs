using System.Runtime.InteropServices;
using Unity.Collections;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    static class ShaderParam
    {
        public static string KernelUpdate = "Update";
        public static int particleBuffer = Shader.PropertyToID("particleBuffer");
        public static int gravigy = Shader.PropertyToID("gravity");
        public static int deltaTime = Shader.PropertyToID("deltaTime");
    }


    public ComputeShader computeShader;


    public int particleCount = 10000;
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
        particleBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, particleCount, Marshal.SizeOf(typeof(Particle)));

        var datas = new NativeArray<Particle>(particleCount, Allocator.Temp);

        for (var i = 0; i < particleCount; ++i)
        {
            var randomVector = new Vector3(Random.value, Random.value, Random.value);
            var pos = Vector3.Scale(randomVector, bounds.size) + bounds.min;


            datas[i] = new Particle()
            {
                poision = pos,
            };
        }

        particleBuffer.SetData(datas);

        datas.Dispose();
    }

    private void DispatchParticle()
    {
        var kernel = computeShader.FindKernel(ShaderParam.KernelUpdate);

        computeShader.SetBuffer(kernel, ShaderParam.particleBuffer, particleBuffer);
        computeShader.SetFloat(ShaderParam.gravigy, gravity);
        computeShader.SetFloat(ShaderParam.deltaTime, Time.deltaTime);


        computeShader.GetKernelThreadGroupSizes(kernel, out var x, out var _, out var _);
        computeShader.Dispatch(kernel, Mathf.CeilToInt((float)particleCount / x), 1, 1);
    }
}
