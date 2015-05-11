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
    /// <summary>
    /// A ParticleEmmitter spawns particles within the ellipsoid defined by transforming 
    /// the unit sphere by the modelMatrix. 
    /// </summary>
    public abstract class ParticleEmitter: MobileObject
    {
        // The rate that particles are emitted in particles/frame
        protected float rate;

        // The (fractional) number of additional particles we weren't able to emit this frame
        protected float excessParticles = 0;

        protected float scaleFactor = 1;

        public virtual float ParticleZVelocity
        {
            get
            {
                return 2.0f;
            }
        }

        protected Random rand = new Random();

        public ParticleEmitter(Vector3 position, float particlesPerSecond, float scale)
            : base(position)
        {
            rate = particlesPerSecond;
            this.scaleFactor = scale;
        }

        public ParticleEmitter(Vector3 position, float particlesPerSecond) : base(position) 
        {
            rate = particlesPerSecond;
        }
        protected float randomAngle()
        {
            return (float)rand.NextDouble() * 2.0f * 3.1415926535f;
        }
        protected Vector3 randomVector()
        {
            //See: http://math.stackexchange.com/a/44691
            float theta = randomAngle();
            float z = (float)rand.NextDouble() * 2.0f - 1.0f;

            float r = (float)Math.Sqrt(1.0 - z*z);
            float x = (float)Math.Cos(theta) * r;
            float y = (float)Math.Sin(theta) * r;

            return new Vector3(x, y, z);
        }
        abstract protected ParticleSystem.Particle generateParticle();

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            base.update(e, parentToWorldMatrix);

            excessParticles += rate * (float)e.Time;
            while (excessParticles >= 1.0f)
            {
                excessParticles -= 1.0f;
                ParticleSystem.SpawnParticle(generateParticle());
            }
        }

        public override void render(Camera camera)
        {
            // Individual particles are batched for rendering and updating, so we don't
            // need to do anything here.
        }
    }
}