// basic fragment shader for Parallax Mapping
// Code adapted from http://sunandblackcat.com/tipFullView.php?l=eng&topicid=28

#version 330

#define MAX_LIGHTS 40

in vec3 geom_position;
in vec3 geom_worldPos;
in vec2 geom_texCoord;
in vec3 geom_normal;
in vec3 geom_tangent;
in vec3 geom_bitangent;

// Phong shading base colors
// TODO: Required later?
uniform vec3 mat_ambient;
uniform vec4 mat_diffuse;
uniform vec3 mat_specular;
uniform float mat_shininess;

// textures
uniform sampler2D diffuseTexture;
uniform sampler2D specularTexture;
uniform sampler2D heightTexture;
// TODO: What space is this in?
uniform sampler2D normalTexture;
// scale for size of Parallax Mapping effect
uniform float parallaxScale;

// Light uniforms
uniform mat4 inverseViewMatrix;
uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];
uniform float light_falloffDistance[MAX_LIGHTS];
uniform float light_energy[MAX_LIGHTS];
uniform vec3 light_color[MAX_LIGHTS];

// Camera for POM
uniform vec3 camera_position;

vec3 toLightsInTS[MAX_LIGHTS];

out vec4 out_frag_color;

vec4 normalMapLighting(in vec2 T, in vec3 V)
{
	vec3 N = normalize(texture(normalTexture, T).xyz * 2 - 1);

	vec3 D = texture(diffuseTexture, T).rgb;
	vec4 result;
	for (int i = 0; i < light_count; i++) {
		vec3 L = normalize(toLightsInTS[i]);
		// ambient lighting
		float iamb = 0.2;
		// diffuse lighting
		float idiff = clamp(dot(N, L), 0, 1);
		// specular lighting
		float ispec = 0;
		if(dot(N, L) > 0.2) {
			vec3 R = reflect(-L, N);
			ispec = pow(dot(R, V), 32) / 1.5;
		}
		result.rgb += D;// * (ambientLighting + (idiff + ispec));
		//  * pow(shadowMultiplier, 4)
	}
	result.a = 1;
	return result;
}

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
	
	float heightFromTexture = texture(heightTexture, currentTexCoord).r;
	// while the current texture height is above the surface
	// extend along V until the layer is below the heightMap
	while(heightFromTexture > curLayerHeight)
	{
		curLayerHeight += layerHeight; 
		// offset texture coordinate
		currentTexCoord -= deltaTex;
		// new depth from heightmap
		heightFromTexture = texture(heightTexture, currentTexCoord).r;
	}
	
	// Recalculate step above to use for interpolation
	vec2 prevTexCoord = currentTexCoord + deltaTex;
	
	float distanceToNextH = heightFromTexture - curLayerHeight;
	float distanceToPrevH = texture(heightTexture, prevTexCoord).r
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
	vec3 worldDirToCamera = normalize(camera_position - geom_worldPos);
	
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
	vec2 Tex = parallaxMapping(V, geom_texCoord, parallaxHeight);
	
	// TODO: Shadowing?
	
	// Lighting calculation ** ADD shadow here**
	out_frag_color = vec4(1, 0, 0, 1);
}