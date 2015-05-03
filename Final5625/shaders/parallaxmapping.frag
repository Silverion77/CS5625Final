// basic fragment shader for Parallax Mapping
// Code adapted from http://sunandblackcat.com/tipFullView.php?l=eng&topicid=28

#version 330

in vec2 geom_texCoord;
in vec2 toLightInTS;
in vec2 toCameraInTS;

// textures
uniform sampler2D mat_texture;
uniform sampler2D heightTexture;
// TODO: What space is this in?
uniform sampler2D normalTexture;

out vec4 out_frag_color;

// scale for size of Parallax Mapping effect
uniform float parallaxScale;

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
	vec3 V = normalize(toCameraInTS);
	vec3 L = normalize(toLightInTS);
	
	// get texture coordinates from the Parallax Mapping
	float parallaxHeight;
	vec2 T = parallaxMapping(V, geom_texCoord, parallaxHeight);
	
	// TODO: Shadowing?
	
	// Lighting calculation ** ADD shadow here**
	out_frag_color = normalMapLighting(T, L, V);
}