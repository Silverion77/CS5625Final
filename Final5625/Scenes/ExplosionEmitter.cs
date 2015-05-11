using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace Chireiden.Scenes
{
    public class ExplosionEmitter : ParticleEmitter
    {
        double duration;
        double elapsed;

        public ExplosionEmitter(Vector3 loc, float rate, float scale, double duration)
            : base(loc, rate, scale)
        {
            this.duration = duration;
            elapsed = 0;
        }

        double velocityFunction(double t)
        {
            return 20 * t * t;
        }
        override protected ParticleSystem.Particle generateParticle()
        {
            double factor = 1 - elapsed / duration;

            excessParticles -= 1.0f;
            ParticleSystem.Particle p = new ParticleSystem.Particle();

            var r = randomVector() * scaleFactor;
            var pos = Vector4.Transform(new Vector4(r.X, r.Y, r.Z, 1), toWorldMatrix);

            p.position = new Vector3(pos.X / pos.W, pos.Y / pos.W, pos.Z / pos.W);
            p.velocity = randomVector();
            p.velocity = p.velocity * (float)velocityFunction(factor) * (float)Math.Pow(2, (rand.NextDouble() - 1) * 3);

            p.rotation = randomAngle();
            p.angularVelocity = 0;

            p.gravity = 0.0f;
            p.radius = 1.0f * (Math.Min(1, scaleFactor * 5));
            p.life = 3.0f;
            p.invTotalLife = 1.0f / p.life;
            p.texture = 0;
            return p;
        }
        public override void emitParticles(double delta)
        {
            elapsed += delta;
            double factor = 1 - elapsed / duration;
            excessParticles += rate * (float)(factor * delta);
            while (excessParticles >= 1.0f)
            {
                excessParticles -= 1.0f;
                ParticleSystem.SpawnParticle(generateParticle());
            }
        }
    
    }
}
