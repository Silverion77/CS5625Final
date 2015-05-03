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
    class ParticleEmitter: MobileObject
    {
        // The rate that particles are emitted in particles/frame
        float rate;

        // The (fractional) number of additional particles we weren't able to emit this frame
        float excessParticles = 0;

        Random rand = new Random();

        public ParticleEmitter(Vector3 position, float particlesPerSecond) : base(position) 
        {
            rate = particlesPerSecond;
        }
        private float randomAngle()
        {
            return (float)rand.NextDouble() * 2.0f * 3.1415926535f;
        }
        private Vector3 randomVector()
        {
            //See: http://math.stackexchange.com/a/44691
            float theta = randomAngle();
            float z = (float)rand.NextDouble() * 2.0f - 1.0f;

            float r = (float)Math.Sqrt(1.0 - z*z);
            float x = (float)Math.Cos(theta) * r;
            float y = (float)Math.Sin(theta) * r;

            return new Vector3(x, y, z);
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            base.update(e, parentToWorldMatrix);

            excessParticles += rate * (float)e.Time;
            while (excessParticles >= 1.0f)
            {
                excessParticles -= 1.0f;
                ParticleSystem.Particle p = new ParticleSystem.Particle();

                var r = randomVector();
                var pos = Vector4.Transform(new Vector4(r.X, r.Y, r.Z, 1), toWorldMatrix);

                p.position = new Vector3(pos.X / pos.W, pos.Y / pos.W, pos.Z / pos.W);
                p.velocity = new Vector3((float)rand.NextDouble() - 0.5f, (float)rand.NextDouble() - 0.5f, 2.0f);
                p.rotation = randomAngle();
                p.angularVelocity = 0;
                
                p.gravity = 0.0f;
                p.radius = 1.0f;
                p.life = 3.0f;
                p.invTotalLife = 1.0f / p.life;
                p.texture = 0;
                ParticleSystem.SpawnParticle(p);
            }
        }

        public override void render(Camera camera)
        {
            // Individual particles are batched for rendering and updating, so we don't
            // need to do anything here.
        }
    }
}