uniform sampler2D texture;
uniform float oilFactor;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		gl_FragColor = vec4(pixel.r + oilFactor * (5.0 / 255.0), pixel.g - oilFactor * (100.0 / 255.0), pixel.b - oilFactor * (150.0 / 255.0), pixel.a);
	}
}