import Image
import numpy as np

w,h = 256,1
data = np.zeros( (h,w,4), dtype=np.uint8)
for x in range(w):
	r, g, b, a = 0, 0, 0, 0
	if x < w * 0.2:
		r, g, b = 1, 0.6, 0.1
	elif x < w * 0.4:
		t = (x - w * 0.2) / (w * 0.2)
		r, g, b, a = 1.0 - 0.9*t, 0.6 - 0.5 * t, 0.1, t * 0.2
	else:
		a = 0.2
		r, g, b = 0.1, 0.1, 0.1
		
	#Fade in
	s = min(x / w * 10.0, 1);
	r, g, b = r*s, g*s, b*s

	#Fade out
	s = min((w-x) / w * 5.0, 1);
	r, g, b, a = r*s, g*s, b*s, a*s
	
	#Write back
	data[0, x] = [r*255, g*255, b*255, a*255]
	
img = Image.fromarray(data, 'RGBA')
img.save('reaction.png')