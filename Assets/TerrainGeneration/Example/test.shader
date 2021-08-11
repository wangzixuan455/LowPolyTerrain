Shader "Unlit/test"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_ColTex("Color Texture",2D) = "white"{}
		_InfoTex("Info Texture",2D) = "white"{}
		_Range("Color Contor",Range(0,1)) = 0.672
		_Diffuse("Diffuse",Color) = (1,1,1,1)
    }
    SubShader
    {
		Tags { "RenderType" = "Opaque" }
		LOD 100
        Pass
        {
			Tags{"LightMode" = "ForwardBase"}
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
			#include "Lighting.cginc"
            #include "UnityCG.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float3 uv3 : TEXCOORD2;
				float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
				float2 uv2 : TEXCOORD1;
				float2 uv3 : TEXCOORD2;
                float4 vertex : SV_POSITION;
				fixed3 diff : TEXCOORD3;
				float3 nor : TEXCOORD4;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
			sampler2D _ColTex;
			float4 _ColTex_ST;
			sampler2D _InfoTex;
			float4 _InfoTex_ST;
			float _Range;
			fixed4 _Diffuse;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.uv2 = v.uv2;
				o.uv3 = v.uv3;
				fixed3 ambient = UNITY_LIGHTMODEL_AMBIENT.xyz;//获得环境光
				//法线的MVP
				fixed3 worldNormal = normalize(UnityObjectToClipPos(v.normal));
				//获取光线方向
				fixed3 worldlight = normalize(_WorldSpaceLightPos0.xyz);
				//计算
				fixed3 diffuse = _LightColor0.rgb * _Diffuse.rgb * saturate(dot(worldNormal, worldlight));
				o.diff = diffuse;
				o.nor = v.normal;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
				fixed4 col1 = tex2D(_ColTex, i.uv2);
                fixed4 col2 = tex2D(_MainTex, i.uv);
				fixed4 col3 = tex2D(_InfoTex, i.uv3);

               // fixed4 col = (col2*col1 * 2 + col2 * col2*(1 - col1 * 2)) + (1 - _Range)*(col2*(1 - col1) * 2 + sqrt(col2)*(2 * col1 - 1));//柔光
				fixed4 col = _Range * (col2 - (1 - col2)*(1 - 2 * col1) / (2 * col1)) + (1 - _Range)*(col2 + col2 * (2 * col1 - 1) / (2 * (1 - col1))); //亮光
				//fixed4 col = col2 / col1;
				//return fixed4(i.nor, 1.0);
				fixed4 mix = col +fixed4(i.diff, 1.0);
				return mix;//col3 * col3.a + col * (1-col3.a);
            }
            ENDCG
        }
    }
}
