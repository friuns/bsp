// Unity built-in shader source. Copyright (c) 2016 Unity Technologies. MIT license (see license.txt)

// Unlit shader. Simplest possible textured shader.
// - no lighting
// - no lightmap support
// - no per-material color

Shader "bspLightmap" {
Properties {
    _MainTex ("Base (RGB)", 2D) = "white" {}
	_LightMap ("Lightmap (RGB)", 2D) = "white" {}
	_Color ("Main Color", Color) = (1,1,1,1)
}

SubShader {
    Tags { "RenderType"="Opaque" }
    LOD 200

    Pass {
	  Tags {"LightMode"="ForwardBase"}
        CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

         #include "UnityCG.cginc"
            #include "Lighting.cginc"

            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
            #include "AutoLight.cginc"

            struct appdata_t {
                float4 vertex  : POSITION;
                float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 texcoord : TEXCOORD0;
				float2 texcoord1 : TEXCOORD1;
                UNITY_FOG_COORDS(3)
                UNITY_VERTEX_OUTPUT_STEREO
					 SHADOW_COORDS(2) 
            };

            sampler2D _MainTex;
			sampler2D _LightMap;
            float4 _MainTex_ST;
			float4 _LightMap_ST;

            v2f vert (appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord1 = TRANSFORM_TEX(v.texcoord1, _LightMap);
                UNITY_TRANSFER_FOG(o,o.pos);
				TRANSFER_SHADOW(o)
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
				fixed shadow = SHADOW_ATTENUATION(i);
                fixed4 col = tex2D(_MainTex, i.texcoord)*min(tex2D(_LightMap, i.texcoord1),max(shadow,.5)*half4(1,1,1,1)) ;
                UNITY_APPLY_FOG(i.fogCoord, col);
                UNITY_OPAQUE_ALPHA(col.a);
                return col;
            }
        ENDCG
    }UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
}
Fallback "Legacy Shaders/Lightmapped/VertexLit2"
}
