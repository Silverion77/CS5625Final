using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK;

using Chireiden.Meshes;

namespace Chireiden.Scenes
{
    public class ZombieProjectile : FieryProjectile
    {
        
        public ZombieProjectile(float s, Vector3 t, Vector3 v)
            : base(s, t, v)
        { }


        protected override void setUpFireLights()
        {
            light = new PointLight(new Vector3(0, 0, 0), 10, 20, new Vector3(0.2f, 0.48f, 1f));
            fire = new FireEmitter(new Vector3(0, 0, 0), 150f, 0.5f);
            this.addChild(light);
            this.addChild(fire);
        }

        public override void checkTargetHits(List<ZombieFairy> zombies)
        {
        }

        public override void checkTargetHit(UtsuhoReiuji okuu)
        {
            if (okuu.isInvulnerable()) return;
            Vector3 collisionPos = okuu.worldPosition;
            if (Math.Abs(collisionPos.X - worldPosition.X) > 3
                || Math.Abs(collisionPos.Y - worldPosition.Y) > 3) return;
            float distRequired = hitRadius + UtsuhoReiuji.hitCylinderRadius;

            float dist = (collisionPos.Xy - worldPosition.Xy).Length;
            if (dist <= distRequired && worldPosition.Z > 0 && worldPosition.Z < UtsuhoReiuji.hitCylinderHeight)
            {
                Console.WriteLine("Hit Okuu");
                collided = true;
            }
        }
    }
}
