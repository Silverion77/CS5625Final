/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 * 
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488), 
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */

#version 140

in vec2 geom_texCoord;
uniform samplerCube shadowMap;
uniform float minZ = 10;
uniform float maxZ = 30;
uniform int shadowMapWidth = 1024;
uniform int shadowMapHeight = 1024;
out vec4 FragColor;

void main() {
	vec4 value = texture(shadowMap, normalize(vec3(1, 0, gl_FragCoord.x / shadowMapWidth)));
	float theMinZ = minZ;
	float theMaxZ = maxZ;
	if (minZ > maxZ) {
		theMinZ = maxZ;
		theMaxZ = minZ;
	}
	if (value.w != 0) {
		float theZ = value.w;
		if (theZ < theMinZ) theZ = theMinZ;
		if (theZ > theMaxZ) theZ = theMaxZ;
		theZ = (theZ - theMinZ) / (theMaxZ - theMinZ);		
		FragColor = vec4(theZ, theZ, theZ, 1);		
	} else {
		FragColor = vec4(1);
	}
}