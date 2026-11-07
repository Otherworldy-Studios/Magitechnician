// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/WaterFall_Transparent"
{
	Properties
	{
		_WaterColour("WaterColour", Color) = (0.1933517,0.745283,0.6866668,0)
		_Emission("Emission", Range( 0 , 1)) = 1
		_Opacity("Opacity", Range( 0 , 1)) = 1
		_WaterOverlayPower("WaterOverlayPower", Range( 0 , 1)) = 1
		_Smoothness("Smoothness", Range( 0 , 1)) = 1
		_Metallic("Metallic", Range( 0 , 1)) = 1
		_UVScrollSpeed("UVScroll Speed", Range( 0 , 1)) = 0.5
		_ColourMask("ColourMask", 2D) = "white" {}
		_VertOffsetPower("VertOffsetPower", Int) = 0
		[Toggle(_VERTEXOFFSET_TOGGLE_ON)] _VertexOffset_Toggle("VertexOffset_Toggle", Float) = 0
		_Dirt_Normals_01("Dirt_Normals_01", 2D) = "white" {}
		_FresnelPower("FresnelPower", Range( 0 , 1)) = 0
		_FresnelColour("FresnelColour", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "AlphaTest+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		Blend SrcAlpha OneMinusSrcAlpha
		
		CGINCLUDE
		#include "UnityShaderVariables.cginc"
		#include "UnityPBSLighting.cginc"
		#include "Lighting.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _VERTEXOFFSET_TOGGLE_ON
		#ifdef UNITY_PASS_SHADOWCASTER
			#undef INTERNAL_DATA
			#undef WorldReflectionVector
			#undef WorldNormalVector
			#define INTERNAL_DATA half3 internalSurfaceTtoW0; half3 internalSurfaceTtoW1; half3 internalSurfaceTtoW2;
			#define WorldReflectionVector(data,normal) reflect (data.worldRefl, half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal)))
			#define WorldNormalVector(data,normal) half3(dot(data.internalSurfaceTtoW0,normal), dot(data.internalSurfaceTtoW1,normal), dot(data.internalSurfaceTtoW2,normal))
		#endif
		struct Input
		{
			float2 uv_texcoord;
			float3 worldPos;
			float3 worldNormal;
			INTERNAL_DATA
		};

		uniform float _UVScrollSpeed;
		uniform int _VertOffsetPower;
		uniform sampler2D _Dirt_Normals_01;
		uniform float _Emission;
		uniform float4 _WaterColour;
		uniform sampler2D _ColourMask;
		uniform float _WaterOverlayPower;
		uniform float _FresnelPower;
		uniform float4 _FresnelColour;
		uniform float _Metallic;
		uniform float _Smoothness;
		uniform float _Opacity;


		float2 voronoihash10( float2 p )
		{
			p = p - 10 * floor( p / 10 );
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi10( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
		{
			float2 n = floor( v );
			float2 f = frac( v );
			float F1 = 8.0;
			float F2 = 8.0; float2 mg = 0;
			for ( int j = -2; j <= 2; j++ )
			{
				for ( int i = -2; i <= 2; i++ )
			 	{
			 		float2 g = float2( i, j );
			 		float2 o = voronoihash10( n + g );
					o = ( sin( time + o * 6.2831 ) * 0.5 + 0.5 ); float2 r = f - g - o;
					float d = 0.5 * dot( r, r );
			 		if( d<F1 ) {
			 			F2 = F1;
			 			F1 = d; mg = g; mr = r; id = o;
			 		} else if( d<F2 ) {
			 			F2 = d;
			
			 		}
			 	}
			}
			return (F2 + F1) * 0.5;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_INITIALIZE_OUTPUT( Input, o );
			float time10 = 0.0;
			float2 voronoiSmoothId0 = 0;
			float mulTime3 = _Time.y * _UVScrollSpeed;
			float2 uv_TexCoord4 = v.texcoord.xy * float2( 2,1 );
			float2 panner5 = ( mulTime3 * float2( 0,1 ) + uv_TexCoord4);
			float2 coords10 = panner5 * 1.0;
			float2 id10 = 0;
			float2 uv10 = 0;
			float fade10 = 0.5;
			float voroi10 = 0;
			float rest10 = 0;
			for( int it10 = 0; it10 <5; it10++ ){
			voroi10 += fade10 * voronoi10( coords10, time10, id10, uv10, 0,voronoiSmoothId0 );
			rest10 += fade10;
			coords10 *= 2;
			fade10 *= 0.5;
			}//Voronoi10
			voroi10 /= rest10;
			#ifdef _VERTEXOFFSET_TOGGLE_ON
				float staticSwitch27 = ( voroi10 * _VertOffsetPower );
			#else
				float staticSwitch27 = 0.0;
			#endif
			float3 temp_cast_0 = (staticSwitch27).xxx;
			v.vertex.xyz += temp_cast_0;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float mulTime11 = _Time.y * _UVScrollSpeed;
			float2 uv_TexCoord15 = i.uv_texcoord * float2( 2,1 );
			float2 panner20 = ( mulTime11 * float2( 0,1 ) + uv_TexCoord15);
			o.Normal = tex2D( _Dirt_Normals_01, panner20 ).rgb;
			float mulTime3 = _Time.y * _UVScrollSpeed;
			float2 uv_TexCoord4 = i.uv_texcoord * float2( 2,1 );
			float2 panner5 = ( mulTime3 * float2( 0,1 ) + uv_TexCoord4);
			float4 blendOpSrc24 = ( _Emission * ( _WaterColour * tex2D( _ColourMask, panner5 ) ) );
			float4 blendOpDest24 = _WaterColour;
			float4 lerpBlendMode24 = lerp(blendOpDest24,( 1.0 - ( 1.0 - blendOpSrc24 ) * ( 1.0 - blendOpDest24 ) ),_WaterOverlayPower);
			o.Albedo = ( saturate( lerpBlendMode24 )).rgb;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float fresnelNdotV14 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode14 = ( 0.0 + 0.63 * pow( 1.0 - fresnelNdotV14, 2.44 ) );
			o.Emission = ( min( fresnelNode14 , _FresnelPower ) * _FresnelColour ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = _Opacity;
		}

		ENDCG
		CGPROGRAM
		#pragma surface surf Standard keepalpha fullforwardshadows vertex:vertexDataFunc 

		ENDCG
		Pass
		{
			Name "ShadowCaster"
			Tags{ "LightMode" = "ShadowCaster" }
			ZWrite On
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 3.0
			#pragma multi_compile_shadowcaster
			#pragma multi_compile UNITY_PASS_SHADOWCASTER
			#pragma skip_variants FOG_LINEAR FOG_EXP FOG_EXP2
			#include "HLSLSupport.cginc"
			#if ( SHADER_API_D3D11 || SHADER_API_GLCORE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_METAL || SHADER_API_VULKAN )
				#define CAN_SKIP_VPOS
			#endif
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "UnityPBSLighting.cginc"
			sampler3D _DitherMaskLOD;
			struct v2f
			{
				V2F_SHADOW_CASTER;
				float2 customPack1 : TEXCOORD1;
				float4 tSpace0 : TEXCOORD2;
				float4 tSpace1 : TEXCOORD3;
				float4 tSpace2 : TEXCOORD4;
				UNITY_VERTEX_INPUT_INSTANCE_ID
				UNITY_VERTEX_OUTPUT_STEREO
			};
			v2f vert( appdata_full v )
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID( v );
				UNITY_INITIALIZE_OUTPUT( v2f, o );
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO( o );
				UNITY_TRANSFER_INSTANCE_ID( v, o );
				Input customInputData;
				vertexDataFunc( v, customInputData );
				float3 worldPos = mul( unity_ObjectToWorld, v.vertex ).xyz;
				half3 worldNormal = UnityObjectToWorldNormal( v.normal );
				half3 worldTangent = UnityObjectToWorldDir( v.tangent.xyz );
				half tangentSign = v.tangent.w * unity_WorldTransformParams.w;
				half3 worldBinormal = cross( worldNormal, worldTangent ) * tangentSign;
				o.tSpace0 = float4( worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x );
				o.tSpace1 = float4( worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y );
				o.tSpace2 = float4( worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z );
				o.customPack1.xy = customInputData.uv_texcoord;
				o.customPack1.xy = v.texcoord;
				TRANSFER_SHADOW_CASTER_NORMALOFFSET( o )
				return o;
			}
			half4 frag( v2f IN
			#if !defined( CAN_SKIP_VPOS )
			, UNITY_VPOS_TYPE vpos : VPOS
			#endif
			) : SV_Target
			{
				UNITY_SETUP_INSTANCE_ID( IN );
				Input surfIN;
				UNITY_INITIALIZE_OUTPUT( Input, surfIN );
				surfIN.uv_texcoord = IN.customPack1.xy;
				float3 worldPos = float3( IN.tSpace0.w, IN.tSpace1.w, IN.tSpace2.w );
				half3 worldViewDir = normalize( UnityWorldSpaceViewDir( worldPos ) );
				surfIN.worldPos = worldPos;
				surfIN.worldNormal = float3( IN.tSpace0.z, IN.tSpace1.z, IN.tSpace2.z );
				surfIN.internalSurfaceTtoW0 = IN.tSpace0.xyz;
				surfIN.internalSurfaceTtoW1 = IN.tSpace1.xyz;
				surfIN.internalSurfaceTtoW2 = IN.tSpace2.xyz;
				SurfaceOutputStandard o;
				UNITY_INITIALIZE_OUTPUT( SurfaceOutputStandard, o )
				surf( surfIN, o );
				#if defined( CAN_SKIP_VPOS )
				float2 vpos = IN.pos;
				#endif
				half alphaRef = tex3D( _DitherMaskLOD, float3( vpos.xy * 0.25, o.Alpha * 0.9375 ) ).a;
				clip( alphaRef - 0.01 );
				SHADOW_CASTER_FRAGMENT( IN )
			}
			ENDCG
		}
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18909
-3411;13;2946;1351;2428.583;829.7703;1.3;True;True
Node;AmplifyShaderEditor.RangedFloatNode;1;-2600.454,-168.0422;Float;False;Property;_UVScrollSpeed;UVScroll Speed;7;0;Create;True;0;0;0;False;0;False;0.5;0.717;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;2;-2583.357,-511.4357;Inherit;False;Constant;_Vector0;Vector 0;14;0;Create;True;0;0;0;False;0;False;2,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleTimeNode;3;-2255.254,-272.6422;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;4;-2429.254,-485.8422;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;5;-1994.055,-539.2424;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;6;-1733.814,-555.2261;Inherit;False;Property;_WaterColour;WaterColour;0;0;Create;True;0;0;0;False;0;False;0.1933517,0.745283,0.6866668,0;0.4424616,0.5274525,0.5754716,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;7;-1982.081,-223.7788;Inherit;True;Property;_ColourMask;ColourMask;8;0;Create;True;0;0;0;False;0;False;-1;cb3f411ea46ea3342912d9a41154e5e5;cb3f411ea46ea3342912d9a41154e5e5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;8;-1581.927,491.1303;Inherit;False;Constant;_Vector1;Vector 1;14;0;Create;True;0;0;0;False;0;False;2,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;9;-1628.168,-15.41569;Inherit;False;Property;_Emission;Emission;2;0;Create;True;0;0;0;False;0;False;1;1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleTimeNode;11;-1088.073,876.5031;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;15;-1262.073,663.3031;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;13;-1475.284,-324.7002;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-743.3003,-660.7462;Inherit;False;Property;_FresnelPower;FresnelPower;12;0;Create;True;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;10;-1109.956,163.7666;Inherit;True;1;0;1;3;5;True;10;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.IntNode;12;-1047.615,452.2958;Inherit;False;Property;_VertOffsetPower;VertOffsetPower;9;0;Create;True;0;0;0;False;0;False;0;0;False;0;1;INT;0
Node;AmplifyShaderEditor.FresnelNode;14;-796.2922,-943.0615;Inherit;True;Standard;TangentNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0.63;False;3;FLOAT;2.44;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;19;-1260.481,-288.5078;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;22;-869.3485,251.4493;Inherit;False;2;2;0;FLOAT;0;False;1;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;17;-381.7032,-596.4398;Inherit;False;Property;_FresnelColour;FresnelColour;13;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.9858491,0.9888815,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;20;-826.873,609.9031;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMinOpNode;21;-409.4052,-920.4124;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;18;-1035.318,-86.23483;Inherit;False;Property;_WaterOverlayPower;WaterOverlayPower;4;0;Create;True;0;0;0;False;0;False;1;0.751;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;28;-546.1806,507.1683;Inherit;True;Property;_Dirt_Normals_01;Dirt_Normals_01;11;0;Create;True;0;0;0;False;0;False;-1;abc7d86babc385d4c8707eb4c4d8aecd;6f7201a46a43e824bb35cd183f7a5e20;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StaticSwitch;27;-454.149,214.7603;Inherit;False;Property;_VertexOffset_Toggle;VertexOffset_Toggle;10;0;Create;True;0;0;0;False;0;False;0;0;0;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;24;-727.1489,-301.2397;Inherit;True;Screen;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;23;-440.3883,21.65127;Inherit;False;Property;_Metallic;Metallic;6;0;Create;True;0;0;0;False;0;False;1;0.299;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;29;-53.63574,176.6318;Inherit;False;Property;_Opacity;Opacity;3;0;Create;True;0;0;0;False;0;False;1;0.615;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;26;-439.6601,104.673;Inherit;False;Property;_Smoothness;Smoothness;5;0;Create;True;0;0;0;False;0;False;1;0.294;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;25;-109.8468,-802.723;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;309.1113,-25.30738;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;SyntyStudios/WaterFall_Transparent;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Custom;0.5;True;True;0;True;Transparent;;AlphaTest;All;16;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;3;0;1;0
WireConnection;4;0;2;0
WireConnection;5;0;4;0
WireConnection;5;1;3;0
WireConnection;7;1;5;0
WireConnection;11;0;1;0
WireConnection;15;0;8;0
WireConnection;13;0;6;0
WireConnection;13;1;7;0
WireConnection;10;0;5;0
WireConnection;19;0;9;0
WireConnection;19;1;13;0
WireConnection;22;0;10;0
WireConnection;22;1;12;0
WireConnection;20;0;15;0
WireConnection;20;1;11;0
WireConnection;21;0;14;0
WireConnection;21;1;16;0
WireConnection;28;1;20;0
WireConnection;27;0;22;0
WireConnection;24;0;19;0
WireConnection;24;1;6;0
WireConnection;24;2;18;0
WireConnection;25;0;21;0
WireConnection;25;1;17;0
WireConnection;0;0;24;0
WireConnection;0;1;28;0
WireConnection;0;2;25;0
WireConnection;0;3;23;0
WireConnection;0;4;26;0
WireConnection;0;9;29;0
WireConnection;0;11;27;0
ASEEND*/
//CHKSM=9E1FDA325FA66AEAB49F3827A419027FA826190E