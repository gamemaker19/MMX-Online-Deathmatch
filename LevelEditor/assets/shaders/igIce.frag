uniform sampler2D texture;
uniform float igFreezeProgress;

void main() {
	vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
	if (pixel.a == 0.0) {
		gl_FragColor = pixel;
	} else {
		gl_FragColor = vec4(pixel.r + igFreezeProgress * (0.0 / 255.0), pixel.g + igFreezeProgress * (238.0 / 255.0), pixel.b + igFreezeProgress * (255.0 / 255.0), pixel.a);
	}
}