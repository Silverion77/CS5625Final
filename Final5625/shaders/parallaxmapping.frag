// basic fragment shader for Parallax Mapping

#version 330

#define MAX_LIGHTS 40

in vec3 geom_position;
in vec3 geom_worldPos;
in vec2 geom_texCoord;
in vec3 geom_normal;
in vec3 geom_tangent;
in vec3 geom_bitangent;

// textures
uniform sampler2D diffuseTexture;
uniform sampler2D specularTexture;
uniform sampler2D heightTexture;
uniform sampler2D normalTexture;
uniform float mat_shininess;
// scale for size of Parallax Mapping effect
uniform float parallaxScale;
uniform vec2 scale_factor;

// Light uniforms
uniform mat4 viewMatrix;
uniform mat4 inverseViewMatrix;
uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];
uniform float light_falloffDistance[MAX_LIGHTS];
uniform float light_energy[MAX_LIGHTS];
uniform vec3 light_color[MAX_LIGHTS];

// Camera for POM
uniform vec3 camera_position;

vec3 toLightsInTS[MAX_LIGHTS];

layout (location = 0) out vec4 out_frag_color; 
layout (location = 1) out vec4 out_frag_normal; 
layout (location = 2) out vec4 out_frag_position; 

vec4 normalMapLighting(in vec2 T, in vec3 V, in vec3 n)
{
	vec3 mat_diffuse = texture(diffuseTexture, T / scale_factor).rgb;
	vec3 mat_specular = texture(specularTexture, T / scale_factor).rgb;

	vec3 v = -normalize(geom_position);

	vec4 result;
	for (int i = 0; i < light_count; i++) {
		vec3 l = light_eyePosition[i] - geom_position;
		vec3 L = toLightsInTS[i];
		float r_squared = dot(l, l);
		l = normalize(l);

		// The attenuation model used here is from http://wiki.blender.org/index.php/Doc:2.6/Manual/Lighting/Lights/Light_Attenuation
		float d2 = light_falloffDistance[i] * light_falloffDistance[i];
		float intensity = light_energy[i] * (d2 / (d2 + r_squared));
		// Compute diffuse
		float diffuse_dot = max(dot(n, l), 0);

		// give some ambient lighting
		//result.xyz += 0.2 * mat_diffuse;
		// the rest depends on the light angle (diffuse_dot)
		result.xyz += mat_diffuse * diffuse_dot * light_color[i] * intensity;

		// specular lighting
		if(dot(n, l) > 0.2) {
			vec3 h = normalize(v + l);
			float specular_dot = pow(max(0, dot(n, h)), mat_shininess);
			result.xyz += mat_specular * specular_dot * light_color[i] * intensity;
		}
	}
	// Make it fade to black as we go higher
	float scalingFactor = min(1, pow(1.5, -(geom_worldPos.z - 10)));
	result.xyz *= scalingFactor;
	result.a = 1;
	return result;
}

// Code adapted from http://sunandblackcat.com/tipFullView.php?l=eng&topicid=28
vec2 parallaxMapping(in vec3 V, in vec2 T, out float parallaxHeight)
{
	// determine optimal number of layers
	const float minLayers = 10;
	const float maxLayers = 15;
	float numLayers = mix(maxLayers, minLayers, abs(dot(vec3(0, 0, 1), V)));
	
	// height of each stepped layer
	float layerHeight = 1.0 / numLayers;
	// current depth of the layer
	float curLayerHeight = 0;
	// change in texture coordinate for each layer.
	// V is in Tangent Space, therefore V.z is in normal and V.xy is along the
	// texture gradients. Division gives change in texCoord for a unit of height
	// per layer.
	vec2 deltaTex = parallaxScale * V.xy / V.z / numLayers;
	
	vec2 currentTexCoord = T;
	
	float heightFromTexture = 1.0 - texture2D(heightTexture, currentTexCoord / scale_factor).r;
	// while the current texture height is above the surface
	// extend along V until the layer is below the heightMap
	while(heightFromTexture > curLayerHeight)
	{
		curLayerHeight += layerHeight; 
		// offset texture coordinate
		currentTexCoord -= deltaTex;
		// new depth from heightmap
		heightFromTexture = 1.0 - texture2D(heightTexture, currentTexCoord / scale_factor).r;
	}
	
	// Recalculate step above to use for interpolation
	vec2 prevTexCoord = currentTexCoord + deltaTex;
	
	float distanceToNextH = heightFromTexture - curLayerHeight;
	float distanceToPrevH = 1.0 - texture2D(heightTexture, prevTexCoord / scale_factor).r
								- curLayerHeight + layerHeight;
	
	// Linear interpolation
	float weight = distanceToNextH / (distanceToNextH - distanceToPrevH);
	vec2 texCoord = prevTexCoord * weight + (1.0 - weight) * currentTexCoord;
	parallaxHeight = curLayerHeight + distanceToPrevH * weight + (1.0 - weight) * distanceToNextH;
	
	return texCoord;
}

void main()
{
	// Reconstruct TS post interpolation
	vec3 N = normalize(geom_normal);
	vec3 T_Parallel = dot(geom_tangent, N) * N;
	// Orthogonalize tangent
	vec3 T = normalize(geom_tangent - T_Parallel);
	vec3 B_crossed = cross(N, T);
	vec3 B;
	if (dot(B_crossed, geom_bitangent) > 0) {
		B = normalize(B_crossed);
	} else {
		B = -normalize(B_crossed);
	}
	
	// World space direction vectors
	vec3 worldDirToCamera = normalize(geom_worldPos - camera_position);
	
	// Transform directions to Tangent Space (TS)
	for (int i = 0; i < light_count; i++) {
		vec3 worldDirToLight = normalize((inverseViewMatrix * vec4(light_eyePosition[i], 1)).xyz - geom_worldPos.xyz);
		toLightsInTS[i] = vec3(
			dot(worldDirToLight, T),
			dot(worldDirToLight, B),
			dot(worldDirToLight, N)
		);
	}
	
	vec3 toCameraInTS = vec3(
		dot(worldDirToCamera, T),
		dot(worldDirToCamera, B),
		dot(worldDirToCamera, N)
	);
	
	vec3 V = normalize(toCameraInTS);
	
	// get texture coordinates from the Parallax Mapping
	float parallaxHeight;
	vec2 tex = parallaxMapping(V, geom_texCoord, parallaxHeight);
	
	// Normal in TS??
	vec3 Norm = texture(normalTexture, tex / scale_factor).xyz;
	Norm = T * Norm.x + B * Norm.y + N * Norm.z;
	Norm = normalize((viewMatrix * vec4(N, 0)).xyz);
	out_frag_color = normalMapLighting(tex, V, Norm);
	out_frag_normal = vec4(Norm, 1.0);
	out_frag_position = vec4(geom_position, 1.0);
}