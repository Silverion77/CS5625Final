import Image
import numpy as np

w,h = 256,1
data = np.zeros( (h,w,4), dtype=np.uint8)
for x in range(w):
	r, g, b, a = 1.0, 0.8, 0.0, 1.0
	
	#Fade in
	s = min(x / w * 10.0, 1);
	r, g, b, a = r*s, g*s, b*s, a*s

	#Fade out
	s = min((w-x) / w * 5.0, 1);
	r, g, b, a = r*s, g*s, b*s, a*s
	
	#Write back
	data[0, x] = [r*255, g*255, b*255, a*255]
	
img = Image.fromarray(data, 'RGBA')
img.save('flash.png')
