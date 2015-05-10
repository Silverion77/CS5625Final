using System;
using System.Collections.Generic;

using OpenTK;

namespace Chireiden.Scenes
{
    public class Scatterer : ParticleEmitter
    {

        public override float ParticleZVelocity
        {
            get
            {
                return 2 * (float)Utils.randomDouble() - 0.5f;
            }
        }
        
        public Scatterer(Vector3 position, float particlesPerSecond, float scale)
            : base(position, particlesPerSecond, scale)
        {

        }

        public Scatterer(Vector3 position, float pps) : base(position, pps) { }
    }
}
