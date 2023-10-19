// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "Imagine/Character/Chara" 
{
	Properties
	{
		_SkinColor ("Skin Color", Color) = (1,1,1,1)
		_RimColor1 ("RimColor1", Color) = (.5,.5,.5,1)
		_RimColor2 ("RimColor2", Color) = (.5,.5,.5,1)
		_MainTex ("Base (RGB) ", 2D) = "white" {}
		_RampTex ("ToonRamp (RGB) ", 2D) = "white" {}
		_RimTex ("Rim (RGB) ", 2D) = "white" {} 
		_FalloffTex ("Falloff (RGB) ", 2D) = "white" {} 
		[MaterialToggle] _EyeClose ("Eye Close", Float) = 0  
		_EyeTex ("Eye (RGB) ", 2D) = "white" {}  
		_RimMask ("RimMask (RGB) ", 2D) = "white" {} 
		_SHLightingDirection ("LightProbe Direction",range(0,1)) = 0
		_SHLightingScale ("LightProbe influence scale",range(0,1)) = 0
		_SHLightingSaturation ("LightProbe Saturation",range(0,1)) = 1
		_SkinShadowColor ("Skin Shadow Color", Color) = (1,1,1,1)
		_SkinShadowTex ("SkinShadow (RGB) ", 2D) = "black" {}

		[MaterialToggle] _EnvOn ("Env Specular", Float) = 0 //
		_EnvTex ("Cube env tex", CUBE) = "black" {}		// 擬似スペキュラ用キューブマップ

		_specularPower ("Specular Intensity", Range(0.0, 50.0)) = 10
		_specularColor("Specular Color", Color) = (1,1,1,1)
		_anisotropicX ("Anisotropic X", Range(0.0, 1.0)) = 1
		_anisotropicY ("Anisotropic Y", Range(0.0, 1.0)) = 0.1

		_BloomMask ("Bloom Mask", Range(0.0, 10.0)) = 0 

		// ブレンド設定
		[HideInInspector] _BlendOp ("__blendop", Float) = 0.0		// Add
		[HideInInspector] _SrcBlendFactor ("__src", Float) = 1.0	// One
		[HideInInspector] _DstBlendFactor ("__dst", Float) = 0.0	// Zero
		[HideInInspector] _ZWrite ("__zw", Float) = 1.0			// On

		// フェードアルファ値
		[HideInInspector] _FadeAlpha ("__fade_alpha", Range(0.0, 1.0)) = 1.0
	}

	SubShader
	{
		Tags {
			"RenderType"="Opaque"
			"LightMode"="ForwardBase"
			
			"Queue"="Geometry"
			
			// イマジンプロジェクトでの認可マーク
			// ※SetReplacementShaderでの置き換え用。SubShader直下に置く必要がある
			"AcqApproval"="OK"
		}
		LOD 100

		ZWrite ON

		Pass
		{
			BlendOp [_BlendOp]
			Blend [_SrcBlendFactor] [_DstBlendFactor]
			ZWrite [_ZWrite]

			CGPROGRAM
			#pragma fragmentoption ARB_precision_hint_fastest
			#pragma vertex vert
			#pragma fragment frag	
			#include "UnityCG.cginc"
			#include "AutoLight.cginc"

			#pragma multi_compile _ _FADE_ON

			struct v2f
			{
				float4 pos    : SV_POSITION;
				float3 normal : TEXCOORD0;
				float2 uv     : TEXCOORD1;
				float3 eyeDir : TEXCOORD2;
				float3 lightDir : TEXCOORD3;
				float2 uv2		: TEXCOORD4;
				fixed3 SHLighting: TEXCOORD5;
				float3 refl	: TEXCOORD6;

				fixed3 binormalDir : TEXCOORD7;	
				fixed3 tangentDir :	TEXCOORD8;	
			};

			fixed4 _SkinColor;
			fixed4 _RimColor1;
			fixed4 _RimColor2;
			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _RampTex;
			sampler2D _RimTex;
			sampler2D _RimMask;
			sampler2D _FalloffTex;
			float _EyeClose;
			sampler2D _EyeTex;
			float4 _EyeTex_ST;
			float _SHLightingScale;
			float _SHLightingSaturation;
			fixed4 _SkinShadowColor;
			sampler2D _SkinShadowTex;
			samplerCUBE	_EnvTex;

			float _EnvOn;
			uniform fixed3 _specularColor;
			uniform half _specularPower;
			uniform fixed _anisotropicX;
			uniform fixed _anisotropicY;

			uniform half4 _CustomLightProbeRate;
			uniform float _SHLightingDirection;

			float _BloomMask;
			fixed _FadeAlpha;

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
				o.uv2 = TRANSFORM_TEX( v.texcoord.xy, _EyeTex );
				o.normal = normalize( mul( unity_ObjectToWorld, float4( v.normal, 0 ) ).xyz );

				// Eye direction vector
				float4 worldPos =  mul( unity_ObjectToWorld, v.vertex );
				o.eyeDir = normalize( _WorldSpaceCameraPos - worldPos );
//				o.lightDir = normalize ( WorldSpaceLightDir( v.vertex ));
				o.lightDir = normalize ( fixed3( 1 , 1 , 1 ) );

				// evaluate SH light
				float3 worldNormal = mul((float3x3)unity_ObjectToWorld, v.normal);	

				//o.SHLighting = lerp ( float3(1,1,1), ShadeSH9(float4(worldNormal,1)), _SHLightingScale);
				//o.SHLighting = lerp ( float3(1,1,1), ShadeSH9(float4(0,1,0,1)).rgb, _SHLightingScale);

				//o.SHLighting = lerp ( float3(1,1,1), ShadeSH9(float4(0,1,0,1		)).rgb , _CustomLightProbeRate.x * _SHLightingScale );
				//o.SHLighting = lerp ( float3(1,1,1), ShadeSH9(float4(worldNormal,1)).rgb , _CustomLightProbeRate.x * _SHLightingScale );


				o.SHLighting =  lerp ( ShadeSH9(float4(0,1,0,1)).rgb,ShadeSH9(float4(worldNormal,1)), _SHLightingDirection);

				// Unity5.3.1にしてからLightProbeがおとなしくなったので、白飛びしてた時の見た目に近くなるように単純に倍率をかける
				o.SHLighting = o.SHLighting * _CustomLightProbeRate.y;
				/*
				// LightProbeの白飛び暫定対応
				float fLightProbeColMax = max( o.SHLighting.r , max( o.SHLighting.g , o.SHLighting.b ) );
				float fLightProbeRate = 1.1;
				if( fLightProbeColMax > fLightProbeRate )
				{
					o.SHLighting = ( o.SHLighting / fLightProbeColMax * fLightProbeRate );
//					o.SHLighting = float4(1,0,0,1);
				}
				*/
				o.SHLighting = lerp ( float3(1,1,1), o.SHLighting.rgb, _CustomLightProbeRate.x * _SHLightingScale);			

				o.SHLighting = Desaturate(o.SHLighting, (1-_SHLightingSaturation));

				o.refl = reflect(-WorldSpaceViewDir(v.vertex), worldNormal);
				o.refl.x = -o.refl.x;

				o.tangentDir = normalize(mul(v.tangent, unity_WorldToObject).xyz);
				o.binormalDir = normalize(cross(o.normal, o.tangentDir)); 							

				return o;
			}

			fixed ward(fixed3 V, fixed3 T, fixed3 B, fixed3 N, fixed3 L)
			{
				//calculate the half vecor like blinn
				fixed3 H = normalize(V + L);
				//calculate per pixel tangent and binormals
				//technically i think i should be normalizing these...but i never saw a difference
				//i think its because i'm taking the cross of 2 normalized vectors resulting in another normalized vector
				//if issues are noticed try normalizing these
				fixed3 tangentDir = cross(B, N);
				fixed3 binormalDir = cross(T, N);			

				fixed ndotL = dot(N, L);
				fixed ndotH = dot(N, H);
				fixed ndotV = dot(N, V);
				fixed tdotHX = dot(tangentDir, H) / _anisotropicX ;
				fixed bdotHY = dot(binormalDir, H)  / _anisotropicY;

				//my approximate specular rim - (pow( (1-ndotV), 3) * 3)
				fixed specRim =  (1-ndotV);
				specRim *= specRim * specRim;
				specRim *= 3;

				fixed specularHighlight = exp( -(tdotHX*tdotHX + bdotHY*bdotHY ) ) * (1+specRim);
				//divide by pi
				specularHighlight /= 3.14159 ;
				//multiply by ndotL and specular multiplier and return				
				return specularHighlight * ndotL * _specularPower;
			}

			fixed4 frag (v2f i) : SV_Target
			{	
				#if defined( _FADE_ON )
					if ( _FadeAlpha <= 0 ) discard;
				#endif

				fixed4 col = tex2D(_MainTex, i.uv);

				fixed4 eye = tex2D(_EyeTex, i.uv2);

				fixed4 skinShadow = tex2D(_SkinShadowTex, i.uv);

				// eye texture
				fixed3 combinedColor;
				combinedColor = col.rgb;
				if( _EyeClose ) combinedColor = lerp ( combinedColor.rgb, eye.rgb, eye.a);	

				// skinShadow
				combinedColor = lerp ( combinedColor.rgb, _SkinShadowColor.rgb, skinShadow.r);	

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

				combinedColor.rgb *= _SkinColor;

				// 擬似スペキュラー
				combinedColor.rgb += texCUBE(_EnvTex,i.refl) * col.a * _EnvOn;

				// aniso
				fixed specularHighlight = ward(i.eyeDir, i.tangentDir, i.binormalDir, i.normal, i.lightDir);
				combinedColor.rgb += specularHighlight*_specularColor;		

				// ライトプローブ
				combinedColor.rgb *= i.SHLighting.rgb;
				//combinedColor.rgb *= lerp ( float3(1,1,1), i.SHLighting.rgb, _SHLightingScale);

				#if defined( _FADE_ON )
					return  fixed4(combinedColor.rgb, _FadeAlpha);
				#else
					return  fixed4(combinedColor.rgb, col.a);
				#endif
			}
			ENDCG

			Stencil
			{	
				Ref [_BloomMask]
				Comp Always
				Pass Replace
			}
		}
	}
	Fallback "VertexLit"
}
