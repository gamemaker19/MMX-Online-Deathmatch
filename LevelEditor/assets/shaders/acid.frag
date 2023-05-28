uniform sampler2D texture;
uniform float acidFactor;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		gl_FragColor = vec4(pixel.r - acidFactor * (150.0 / 255.0), pixel.g + acidFactor * (5.0 / 255.0), pixel.b - acidFactor * (150.0 / 255.0), pixel.a);
	}
}