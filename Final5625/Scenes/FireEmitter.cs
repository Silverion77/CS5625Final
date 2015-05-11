using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Chireiden.Scenes
{
    public class FireEmitter : ParticleEmitter
    {
        public FireEmitter(Vector3 position, float particlesPerSecond, float scale) :
            base(position, particlesPerSecond, scale) { }

        public FireEmitter(Vector3 position, float particlesPerSecond) :
            base(position, particlesPerSecond) { }

        override protected ParticleSystem.Particle generateParticle()
        {
            ParticleSystem.Particle p = new ParticleSystem.Particle();

            var r = randomVector() * scaleFactor;
            var pos = Vector4.Transform(new Vector4(r.X, r.Y, r.Z, 1), toWorldMatrix);

            p.position = new Vector3(pos.X / pos.W, pos.Y / pos.W, pos.Z / pos.W);
            p.velocity = new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, ParticleZVelocity);
            p.rotation = randomAngle();
            p.angularVelocity = 0;

            p.gravity = 0.0f;
            p.radius = 1.0f * (Math.Min(1, scaleFactor * 5));
            p.life = 3.0f;
            p.invTotalLife = 1.0f / p.life;
            p.texture = 0;
            p.colorScale = 1;
            p.alphaScale = 1;

            return p;
        }
    }
}
