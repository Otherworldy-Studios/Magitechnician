// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "SyntyStudios/Aurora"
{
	Properties
	{
		_Brightness("Brightness", Range( 0 , 5)) = 1
		_TextureSample0("Texture Sample 0", 2D) = "white" {}
		_TextureSample4("Texture Sample 4", 2D) = "white" {}
		_Color_Up("Color_Up", Color) = (0.2269654,1,0,0)
		_Color_Down("Color_Down", Color) = (0.2269654,1,0,0)
		_TextureSample2("Texture Sample 2", 2D) = "white" {}
		_TextureSample3("Texture Sample 3", 2D) = "white" {}
		_Speed("Speed", Vector) = (1,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Transparent"  "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true"  }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma surface surf Standard alpha:fade keepalpha exclude_path:deferred 
		struct Input
		{
			float2 uv_texcoord;
		};

		uniform float4 _Color_Up;
		uniform sampler2D _TextureSample3;
		uniform float4 _TextureSample3_ST;
		uniform sampler2D _TextureSample2;
		uniform float4 _TextureSample2_ST;
		uniform float4 _Color_Down;
		uniform sampler2D _TextureSample0;
		uniform float2 _Speed;
		uniform float _Brightness;
		uniform sampler2D _TextureSample4;
		uniform float4 _TextureSample4_ST;
		uniform sampler2D _TextureSample1;
		uniform float4 _TextureSample1_ST;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_TextureSample3 = i.uv_texcoord * _TextureSample3_ST.xy + _TextureSample3_ST.zw;
			float2 uv_TextureSample2 = i.uv_texcoord * _TextureSample2_ST.xy + _TextureSample2_ST.zw;
			float mulTime76 = _Time.y * 0.02;
			float2 panner74 = ( mulTime76 * _Speed + i.uv_texcoord);
			float4 tex2DNode75 = tex2D( _TextureSample0, panner74 );
			float2 uv_TextureSample4 = i.uv_texcoord * _TextureSample4_ST.xy + _TextureSample4_ST.zw;
			o.Emission = ( ( ( ( _Color_Up * tex2D( _TextureSample3, uv_TextureSample3 ) ) + ( tex2D( _TextureSample2, uv_TextureSample2 ) * _Color_Down ) ) * tex2DNode75 ) * ( _Brightness * tex2D( _TextureSample4, uv_TextureSample4 ) ) ).rgb;
			float2 uv_TextureSample1 = i.uv_texcoord * _TextureSample1_ST.xy + _TextureSample1_ST.zw;
			o.Alpha = ( tex2DNode75.a * tex2D( _TextureSample1, uv_TextureSample1 ).a );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=18909
-2046;529.2;1842;998;576.3679;839.8651;1.430097;True;False
Node;AmplifyShaderEditor.TextureCoordinatesNode;71;503.8651,-242.1998;Inherit;False;0;-1;2;3;2;SAMPLER2D;;False;0;FLOAT2;1,1;False;1;FLOAT2;0,0;False;5;FLOAT2;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;85;658.429,-657.3755;Inherit;True;Property;_TextureSample2;Texture Sample 2;6;0;Create;True;0;0;0;False;0;False;-1;None;f0c66d21610fe0c4f995b8c65c020587;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleTimeNode;76;548.8651,40.80017;Inherit;False;1;0;FLOAT;0.02;False;1;FLOAT;0
Node;AmplifyShaderEditor.ColorNode;84;643.4289,-462.3755;Inherit;False;Property;_Color_Down;Color_Down;5;0;Create;True;0;0;0;False;0;False;0.2269654,1,0,0;0,0.9896681,1,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;87;630.429,-1030.375;Inherit;True;Property;_TextureSample3;Texture Sample 3;7;0;Create;True;0;0;0;False;0;False;-1;None;7a412de49dc50154e8942ce1a5e11b32;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.ColorNode;82;641.429,-839.3755;Inherit;False;Property;_Color_Up;Color_Up;4;0;Create;True;0;0;0;False;0;False;0.2269654,1,0,0;0.5549389,0,0.7075471,0;True;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.Vector2Node;73;556.8651,-106.1998;Float;False;Property;_Speed;Speed;8;0;Create;True;0;0;0;False;0;False;1,0;0,-80;0;3;FLOAT2;0;FLOAT;1;FLOAT;2
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;88;1004.429,-525.3755;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.PannerNode;74;834.8651,-183.1998;Inherit;False;3;0;FLOAT2;0,0;False;2;FLOAT2;0,0;False;1;FLOAT;1;False;1;FLOAT2;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;83;1057.429,-804.3755;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;96;1302.796,-906.9232;Inherit;False;Property;_Brightness;Brightness;0;0;Create;True;0;0;0;False;0;False;1;5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;75;1039.865,-214.1998;Inherit;True;Property;_TextureSample0;Texture Sample 0;2;0;Create;True;0;0;0;False;0;False;-1;None;89f0d1856255d694bbd2e380e77d64af;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SamplerNode;98;1243.086,-632.3451;Inherit;True;Property;_TextureSample4;Texture Sample 4;3;0;Create;True;0;0;0;False;0;False;-1;None;dc6b37c0e06ae4340a593f9b02730eb6;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;90;1254.047,-763.7762;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;97;1561.341,-602.6476;Inherit;False;2;2;0;FLOAT;0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;79;1353.429,-363.3755;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;78;847.4104,263.0626;Inherit;True;Global;_TextureSample1;Texture Sample 1;4;0;Create;True;0;0;0;False;0;False;-1;None;None;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;8;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;6;FLOAT;0;False;7;SAMPLERSTATE;;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;92;1165.937,10.64917;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;77;1333.41,133.0626;Inherit;False;2;2;0;FLOAT;0;False;1;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;95;1701.796,-453.9232;Inherit;False;2;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;94;787.3519,77.18646;Inherit;False;Property;_Float0;Float 0;1;0;Create;True;0;0;0;False;0;False;1;5;0;5;0;1;FLOAT;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;1615.661,-250.3898;Float;False;True;-1;2;ASEMaterialInspector;0;0;Standard;SyntyStudios/Aurora;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;False;False;Off;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Transparent;0.5;True;False;0;False;Transparent;;Transparent;ForwardOnly;16;all;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;2;5;False;-1;10;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;-1;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;False;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;88;0;85;0
WireConnection;88;1;84;0
WireConnection;74;0;71;0
WireConnection;74;2;73;0
WireConnection;74;1;76;0
WireConnection;83;0;82;0
WireConnection;83;1;87;0
WireConnection;75;1;74;0
WireConnection;90;0;83;0
WireConnection;90;1;88;0
WireConnection;97;0;96;0
WireConnection;97;1;98;0
WireConnection;79;0;90;0
WireConnection;79;1;75;0
WireConnection;92;0;75;4
WireConnection;92;1;94;0
WireConnection;77;0;75;4
WireConnection;77;1;78;4
WireConnection;95;0;79;0
WireConnection;95;1;97;0
WireConnection;0;2;95;0
WireConnection;0;9;77;0
ASEEND*/
//CHKSM=F77BEAB53BA17B417D3874586DB0E8CAA10C0571