// Simple vert shader for Parallax Mapping
// Code adapted from http://sunandblackcat.com/tipFullView.php?l=eng&topicid=28

#version 330

in vec3 vert_position;
in vec3 vert_normal;
in vec2 vert_texCoord;
in vec4 vert_tangent;

// uniforms
uniform mat4 modelMatrix;
uniform mat4 viewMatrix;
uniform mat4 projectionMatrix;
// TODO: Normal matrix to preserve orthogonality?
uniform vec3 light_position;
uniform vec3 camera_position;

// out
out vec2 geom_texCoord;
out vec3 toLightInTS;
out vec3 toCameraInTS;

void main()
{
	vec4 worldPos = modelMatrix * vec4(vert_position, 1);
	vec3 N = normalize((modelMatrix * vec4(vert_normal, 0)).xyz);
	vec3 T = normalize((modelMatrix * vec4(vert_tangent.xyz, 0)).xyz);
	vec3 B = normalize(cross(N, T) * vert_tangent.w);
	
	// World space direction vectors
	vec3 worldDirToLight = normalize(light_position - worldPos.xyz);
	vec3 worldDirToCamera = normalize(camera_position - worldPos.xyz);
	
	// Transform directions to Tangent Space (TS)
	toLightInTS = vec3(
		dot(worldDirToLight, T),
		dot(worldDirToLight, B),
		dot(worldDirToLight, N)
	);
	
	toCameraInTS = vec3(
		dot(worldDirToCamera, T),
		dot(worldDirToCamera, B),
		dot(worldDirToCamera, N)
	);
	
	geom_texCoord = vert_texCoord;
	
	gl_Position = projectionMatrix * viewMatrix * worldPos;
}