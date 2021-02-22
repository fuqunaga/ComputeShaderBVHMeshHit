Shader "Custom/Particle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size("Size", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
            #include "Particle.hlsl"

            StructuredBuffer<Particle> _ParticleBuffer;

            sampler2D _MainTex;
            float _Size;

            struct g2f 
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD;
            };


            float4 vert(uint iid : SV_INSTANCEID) : POSITION
            {
                Particle particle = _ParticleBuffer[iid];
                return float4(particle.position, 1);
            }

            [maxvertexcount(4)]
            void geom(point float4 p[1] : POSITION, inout TriangleStream<g2f> outStream)
            {
                float3 center = p[0].xyz;

                float3 up = float3(0, 1, 0);
                float3 look = _WorldSpaceCameraPos - center;
                look = normalize(look);

                float3 right = cross(up, look);
                up = cross(look, right);
                
                float3 r = right * _Size * 0.5;
                float3 u = up * _Size * 0.5;
                        
                float3 v[4];
                v[0] = center + r - u;
                v[1] = center + r + u;
                v[2] = center - r - u;
                v[3] = center - r + u;

                float2 uv[4];
                uv[0] = float2(1, 0);
                uv[1] = float2(1, 1);
                uv[2] = float2(0, 0);
                uv[3] = float2(0, 1);

                g2f Out;

                Out.pos = TransformWorldToHClip(v[0]);
                Out.uv = uv[0];
                outStream.Append(Out);

                Out.pos = TransformWorldToHClip(v[1]);
                Out.uv = uv[1];
                outStream.Append(Out);

                Out.pos = TransformWorldToHClip(v[2]);
                Out.uv = uv[2];
                outStream.Append(Out);

                Out.pos = TransformWorldToHClip(v[3]);
                Out.uv = uv[3];
                outStream.Append(Out);
            }           


            float4 frag () : SV_Target
            {
                return float4(1,0,0,1);
            }

            ENDHLSL
        }
    }
}
