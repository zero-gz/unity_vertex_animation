Shader "Unlit/vertex_animation"
{
	Properties
	{
		_VertexAnimationTex("Texture", 2D) = "black" {}
		_VertexAnimationSpeed("VertexAnimationSpeed", Range(0, 10)) = 1 
		_VertexAnimationScale("VertexAnimationScale", Range(0, 2)) = 1
		_VertexAnimationOffset("VertexAnimationOffset", Vector) = (0.0, 0.0, 0.0, 0.0)
		//[KeywordEnum(MAX, MAYA)] _FBX_TYPE("fbx type", Float) = 0
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag	
			#pragma shader_feature _FBX_TYPE_MAX _FBX_TYPE_MAYA
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 color : COLOR;
			};

			sampler2D _VertexAnimationTex;
			float _VertexAnimationSpeed;
			float _VertexAnimationScale;
			float4 _VertexAnimationOffset;

			v2f vert(appdata v)
			{
				v2f o;
				float x = _Time.y * _VertexAnimationSpeed;
				// 时间为u轴，顶点Index为v轴，组成uv
				// 其中顶点Index来自uv1，定义是 float2 uv1 : TEXCOORD1;
				float4 uv = float4(v.uv1.x, 1.0 -frac(x), 0, 0);
				// 因为是在VertexShader中采样，没有自动lod可以用，所以要指定lod = uv.w = 0。 不太清楚cg里面有没有Load...
				float4 offset = tex2Dlod(_VertexAnimationTex, uv);
				o.color = offset;
				// 因为导出的偏移是在物体自身坐标系内的，所以是直接对顶点数据进行操作
					// 加了一对Offset和Scale来做Remap
				offset.xyz = (offset.xyz * _VertexAnimationScale + _VertexAnimationOffset.xyz);

				float4 new_offset = offset;
				//#ifdef _FBX_TYPE_MAX
					new_offset = float4(-offset.x, offset.z, offset.y, offset.w);
					float4x4 rotation_matrix = {1,0,0,0, 0, 0, -1,0, 0, 1, 0, 0, 0,0,0,1};
					new_offset = mul(rotation_matrix, new_offset);
				//#endif

				/*
				#ifdef _FBX_TYPE_MAYA
					new_offset = float4(offset.z, offset.y, offset.x, offset.w);
				#endif
				*/

				v.vertex.xyz = v.vertex.xyz + new_offset.xyz;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				fixed4 col = i.color; // fixed4(1.0, 1.0, 1.0, 1.0);
				return col;
			}
			ENDCG
		}
	}
}
