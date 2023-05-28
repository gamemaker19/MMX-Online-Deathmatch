uniform sampler2D texture;
uniform sampler2D chargeTexture;
uniform float chargeFactor;

void main() {
	float y = gl_TexCoord[0].y;
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);

	if (pixel.a == 0.0 || y < chargeFactor) {
		gl_FragColor = pixel;
	} else {
		gl_FragColor = texture2D(chargeTexture, gl_TexCoord[0].xy);
	}
}