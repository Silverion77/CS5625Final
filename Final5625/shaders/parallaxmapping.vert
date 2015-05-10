// Simple vert shader for Parallax Mapping

#version 330

#define MAX_LIGHTS 40
#extension GL_ARB_explicit_attrib_location : enable

layout(location = 0) in vec3 vert_position;
layout(location = 1) in vec3 vert_normal;
layout(location = 2) in vec2 vert_texCoord;
layout(location = 3) in vec4 vert_tangent;

// uniforms
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 modelViewMatrix;
uniform mat4 projectionMatrix;
uniform mat3 normalMatrix;

// out
out vec3 geom_position;
out vec3 geom_worldPos;
out vec2 geom_texCoord;
out vec3 geom_normal;
out vec3 geom_tangent;
out vec3 geom_bitangent;

void main()
{
	vec4 worldPos = modelMatrix * vec4(vert_position, 1);
	vec3 N = normalize(vert_normal);
	vec3 T = normalize(vert_tangent.xyz);
	vec3 B = normalize(cross(N, T) * vert_tangent.w);
	geom_normal = normalize(modelMatrix * vec4(N,0)).xyz;
	geom_tangent = normalize(modelMatrix * vec4(T,0)).xyz;
	geom_bitangent = normalize(modelMatrix * vec4(B,0)).xyz;
	
	geom_worldPos = worldPos.xyz;
	geom_texCoord = vert_texCoord;
	geom_position = (viewMatrix * worldPos).xyz;
	gl_Position = projectionMatrix * viewMatrix * worldPos;
}