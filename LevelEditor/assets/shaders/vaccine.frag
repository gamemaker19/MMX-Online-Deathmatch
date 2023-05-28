uniform sampler2D texture;
uniform float vaccineFactor;

void main()
{
   vec4 pixel = texture2D(texture, gl_TexCoord[0].xy);
   if(pixel.a == 0.0)
   {
      gl_FragColor = pixel;
   }
   else
   {
      gl_FragColor = vec4(pixel.r + vaccineFactor*(38.0/255.0), pixel.g + vaccineFactor*(255.0/255.0), pixel.b + vaccineFactor*(107.0/255.0), pixel.a);
   }
}