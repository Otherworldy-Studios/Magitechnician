// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/WaterFall"
{
	Properties
	{
		_WaterColour("WaterColour", Color) = (0.1933517,0.745283,0.6866668,0)
		_Emission("Emission", Range( 0 , 1)) = 1
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
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma shader_feature_local _VERTEXOFFSET_TOGGLE_ON
		#pragma surface surf Standard keepalpha exclude_path:deferred vertex:vertexDataFunc 
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


		float2 voronoihash40( float2 p )
		{
			p = p - 10 * floor( p / 10 );
			p = float2( dot( p, float2( 127.1, 311.7 ) ), dot( p, float2( 269.5, 183.3 ) ) );
			return frac( sin( p ) *43758.5453);
		}


		float voronoi40( float2 v, float time, inout float2 id, inout float2 mr, float smoothness, inout float2 smoothId )
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
			 		float2 o = voronoihash40( n + g );
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
			float time40 = 0.0;
			float2 voronoiSmoothId0 = 0;
			float mulTime13 = _Time.y * _UVScrollSpeed;
			float2 uv_TexCoord12 = v.texcoord.xy * float2( 2,1 );
			float2 panner14 = ( mulTime13 * float2( 0,1 ) + uv_TexCoord12);
			float2 coords40 = panner14 * 1.0;
			float2 id40 = 0;
			float2 uv40 = 0;
			float fade40 = 0.5;
			float voroi40 = 0;
			float rest40 = 0;
			for( int it40 = 0; it40 <5; it40++ ){
			voroi40 += fade40 * voronoi40( coords40, time40, id40, uv40, 0,voronoiSmoothId0 );
			rest40 += fade40;
			coords40 *= 2;
			fade40 *= 0.5;
			}//Voronoi40
			voroi40 /= rest40;
			#ifdef _VERTEXOFFSET_TOGGLE_ON
				float staticSwitch70 = ( voroi40 * _VertOffsetPower );
			#else
				float staticSwitch70 = 0.0;
			#endif
			float3 temp_cast_0 = (staticSwitch70).xxx;
			v.vertex.xyz += temp_cast_0;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float mulTime45 = _Time.y * _UVScrollSpeed;
			float2 uv_TexCoord47 = i.uv_texcoord * float2( 2,1 );
			float2 panner46 = ( mulTime45 * float2( 0,1 ) + uv_TexCoord47);
			o.Normal = tex2D( _Dirt_Normals_01, panner46 ).rgb;
			float mulTime13 = _Time.y * _UVScrollSpeed;
			float2 uv_TexCoord12 = i.uv_texcoord * float2( 2,1 );
			float2 panner14 = ( mulTime13 * float2( 0,1 ) + uv_TexCoord12);
			float4 blendOpSrc68 = ( _Emission * ( _WaterColour * tex2D( _ColourMask, panner14 ) ) );
			float4 blendOpDest68 = _WaterColour;
			float4 lerpBlendMode68 = lerp(blendOpDest68,( 1.0 - ( 1.0 - blendOpSrc68 ) * ( 1.0 - blendOpDest68 ) ),_WaterOverlayPower);
			o.Albedo = ( saturate( lerpBlendMode68 )).rgb;
			float3 ase_worldPos = i.worldPos;
			float3 ase_worldViewDir = normalize( UnityWorldSpaceViewDir( ase_worldPos ) );
			float3 ase_worldNormal = WorldNormalVector( i, float3( 0, 0, 1 ) );
			float fresnelNdotV60 = dot( ase_worldNormal, ase_worldViewDir );
			float fresnelNode60 = ( 0.0 + 0.63 * pow( 1.0 - fresnelNdotV60, 2.44 ) );
			o.Emission = ( min( fresnelNode60 , _FresnelPower ) * _FresnelColour ).rgb;
			o.Metallic = _Metallic;
			o.Smoothness = _Smoothness;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18909
-2807;1004;2382;832;2013.768;1037.861;1;True;True
Node;AmplifyShaderEditor.RangedFloatNode;11;-1280.074,-236.6635;Float;False;Property;_UVScrollSpeed;UVScroll Speed;5;0;Create;True;0;0;0;False;0;False;0.5;0.734;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.Vector2Node;67;-1262.977,-580.0569;Inherit;False;Constant;_Vector2;Vector 2;14;0;Create;True;0;0;0;False;0;False;2,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleTimeNode;13;-934.8741,-341.2635;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;12;-1108.874,-554.4634;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.PannerNode;14;-673.6741,-607.8635;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.ColorNode;2;-413.4337,-623.8473;Inherit;False;Property;_WaterColour;WaterColour;0;0;Create;True;0;0;0;False;0;False;0.1933517,0.745283,0.6866668,0;0.5403169,0.5871752,0.6132076,1;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;5;-661.7,-292.4;Inherit;True;Property;_ColourMask;ColourMask;6;0;Create;True;0;0;0;False;0;False;-1;cb3f411ea46ea3342912d9a41154e5e5;cb3f411ea46ea3342912d9a41154e5e5;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;50;396.4456,859.965;Inherit;False;Constant;_Vector1;Vector 1;14;0;Create;True;0;0;0;False;0;False;2,1;0,0;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.RangedFloatNode;19;-307.7869,-84.03695;Inherit;False;Property;_Emission;Emission;1;0;Create;True;0;0;0;False;0;False;1;0.813;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.VoronoiNode;40;150.7721,368.1033;Inherit;True;1;0;1;3;5;True;10;False;False;False;4;0;FLOAT2;0,0;False;1;FLOAT;0;False;2;FLOAT;1;False;3;FLOAT;0;False;3;FLOAT;0;FLOAT2;1;FLOAT2;2
Node;AmplifyShaderEditor.SimpleTimeNode;45;890.2989,1245.338;Inherit;False;1;0;FLOAT;1;False;1;FLOAT;0
Node;AmplifyShaderEditor.IntNode;42;417.3789,692.786;Inherit;False;Property;_VertOffsetPower;VertOffsetPower;7;0;Create;True;0;0;0;False;0;False;0;1;False;0;1;INT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;10;-154.9034,-393.3214;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.FresnelNode;60;1637.612,-73.50266;Inherit;True;Standard;TangentNormal;ViewDir;False;False;5;0;FLOAT3;0,0,1;False;4;FLOAT3;0,0,0;False;1;FLOAT;0;False;2;FLOAT;0.63;False;3;FLOAT;2.44;False;1;FLOAT;0
Node;AmplifyShaderEditor.TextureCoordinatesNode;47;716.299,1032.138;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;63;2095.522,510.6931;Inherit;False;Property;_FresnelPower;FresnelPower;10;0;Create;True;0;0;0;False;0;False;0;0.1;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;66;2267.314,124.89;Inherit;False;Property;_FresnelColour;FresnelColour;11;0;Create;True;0;0;0;False;0;False;0,0,0,0;0.9858491,0.9888815,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;21;285.0625,-154.8561;Inherit;False;Property;_WaterOverlayPower;WaterOverlayPower;2;0;Create;True;0;0;0;False;0;False;1;0.297;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;17;59.89982,-357.1291;Inherit;True;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;46;1151.499,978.7379;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,1;False;1;FLOAT;2;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMinOpNode;61;2102.229,-170.1598;Inherit;True;2;0;FLOAT;0;False;1;FLOAT;0.5;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;41;391.3789,455.786;Inherit;False;2;2;0;FLOAT;0;False;1;INT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.RangedFloatNode;16;-244.8992,238.4811;Inherit;False;Property;_Metallic;Metallic;4;0;Create;True;0;0;0;False;0;False;1;0.048;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.BlendOpsNode;68;593.2318,-369.861;Inherit;True;Screen;True;3;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;2;FLOAT;1;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;65;2481.325,-68.73947;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;15;-532.8992,78.48108;Inherit;False;Property;_Smoothness;Smoothness;3;0;Create;True;0;0;0;False;0;False;1;0.029;0;1;0;1;FLOAT;0
Node;AmplifyShaderEditor.StaticSwitch;70;866.2318,146.139;Inherit;False;Property;_VertexOffset_Toggle;VertexOffset_Toggle;8;0;Create;True;0;0;0;False;0;False;0;0;1;True;;Toggle;2;Key0;Key1;Create;True;True;9;1;FLOAT;0;False;0;FLOAT;0;False;2;FLOAT;0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT;0;False;7;FLOAT;0;False;8;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;43;774.2002,438.5471;Inherit;True;Property;_Dirt_Normals_01;Dirt_Normals_01;9;0;Create;True;0;0;0;False;0;False;-1;abc7d86babc385d4c8707eb4c4d8aecd;6f7201a46a43e824bb35cd183f7a5e20;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1315.7,-210.1;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;SyntyStudios/WaterFall;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Opaque;0.5;True;False;0;False;Opaque;;Geometry;ForwardOnly;16;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;13;0;11;0
WireConnection;12;0;67;0
WireConnection;14;0;12;0
WireConnection;14;1;13;0
WireConnection;5;1;14;0
WireConnection;40;0;14;0
WireConnection;45;0;11;0
WireConnection;10;0;2;0
WireConnection;10;1;5;0
WireConnection;47;0;50;0
WireConnection;17;0;19;0
WireConnection;17;1;10;0
WireConnection;46;0;47;0
WireConnection;46;1;45;0
WireConnection;61;0;60;0
WireConnection;61;1;63;0
WireConnection;41;0;40;0
WireConnection;41;1;42;0
WireConnection;68;0;17;0
WireConnection;68;1;2;0
WireConnection;68;2;21;0
WireConnection;65;0;61;0
WireConnection;65;1;66;0
WireConnection;70;0;41;0
WireConnection;43;1;46;0
WireConnection;0;0;68;0
WireConnection;0;1;43;0
WireConnection;0;2;65;0
WireConnection;0;3;16;0
WireConnection;0;4;15;0
WireConnection;0;11;70;0
ASEEND*/
//CHKSM=636AFFE09E97D60E662A107532B108F2426A96D8