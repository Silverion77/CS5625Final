/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 *
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488),
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */
 
#version 140

const float PI = 3.14159265358979323846264;
 
in vec2 geom_texCoord;

uniform sampler2D positionBuffer;
uniform sampler2D normalBuffer;
uniform int gbuf_width;
uniform int gbuf_height;
uniform float ssao_radius;
uniform float ssao_depthBias;
uniform int ssao_sampleCount;
uniform mat4 projectionMatrix;

out vec4 FragColor;

// poisson disc samples
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


// Use hammersley point set to generate uniform sample points
// Adapted from http://holger.dammertz.org/stuff/notes_HammersleyOnHemisphere.html
float radicalInverse_VdC(uint bits) {
     bits = (bits << 16u) | (bits >> 16u);
     bits = ((bits & 0x55555555u) << 1u) | ((bits & 0xAAAAAAAAu) >> 1u);
     bits = ((bits & 0x33333333u) << 2u) | ((bits & 0xCCCCCCCCu) >> 2u);
     bits = ((bits & 0x0F0F0F0Fu) << 4u) | ((bits & 0xF0F0F0F0u) >> 4u);
     bits = ((bits & 0x00FF00FFu) << 8u) | ((bits & 0xFF00FF00u) >> 8u);
     return float(bits) * 2.3283064365386963e-10; // / 0x100000000
 }
 
vec2 hammersley2d(uint i, uint N) {
     return vec2(float(i)/float(N), radicalInverse_VdC(i));
}

vec3 uniform_sample(float x, float y) {
     float phi = 2.0 * PI * y;
     float costheta = 1.0 - x;
     float sintheta = sqrt(1.0 - pow(costheta,2));
     return vec3(cos(phi) * sintheta, sin(phi) * sintheta, costheta);
}
///////////////////////////////////////////////////////////

float rand(vec2 coord){
	// courtesy of http://stackoverflow.com/questions/12964279/whats-the-origin-of-this-glsl-rand-one-liner
	return fract(sin(dot(coord.xy ,vec2(12.9898,78.233))) * 43758.5453);
}
 
void main() {
	    // Implement SSAO.
        vec4 p = texelFetch(positionBuffer, ivec2(gl_FragCoord.xy), 0);
        vec3 position = p.xyz;
        vec3 geom_normal = normalize(texelFetch(normalBuffer, ivec2(gl_FragCoord.xy), 0).xyz);
	   
	    int numSamples = ssao_sampleCount;
	    // clamp number of samples to poisson disc sample size
	    if (numSamples > poissonSamples.length()/2) {
	            numSamples = poissonSamples.length()/2;
	    }
	   
	    vec3 rVec = 2*(vec3(rand(geom_texCoord * length(geom_normal)), rand(geom_texCoord * length(geom_normal.xy)), 0.0) * 2 - 1);
	    vec3 tangent = normalize(rVec - geom_normal * dot(rVec, geom_normal));
	    vec3 bitangent = cross(geom_normal, tangent);
	    mat3 tbn = mat3(tangent, bitangent, geom_normal);
	   
	    float contribution = 0;
	   	float tot = 0;
	    for (int i = 0; i < numSamples; i++) {  
		    vec2 hsample = hammersley2d(uint(i), uint(numSamples));
		    vec3 samplePos = poissonSamples[2*i]* uniform_sample(hsample.x, hsample.y);
		    
		    vec3 s = tbn * samplePos;
		    s = s * ssao_radius + position;
		    vec3 omega = normalize(s - position);
		   
		    //project
		    vec4 offset = vec4(s, 1.0);
		    offset = projectionMatrix * offset;
		    offset.xy /= offset.w;
		    offset.x = (offset.x * 0.5 + 0.5) * gbuf_width;
		    offset.y = (offset.y * 0.5 + 0.5) * gbuf_height;
		   
		    //get depth
            float sampleDepth = texelFetch(positionBuffer, ivec2(offset.xy), 0).z;		   

		    if (abs(position.z) <= abs(sampleDepth) + ssao_depthBias || (abs(position.z) > abs(sampleDepth) + ssao_depthBias + 5*ssao_radius)) {
		    	contribution += max(dot(geom_normal, omega),0);
		    }
		    else {
		    	contribution += max(dot(geom_normal, omega),0) * (1 - smoothstep(ssao_depthBias, 3 * ssao_depthBias, distance(position.z, sampleDepth)));
		    }
		    
	    	tot += max(dot(geom_normal, omega),0);
	    }
	    if (tot > 0) {
		    contribution = contribution / tot;
	    }
		      
    	FragColor = vec4(vec3(contribution), 1.0);
}