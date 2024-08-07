﻿//Ref from:
//https://www.patreon.com/posts/quick-game-art-18245226
//https://pastebin.com/6EXJHAgA
///https://pastebin.com/ppbzx7mn
//
//相关讨论： https://www.reddit.com/r/Unity3D/comments/8d5uf5/stylized_simple_liquid_shader_shader_code/
//
///PS:
///Todo:
///-顶部颜色可以遮挡浸入的模型(可以尝试单独拆分出一个LiquidSurface)(经测试，侧面是可以半透明的)
///	-原因：TopColor的实现原理是：并没有水平面，只是模拟出来，而是检查当前视野的方向，如果看到模型的正面就渲染_MainColor，否则渲染_TopColor。因为没有真实的水平面，因此无法遮挡浸在其中的物体（https://www.reddit.com/r/Unity3D/comments/8d5uf5/comment/dxkishp/?utm_source=share&utm_medium=web3x&utm_name=web3xcss&utm_term=1&utm_content=share_button）
///	-测试：
///		-PlaneClippingShader（【无效】因为实现原理一样）：https://github.com/dedovskaya/CrossSectionShader
/// -Scene和Game窗口看到的结果可能不一样（比如Scene的半透明颜色会突然消失）
Shader "Threeyes/SpecialFX/Liquid"
{
	Properties
	{
		//——Appearance——
		[Header(Appearance Setting)]
		_BaseMap("Base Map", 2D) = "white" {}
		[HDR]_MainColor("Main Color", Color) = (1,1,1,1)//Main Color
		[HDR]_TopColor("Top Color", Color) = (1,1,1,1)
		[HDR]_FoamColor("Foam Color", Color) = (1,1,1,1)//Foam Line Color
		[HDR]_RimColor("Rim Color", Color) = (1,1,1,1)
		_RimPower("Rim Power", Range(0,10)) = 0.0

		//——Model (Setup by Script)——
		[Header(Model Setting)]
		_GlobalScale("Global Scale", Float) = 1.0//物体的GlobalScale（需要乘以物体导入时的缩放）	
		_PosOffset("Pos Offset", Float) = 0.0//修复物体坐标的位移，对应模型的中点（从[最低值，最高值]变为[-X,X]）
		_PosScale("Pos Scale", Float) = 1.0//修复物体坐标的缩放(基于上值，结果=0.5/X)

		//——Runtime Motion——
		[Header(Runtime)]
		_FillAmount("Fill Amount", Range(0, 1)) = 0.5//以物体最上/最下的顶点作为计算，数值是归一化值
		_WobbleX("WobbleX", Range(-1,1)) = 0.0//[Runtime]
		_WobbleZ("WobbleZ", Range(-1,1)) = 0.0//[Runtime]
		_FoamLineWidth("Foam Line Width", Range(0,1)) = 0.0//[Runtime]

		////Bottle (PS: BottleEffect pass will not shown on URP because of the single pass, Use seperate Material instead (https://stackoverflow.com/questions/53178750/unity-custom-shader-second-pass-not-executing))
		//[HideInInspector] _BottleWidth("BottleWidth",Range(0,1)) = 0.2
		//[HideInInspector] _BottleColor("BottleColor",Color) = (1,1,1,1)
	}

		SubShader
			{
				Tags {"Queue" = "Geometry"  "DisableBatching" = "True" }

				//WaterEffect
				Pass
				{
					Zwrite On
					Cull Off // we want the front and back faces
					AlphaToMask On // transparency

					CGPROGRAM

					#pragma vertex vert
					#pragma fragment frag
				// make fog work
				#pragma multi_compile_fog

				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
					float3 normal : NORMAL;
				};

				struct v2f
				{
					float2 uv : TEXCOORD0;
					UNITY_FOG_COORDS(1)
					float4 vertex : SV_POSITION;
					float3 viewDir : COLOR;
					float3 normal : COLOR2;
					float fillEdge : TEXCOORD2;//Range: [-0.5f, 0.5f]
				};

				sampler2D _BaseMap;
				float4 _BaseMap_ST;
				float _GlobalScale, _FillAmount, _PosOffset, _PosScale,  _WobbleX, _WobbleZ;
				float4  _MainColor, _RimColor, _TopColor, _FoamColor;
				float _FoamLineWidth, _RimPower;

				float4 RotateAroundYInDegrees(float4 vertex, float degrees)
				{
					float alpha = degrees * UNITY_PI / 180;//如果degreeswei360，则结果为2Π
					float sina, cosa;
					sincos(alpha, sina, cosa);
					float2x2 m = float2x2(cosa, sina, -sina, cosa);
					return float4(vertex.yz , mul(m, vertex.xz)).xzyw;
				}


				v2f vert(appdata v)
				{
					v2f o;

					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _BaseMap);
					UNITY_TRANSFER_FOG(o,o.vertex);
					// get world position of the vertex
					float3 worldPos = mul(unity_ObjectToWorld, v.vertex.xyz);
					// rotate it around XY
					float3 worldPosX = RotateAroundYInDegrees(float4(worldPos,0),360);
					// rotate around XZ
					float3 worldPosZ = float3 (worldPosX.y, worldPosX.z, worldPosX.x);
					// combine rotations with worldPos, based on sine wave from script
					float3 worldPosAdjusted = worldPos + (worldPosX * _WobbleX) + (worldPosZ * _WobbleZ);
					// how high up the liquid is
					//PS：该值范围 [-0.5f, 0.5f]，为液体显示区域)，因此所有物体的坐标都应该映射到该值：先将局部顶点坐标乘以缩放转回物体世界坐标，然后将其重映射到[-0.5f, 0.5f]
					o.fillEdge = (worldPosAdjusted.y / _GlobalScale + _PosOffset) * _PosScale + (1 - _FillAmount);

					o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
					o.normal = v.normal;
					return o;
				}

				fixed4 frag(v2f i, fixed facing : VFACE) : SV_Target
				{
					// sample the texture
					fixed4 col = tex2D(_BaseMap, i.uv) * _MainColor;
				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);

				// rim light
				float dotProduct = 1 - pow(dot(i.normal, i.viewDir), _RimPower);
				float4 RimResult = smoothstep(0.5, 1.0, dotProduct);
				RimResult *= _RimColor;

				// foam edge（泡沫边缘）
				float4 foam = (step(i.fillEdge, 0.5) - step(i.fillEdge, 0.5 - _FoamLineWidth));
				float4 foamColored = foam * (_FoamColor * 0.9);

				// rest of the liquid
				float4 result = step(i.fillEdge, 0.5) - foam;//决定该区域是否可见（step返回0或1，相当于判断）
				float4 resultColored = result * col;

				// both together, with the texture
				float4 finalResult = resultColored + foamColored;
				finalResult.rgb += RimResult;

				// color of backfaces/ top
				float4 topColor = _TopColor * (foam + result);
				//VFACE returns positive for front facing, negative for backfacing

				//topColor = topColor * (1, 1, 1, 0.5);
				//topColor = (1, 0, 0,0.5);
				//finalResult.a = _MainColor.a;
				//topColor.a = _TopColor.a;
				return facing > 0 ? finalResult : topColor;
			}
			ENDCG
		}

		////BottleEffect(Warning:URP不支持multi-pass，因此用户要通过分模型实现瓶子效果)
		//Pass
		//{
		//	//Cull Front
		//	Blend SrcAlpha OneMinusSrcAlpha
		//	CGPROGRAM
		//	#pragma vertex vert
		//	#pragma fragment frag
		//	// make fog work
		//	#pragma multi_compile_fog

		//	#include "UnityCG.cginc"

		//	struct appdata
		//	{
		//		float4 vertex : POSITION;
		//		float2 uv : TEXCOORD0;
		//		float4 normal : NORMAL;
		//	};

		//	struct v2f
		//	{
		//		float2 uv : TEXCOORD0;
		//		UNITY_FOG_COORDS(1)
		//		float4 vertex : SV_POSITION;
		//		float3 viewDir : COLOR;
		//		float3 normal :NORMAL;
		//	};

		//	float4 _BottleColor;
		//	float _BottleWidth;

		//	float4 _RimColor;
		//	float _RimPower;

		//	v2f vert(appdata v)
		//	{
		//		v2f o;
		//		v.vertex.xyz += v.normal.xyz * _BottleWidth;
		//		o.vertex = UnityObjectToClipPos(v.vertex);
		//		o.uv = v.uv;
		//		UNITY_TRANSFER_FOG(o,o.vertex);
		//		o.normal = v.normal.xyz;
		//		o.viewDir = normalize(ObjSpaceViewDir(v.vertex));

		//		return o;
		//	}

		//	fixed4 frag(v2f i,fixed facing : VFace) : SV_Target
		//	{
		//		// sample the texture
		//		fixed4 col = _BottleColor;
		//		// apply fog
		//		UNITY_APPLY_FOG(i.fogCoord, col);

		//		float dotProduct = 1 - pow(dot(i.normal, i.viewDir),_RimPower);//1-pow(dot(i.normal, i.viewDir),_RimPower/10);
		//		float4 RimResult = _BottleColor * smoothstep(0.2,1,dotProduct);

		//		return RimResult;
		//	}
		//	ENDCG
		//}
	}
}