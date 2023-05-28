uniform sampler2D texture;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a > 0.0) {
		gl_FragColor = vec4(pixel.r, pixel.g, 0.75, 0.25);
	} else {
		gl_FragColor = pixel;
	}
}