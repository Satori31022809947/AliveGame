Shader "Custom/StandardDoubleSided"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
        [Enum(UnityEngine.Rendering.CullMode)] _CullMode ("Cull Mode", Float) = 0
        [Toggle] _UseAlphaClip ("Use Alpha Clip", Float) = 0
    }
    
    SubShader
    {
        Tags {"RenderType"="Opaque"}
        LOD 200
        Cull [_CullMode]

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows addshadow
        #pragma target 3.0
        
        sampler2D _MainTex;
        
        struct Input 
        {
            float2 uv_MainTex;
        };
        
        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        fixed _Cutoff;
        fixed _UseAlphaClip;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Sample the texture
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            
            // Apply alpha clipping if enabled
            if (_UseAlphaClip > 0.5)
            {
                clip(c.a - _Cutoff);
            }
            
            // Set output
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
        
        // Custom shadow caster pass for alpha clipping
        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode"="ShadowCaster"}
            
            Cull [_CullMode]
            ZWrite On
            ZTest LEqual
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_shadowcaster
            
            #include "UnityCG.cginc"
            
            struct appdata_t 
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f 
            {
                V2F_SHADOW_CASTER;
                float2 uv : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed _Cutoff;
            fixed _UseAlphaClip;
            
            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o)
                o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                // Sample texture and apply alpha clipping if enabled
                fixed4 texcol = tex2D(_MainTex, i.uv) * _Color;
                
                if (_UseAlphaClip > 0.5)
                {
                    clip(texcol.a - _Cutoff);
                }
                
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
    }
    
    FallBack "Diffuse"
}