using System;
using System.Diagnostics;
using System.Text;
using System.IO;

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

        public FieryProjectile(float s, Vector3 t, Vector3 v)
            : base(s, t, Quaternion.Identity, v)
        {
            sphere = ((MeshGroup)MeshLibrary.Sphere).getMeshes()[0];
            sphere.setMaterial(new FireMaterial());
        }

        public override void render(Camera camera)
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
}
