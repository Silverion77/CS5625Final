using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

namespace Chireiden.Scenes
{
    public class LightExplosion : PlaceableObject
    {
        public PointLight explosionLight;

        ExplosionEmitter emitter;

        double timeRemaining;
        double initialTime;

        public Vector3 Location;

        float intensity;

        public LightExplosion(Vector3 loc, Vector3 color, double duration, float particleRate, float intensity) : base(loc)
        {
            explosionLight = new PointLight(Vector3.Zero, 1, 1, color);
            this.addChild(explosionLight);
            timeRemaining = duration;
            initialTime = duration;
            emitter = new ExplosionEmitter(Vector3.Zero, particleRate, 2, duration);
            this.addChild(emitter);
            this.intensity = intensity;
            Location = loc;
        }

        public void addToWorld(World world)
        {
            world.addChild(this);
            world.registerPointLight(this.explosionLight);
        }

        /// <summary>
        /// This should tend to 0 as t approaches 0 from the positive direction
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        double intensityFunction(double t)
        {
            double factor = (initialTime - timeRemaining) / initialTime;
            return intensity * t * t;
        }

        /// <summary>
        /// See above
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        double falloffFunction(double t)
        {
            return 20;
        }

        public bool isOver() {
            return timeRemaining <= 0;
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            base.update(e, parentToWorldMatrix);
            explosionLight.Energy = (float)intensityFunction(timeRemaining);
            explosionLight.FalloffDistance = (float)falloffFunction(timeRemaining);
            timeRemaining -= e.Time;
        }

        public void removeFromWorld(World world)
        {
            world.unregisterPointLight(this.explosionLight);
            world.removeChild(this);
            Console.WriteLine("removed");
        }

        public override void render(Camera camera)
        {
            // render nothing
        }
    }
}
