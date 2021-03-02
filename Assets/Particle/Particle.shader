Shader "Custom/Particle"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Size("Size", Float) = 1.0
        _Color("Color", Color) = (0.5, 0.5, 0.5,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        AlphaToMask On


        HLSLINCLUDE

        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
        #include "Particle.hlsl"

        struct v2g
        {
            float4 pos : SV_POSITION;
            float4 color : TEXCOORD;
        };

        struct g2f 
        {
            float4 pos : SV_POSITION;
            float2 uv : TEXCOORD0;
            float4 color : TEXCOORD1;
        };


        StructuredBuffer<Particle> _ParticleBuffer;
        float _Size;
        float4 _Color;

        v2g vert(uint iid : SV_INSTANCEID)
        {
            Particle particle = _ParticleBuffer[iid];

            v2g Out;
            Out.pos = float4(particle.position, 1);
            Out.color = particle.color;
            return Out;
        }

        [maxvertexcount(4)]
        void geom(point v2g In[1], inout TriangleStream<g2f> outStream)
        {
            float3 center = In[0].pos.xyz;
            float4 color = In[0].color;

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
            Out.color = color;
            outStream.Append(Out);

            Out.pos = TransformWorldToHClip(v[1]);
            Out.uv = uv[1];
            Out.color = color;
            outStream.Append(Out);

            Out.pos = TransformWorldToHClip(v[2]);
            Out.uv = uv[2];
            Out.color = color;
            outStream.Append(Out);

            Out.pos = TransformWorldToHClip(v[3]);
            Out.uv = uv[3];
            Out.color = color;
            outStream.Append(Out);
        }           

                    
        sampler2D _MainTex;

        float4 frag (g2f In) : SV_Target
        {
            return tex2D(_MainTex, In.uv) * In.color * _Color;;
        }
        
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDHLSL
        } 
        
        
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode" = "ShadowCaster"}

            /*
            ZWrite On
            ZTest LEqual
            */
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            ENDHLSL
        }
    }
}
