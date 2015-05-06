// Simple vert shader for Parallax Mapping
// Code adapted from http://sunandblackcat.com/tipFullView.php?l=eng&topicid=28

#version 330

#define MAX_LIGHTS 40	

in vec3 vert_position;
in vec3 vert_normal;
in vec2 vert_texCoord;
in vec4 vert_tangent;

// uniforms
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 inverseViewMatrix;
uniform mat4 projectionMatrix;
uniform mat3 normalMatrix;
uniform vec3 camera_position;
uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];

// out
out vec2 geom_texCoord;
//out vec3 toLightsInTS[MAX_LIGHTS];
out vec3 toCameraInTS;

void main()
{
	vec4 worldPos = modelMatrix * vec4(vert_position, 1);
	vec3 N = normalize(normalMatrix * vert_normal);
	vec3 T = normalize(normalMatrix * vert_tangent.xyz);
	vec3 B = normalize(cross(N, T) * vert_tangent.w);
	
	// World space direction vectors
	vec3 worldDirToCamera = normalize(camera_position - worldPos.xyz);
	
	// Transform directions to Tangent Space (TS)
	//for (int i = 0; i < light_count; i++) {
	//	vec3 worldDirToLight = normalize((inverseViewMatrix * vec4(light_eyePosition[i], 1)).xyz - worldPos.xyz);
	//	toLightsInTS[i] = vec3(
	//		dot(worldDirToLight, T),
	//		dot(worldDirToLight, B),
	//		dot(worldDirToLight, N)
	//	);
	//}
	
	toCameraInTS = vec3(
		dot(worldDirToCamera, T),
		dot(worldDirToCamera, B),
		dot(worldDirToCamera, N)
	);
	
	geom_texCoord = vert_texCoord;
	
	gl_Position = projectionMatrix * viewMatrix * worldPos;
}