uniform sampler2D texture;
uniform float alpha;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);

	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		gl_FragColor = vec4(pixel.r, pixel.g, pixel.b, alpha);
	}

}