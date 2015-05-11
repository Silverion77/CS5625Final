/*
 * Written for Cornell CS 5625 (Interactive Computer Graphics).
 * Copyright (c) 2015, Department of Computer Science, Cornell University.
 * 
 * This code repository has been authored collectively by:
 * Ivaylo Boyadzhiev (iib2), John DeCorato (jd537), Asher Dunn (ad488), 
 * Pramook Khungurn (pk395), and Sean Ryan (ser99)
 */

#version 120

in vec3 geom_position;
uniform mat4 projectionMatrix;
uniform int shadowMapWidth;
uniform int shadowMapHeight;

void main() {
	float w = -geom_position.z;
	vec4 temp = projectionMatrix * vec4(geom_position, 1);
	vec3 p1 = vec3(temp.x / w, temp.y / w, temp.z / w);
	
	float dx = dFdx(p1.z) * shadowMapWidth;
	float dy = dFdy(p1.z) * shadowMapHeight;
	
	gl_FragData[0] = vec4(dx, dy, p1.z, w);
}