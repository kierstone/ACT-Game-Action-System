// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

/*==========================================================================*/
/*==========================================================================*/
/*!
	@file	imagine_chara_parts_body.shader
	@brief	イマジン専用シェーダー：キャラ描画特化（体パーツ用）
	@author	By 
	@date 	2015/??/??
*/
/*==========================================================================*/
/*==========================================================================*/
Shader "Imagine/Character/Chara_Parts(Body)" 
{
	//==========================================================================//
	//		Properties 									         				//
	//==========================================================================//
	Properties
	{
		_MainTex		("Base (RGB) ", 2D) = "white" {}

		//----------------------------------------------------------------------------
		//		リムライト用パラメータ
		//----------------------------------------------------------------------------
		_RimColor1		("RimColor1", Color) = (.5,.5,.5,1)
		_RimColor2		("RimColor2", Color) = (.5,.5,.5,1)
		_RimTex			("Rim (RGB) ", 2D) = "white" {} 
		_RimMask		("RimMask (RGB) ", 2D) = "white" {} 

		//----------------------------------------------------------------------------
		//		陰影用パラメータ
		//----------------------------------------------------------------------------
		_RampTex		("ToonRamp (RGB) ", 2D) = "white" {}
		_FalloffTex		("Falloff (RGB) ", 2D) = "white" {} 

		//----------------------------------------------------------------------------
		//		LightProbe用パラメータ
		//----------------------------------------------------------------------------
		_SHLightingDirection	("LightProbe Direction",range(0,1)) = 0
		_SHLightingScale		("LightProbe influence scale",range(0,1)) = 0
		_SHLightingSaturation	("LightProbe Saturation",range(0,1)) = 1
		
		//----------------------------------------------------------------------------
		//		アルファアウト用のブレンドパラメータ
		//----------------------------------------------------------------------------
		[HideInInspector] _BlendOp			("__blendop", Float) = 0.0				// Add
		[HideInInspector] _SrcBlendFactor	("__src"	, Float) = 1.0			// One
		[HideInInspector] _DstBlendFactor	("__dst"	, Float) = 0.0				// Zero
		[HideInInspector] _ZWrite			("__zw"		, Float) = 1.0				// On
		[HideInInspector] _FadeAlpha		("__fade_alpha", Range(0.0, 1.0)) = 1.0	// フェードアルファ値
	}
	
	//==========================================================================//
	//		SunShader									         				//
	//==========================================================================//
	SubShader
	{
		//----------------------------------------------------------------------------
		//		SubShader Status 								        				
		//----------------------------------------------------------------------------
		Tags{
			"RenderType"="Opaque"
			"LightMode"="ForwardBase"
			
			"Queue"="Geometry"
			
			// イマジンプロジェクトでの認可マーク
			// ※SetReplacementShaderでの置き換え用。SubShader直下に置く必要がある
			"AcqApproval"="OK"
		}
		LOD 100

		ZWrite ON
		
		//----------------------------------------------------------------------------
		//		Pass
		//----------------------------------------------------------------------------
		Pass
		{
			//----------------------------------------------------------------------------
			//		Pass Status 								        				
			//----------------------------------------------------------------------------
			BlendOp [_BlendOp]
			Blend [_SrcBlendFactor] [_DstBlendFactor]
			ZWrite [_ZWrite]
			
			//---------------------------
			// 描画時に無条件でステンシルバッファに値を書き込み
			// ※Bloomのマスク用
			//---------------------------
			Stencil
			{	
				Ref 0
				Comp Always
				Pass Replace
			}
			//----------------------------------------------------------------------------
			//		Pass RenderProgram
			//----------------------------------------------------------------------------
			CGPROGRAM
				#pragma fragmentoption ARB_precision_hint_fastest
				#pragma vertex vert
				#pragma fragment frag	
				#include "UnityCG.cginc"
				#include "AutoLight.cginc"

				#pragma multi_compile _ _FADE_ON
				
				struct v2f
				{
					float4 pos			: SV_POSITION;
					float3 normal		: TEXCOORD0;
					float2 uv			: TEXCOORD1;
					float3 eyeDir		: TEXCOORD2;
					float3 lightDir		: TEXCOORD3;
					fixed3 SHLighting	: TEXCOORD4;
				};
				
				//----------------------------------------------------------------------------
				//		グローバルパラメータ
				//----------------------------------------------------------------------------
				uniform fixed4	_LightProbeMultColor = fixed4(1,1,1,1);	// LightProbeの色味補正カラー

				//----------------------------------------------------------------------------
				// 
				//----------------------------------------------------------------------------
				sampler2D		_MainTex;		// 基本パラメータ：メインテクスチャ
				float4			_MainTex_ST;	// 基本パラメータ：メインテクスチャUV補正
				
				fixed4			_RimColor1;		// リムライト関連パラメータ：
				fixed4			_RimColor2;		// リムライト関連パラメータ：
				sampler2D		_RimTex;		// リムライト関連パラメータ：
				sampler2D		_RimMask;		// リムライト関連パラメータ：

				sampler2D		_RampTex;
				sampler2D		_FalloffTex;

				float			_SHLightingScale;
				float			_SHLightingSaturation;

				uniform half4	_CustomLightProbeRate;
				uniform float	_SHLightingDirection;

				fixed			_FadeAlpha;

				float4 Desaturate(float3 color, float Desaturation) 
				{ 
					float3 grayXfer = float3(0.3, 0.59, 0.11); 
					float grayf = dot(grayXfer, color); 
					float3 gray = float3(grayf, grayf, grayf); 

					return float4(lerp(color, gray, Desaturation), 1.0); 
				}

				v2f vert (appdata_full v)
				{
					v2f o;
					o.pos = UnityObjectToClipPos( v.vertex );
					o.uv = TRANSFORM_TEX( v.texcoord.xy, _MainTex );
					o.normal = normalize( mul( unity_ObjectToWorld, float4( v.normal, 0 ) ).xyz );

					// Eye direction vector
					float4 worldPos =  mul( unity_ObjectToWorld, v.vertex );
					o.eyeDir = normalize( _WorldSpaceCameraPos - worldPos );
//					o.lightDir = normalize ( WorldSpaceLightDir( v.vertex ));
					o.lightDir = normalize ( fixed3( 1 , 1 , 1 ) );

					// evaluate SH light
					float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);	

#if defined( NG )
//					fixed3 vLightProbeColor = ShadeSH9(float4(worldNormal,1));
//					vLightProbeColor.rgb = lerp( vLightProbeColor.rgb , _LightProbeMultColor.rgb , 1.0-vLightProbeColor.r );
//					o.SHLighting =  lerp( ShadeSH9(float4(0,1,0,1)).rgb , vLightProbeColor.rgb , _SHLightingDirection );
					o.SHLighting =  lerp( ShadeSH9(float4(0,1,0,1)).rgb , ShadeSH9(float4(worldNormal,1)) * _LightProbeMultColor.rgb , _SHLightingDirection );
#else
					fixed3 vLightProbeColor = ShadeSH9(float4(worldNormal,1));
					vLightProbeColor.rgb = lerp( vLightProbeColor.rgb , _LightProbeMultColor.rgb , (1.0-vLightProbeColor.r) * _LightProbeMultColor.a );
					o.SHLighting =  lerp( ShadeSH9(float4(0,1,0,1)).rgb , vLightProbeColor.rgb , _SHLightingDirection );

//					o.SHLighting =  lerp( ShadeSH9(float4(0,1,0,1)).rgb , ShadeSH9(float4(worldNormal,1)) * _LightProbeMultColor.rgb , _SHLightingDirection );
#endif

					// Unity5.3.1にしてからLightProbeがおとなしくなったので、白飛びしてた時の見た目に近くなるように単純に倍率をかける
					o.SHLighting = o.SHLighting * _CustomLightProbeRate.y;
					o.SHLighting = lerp ( float3(1,1,1), o.SHLighting.rgb, _CustomLightProbeRate.x * _SHLightingScale);			

					o.SHLighting = Desaturate(o.SHLighting, (1-_SHLightingSaturation));

					return o;
				}


				fixed4 frag (v2f i) : SV_Target
				{	
					#if defined( _FADE_ON )
						if ( _FadeAlpha <= 0 ) discard;
					#endif

					fixed4 col = tex2D(_MainTex, i.uv);

					fixed3 combinedColor = col.rgb;

					//fall off
					float normalDotEye=  dot (i.normal, i.eyeDir) ;
					float falloffU = clamp( 1.0 - abs( normalDotEye ), 0.02, 0.98 );
					fixed4 falloffSamplerColor = tex2D( _FalloffTex, float2( falloffU, 0.5f ) );
					combinedColor = combinedColor * falloffSamplerColor;

					// toon
					float normalDotLight =  dot (i.normal, i.lightDir) ;
					float ToonFalloffU = normalDotLight * 0.5 + 0.5;
					combinedColor = combinedColor * tex2D( _RampTex, float2( ToonFalloffU, 0.5f ) );

					// rimlight
					//float3 rimLightDir = normalize(mul( float4(1,0.7,0,0), UNITY_MATRIX_V ).xyz.rgb);
					float3 rimLightDir = i.lightDir;
					float rimlightDot = saturate( 0.5 * ( dot( i.normal, rimLightDir ) + 1.0 ) );
					fixed4 RimColor1 = _RimColor1 * tex2D( _RimTex, float2( falloffU*rimlightDot, 0.5f ) );

					// rimlight reverse
					fixed4 RimColor2 = _RimColor2 * tex2D( _RimTex, float2( falloffU*(-1*rimlightDot+1), 0.5f ) );

					// rimMask
					fixed4 rimMask = tex2D(_RimMask, i.uv);		
					combinedColor = combinedColor  + rimMask.rgb * ( RimColor1 + RimColor2 ) ;

					// ライトプローブ
					combinedColor.rgb *= i.SHLighting.rgb;

					#if defined( _FADE_ON )
						return  fixed4(combinedColor.rgb, _FadeAlpha);
					#else
						return  fixed4(combinedColor.rgb, col.a);
					#endif
				}
			ENDCG
			
		}
	}
		
	Fallback "VertexLit"
}
