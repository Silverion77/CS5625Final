using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes;
using Chireiden.Materials;

namespace Chireiden.Scenes
{
    public class FieryProjectile : MobileObject
    {
        private TriMesh sphere;
        private float time = 0f;

        public const float hitRadius = 0.5f;

        protected bool collided = false;

        public PointLight light;
        protected ParticleEmitter fire;

        public FieryProjectile(float s, Vector3 t, Vector3 v)
            : base(s, t, Quaternion.Identity, v)
        {
            sphere = ((MeshGroup)MeshLibrary.Sphere).getMeshes()[0];
            sphere.setMaterial(new FireMaterial());
            setUpFireLights();
        }

        protected virtual void setUpFireLights()
        {
            light = new PointLight(new Vector3(0, 0, 0), 2, 10, new Vector3(1, 0.48f, 0.2f));
            fire = new FireEmitter(new Vector3(0, 0, 0), 150f);
            this.addChild(light);
            this.addChild(fire);
        }

        Vector3 explodePos;

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            base.update(e, parentToWorldMatrix);
            if (worldPosition.Z > 40 || worldPosition.Z < 0) collided = true;
        }

        public override void render(Camera camera)
        {
            if (!(camera is LightCamera))
            {
                ShaderProgram program = ShaderLibrary.FireShader;

                Matrix4 viewMatrix = camera.getViewMatrix();
                Matrix4 projectionMatrix = camera.getProjectionMatrix();
                Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);

                program.use();
                program.setUniformFloat1("un_Time", time);
                program.setUniformMatrix4("projectionMatrix", projectionMatrix);
                program.setUniformMatrix4("modelViewMatrix", modelView);
                sphere.renderMesh(camera, toWorldMatrix, program, 0);
                program.unuse();
                renderChildren(camera);
                time += 0.001f;
            }
        }

        protected override void correctPosition(Vector3 origPos, Matrix4 parentToWorldMatrix)
        {
            if (stage != null)
            {
                Vector3 worldSpaceCorrected = worldPosition;
                Vector3 correction;
                // First do a pass to make sure we don't push through a wall
                if (stage.worldLocInBounds(origPos) && !stage.worldLocInBounds(worldPosition))
                {
                    // If we're not in bounds, we need to do a projection
                    worldSpaceCorrected = stage.computeCollisionTime(origPos, worldPosition);
                    // Add the correction -- overcorrect slightly
                    correction = worldSpaceCorrected - worldPosition;
                    translation = translation + correction;
                    correction.Normalize();
                    translation = translation + correction;
                    collided = true;
                }
                // Don't do the wall repelling
            }
        }

        public Vector3 explosionPos()
        {
            return explodePos;
        }

        public virtual void checkTargetHits(List<ZombieFairy> zombies)
        {
            foreach (ZombieFairy fairy in zombies)
            {
                checkTargetHit(fairy);
                if (collided) return;
            }
        }

        void checkTargetHit(ZombieFairy fairy)
        {
            Vector3 collisionPos = fairy.worldPosition;
            if (Math.Abs(collisionPos.X - worldPosition.X) > 3
                || Math.Abs(collisionPos.Y - worldPosition.Y) > 3) return;
            float distRequired = hitRadius + ZombieFairy.hitCylinderRadius;

            float dist = (collisionPos.Xy - worldPosition.Xy).Length;
            if (dist <= distRequired && worldPosition.Z > 0 && worldPosition.Z < ZombieFairy.hitCylinderHeight)
            {
                Console.WriteLine("Hit fairy");
                collided = true;
            }
        }

        public bool hitSomething()
        {
            return collided;
        }


        public virtual void checkTargetHit(UtsuhoReiuji okuu)
        {

        }
    }
}
