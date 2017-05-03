#version 140

uniform vec3 Scroll;
uniform vec3 r1, r2;

in vec4 aVertexPosition;
in vec4 aVertexTexCoord;
out vec4 vColor;

void main()
{
	gl_Position = vec4((aVertexPosition.xyz - Scroll.xyz) * r1 + r2, 1);  
	vColor = aVertexTexCoord;
} 
