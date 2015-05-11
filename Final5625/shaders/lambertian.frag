/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 * 
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488), 
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */

#version 140

#define MAX_LIGHTS 40

const float PI = 3.14159265358979323846264;

in vec3 geom_position;
in vec3 geom_normal;
in vec2 geom_texCoord;

uniform mat4 inverseModelView;

uniform vec4 mat_diffuseColor;
uniform bool mat_hasTexture;
uniform sampler2D texture;
uniform vec2 texScale;

uniform int light_count;
uniform vec3 light_eyePosition[MAX_LIGHTS];
uniform float light_falloffDistance[MAX_LIGHTS];
uniform float light_energy[MAX_LIGHTS];
uniform vec3 light_color[MAX_LIGHTS];

// shadow map uniforms
uniform sampler2DRect shadowMap[MAX_LIGHTS];
uniform int shadowMapWidth = 1024;
uniform int shadowMapHeight = 1024;
uniform int pointLight_pcfKernelSampleCount = 4;
uniform float pointLight_pcfWindowWidth = 5;
uniform float pointLight_shadowMapBiasScale = .05;
uniform float pointLight_shadowMapConstantBias = .02;
uniform mat4 pointLight_viewMatrix[MAX_LIGHTS];
uniform mat4 pointLight_projectionMatrix;

out vec4 out_frag_color;


// poisson samples
uniform float poissonSamples[80] = float[80](0.0340212, 0.674301, 0.0858099, 0.496943, 0.0694185, 0.860058, 0.0406619, 0.218842,
									 		 0.0735586, 0.0448695, 0.15123, 0.339518, 0.271311, 0.469369, 0.200576, 0.774924,
									 		 0.204111, 0.154617, 0.20539, 0.948375, 0.185354, 0.612016, 0.321298, 0.0390153,
									 		 0.304494, 0.28675, 0.326761, 0.667022, 0.426756, 0.385165, 0.43075, 0.539734, 
											 0.35753, 0.855105, 0.472576, 0.713726, 0.599869, 0.436843, 0.531687, 0.274545, 
											 0.413117, 0.167727, 0.53426, 0.849135, 0.472694, 0.998865, 0.641721, 0.957124,
											 0.579323, 0.597005, 0.589336, 0.120575, 0.745134, 0.148836, 0.688499, 0.293683,
											 0.785452, 0.00750335, 0.65954, 0.738719, 0.916359, 0.112317, 0.829323, 0.681274,
											 0.773597, 0.848216, 0.731165, 0.561037, 0.795832, 0.41844, 0.931794, 0.949323,
											 0.863814, 0.265922, 0.97478, 0.38034, 0.928406, 0.792087, 0.917633, 0.543024);

float rand(vec2 coord){
	// courtesy of http://stackoverflow.com/questions/12964279/whats-the-origin-of-this-glsl-rand-one-liner
	return fract(sin(dot(coord.xy ,vec2(12.9898,78.233))) * 43758.5453);
}

// returns whether the given sample is in shadow or not and the w-component of the shadow map texel
vec2 getShadowSample(int i, vec3 position, vec2 offset) {
	// TODO: cubemap instead of single texture???

	vec4 p = inverseModelView * vec4(position, 1);
	p = pointLight_projectionMatrix * pointLight_viewMatrix[i] * p;
	p /= p.w;
	p.x = (p.x * 0.5 + 0.5) * shadowMapWidth;
	p.y = (p.y * 0.5 + 0.5) * shadowMapHeight;
	
	vec4 q = texture2DRect(shadowMap[i], p.xy + offset);
	float c = q.z + (pointLight_shadowMapBiasScale * max(abs(q.x), abs(q.y))) + pointLight_shadowMapConstantBias;
	
	if (p.z > c) return vec2(0.0, q.w);
	else return vec2(1.0, q.w);
}

float getShadowFactor(int i, vec3 position) {	
	// PCF Implementation

	// form a square window of size spotLight_pcfWindowWidth around position

	// get a "pseudo-random" angle between 0 and 2 pi radians
	float angle = rand(geom_texCoord) * 2.0 * PI;
	// build a rotation matrix to rotate a point by that angle (col-major)
	mat2 rotation = mat2(cos(angle), sin(angle), -sin(angle), cos(angle));
		
	int numSamples = pointLight_pcfKernelSampleCount;
	// clamp number of samples to poisson disc sample size
	if (numSamples > poissonSamples.length()/2) {
		numSamples = poissonSamples.length()/2;
	}
	// apply the rotation to each sample as we go
	// translate by (.5,.5) before rotation then translate back so that we
	// are rotating about the center
	// then scale appropriately
	float light_contribution = 0;
	for (int i = 0; i < numSamples; i++) {
		vec2 rotatedSample = (rotation * (vec2(poissonSamples[2*i], poissonSamples[2*i+1]) - .5)) + .5;
		vec2 offset = (rotatedSample - vec2(.5,.5)) * pointLight_pcfWindowWidth;
			
		light_contribution += getShadowSample(i, position, offset).x;
	}
	// average contributions
	return float(light_contribution) / numSamples; 
}

void main()
{
	vec4 diffuse = mat_diffuseColor;
	vec4 tex = vec4(0, 0, 0, 1);
	if (mat_hasTexture) {
		tex = texture2D(texture, geom_texCoord / texScale);
		diffuse = diffuse * tex;
	}

	vec4 result = vec4(0,0,0,diffuse.a);
	vec3 n = normalize(geom_normal);

	float factor;

	for (int i=0; i<light_count; i++) {

		vec3 l = light_eyePosition[i] - geom_position;
		float r_squared = dot(l, l);
		l = normalize(l);

		// The attenuation model used here is from http://wiki.blender.org/index.php/Doc:2.6/Manual/Lighting/Lights/Light_Attenuation
		float d2 = light_falloffDistance[i] * light_falloffDistance[i];
		float intensity = light_energy[i] * (d2 / (d2 + r_squared));

		float dotProd = max(dot(n,l), 0);
		
		factor = 1.0; //getShadowFactor(i, geom_position);
		result.xyz += factor * diffuse.xyz * dotProd * light_color[i] * intensity;
	}

	vec3 world_position = (inverseModelView * vec4(geom_position, 1)).xyz;

	// Make it fade to black as we go higher
	float scalingFactor = min(1, pow(1.5, -(world_position.z - 10)));
	result.xyz *= scalingFactor;

	out_frag_color = factor * result;
}
