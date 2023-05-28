uniform sampler2D texture;
uniform float factor;
uniform float alpha; // Default uniform values not supported in GLSL 1.10

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		float r = mix(pixel.r, 0.0, factor);
		float g = mix(pixel.g, 0.0, factor);
		float b = mix(pixel.b, 0.0, factor);
		gl_FragColor = vec4(r, g, b, alpha);
	}
}