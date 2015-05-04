using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes.Animations;

namespace Chireiden.Meshes
{
    /// <summary>
    /// A set of meshes that are a part of the same model, and therefore
    /// always share the same world-space transformations, and are rendered
    /// as one unit.
    /// </summary>
    public class MeshGroup : MeshContainer
    {
        List<TriMesh> meshes;

        public MeshGroup()
        {
            meshes = new List<TriMesh>();
        }

        public MeshGroup(List<TriMesh> ms)
        {
            meshes = new List<TriMesh>();
            this.meshes.AddRange(ms);
        }

        public void renderMeshes(Camera camera, Matrix4 toWorldMatrix)
        {
            ShaderProgram program = Shaders.BlenderShader;

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            // Compute transformation matrices
            Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);

            program.use();

            // set shader uniforms, incl. modelview and projection matrices
            program.setUniformMatrix4("projectionMatrix", projectionMatrix);
            program.setUniformMatrix4("modelViewMatrix", modelView);
            program.setUniformMatrix4("viewMatrix", viewMatrix);

            camera.setPointLightUniforms(program);

            foreach (TriMesh m in meshes) {
                m.renderMesh(camera, toWorldMatrix, Shaders.BlenderShader, 0);
            }

            program.unuse();
        }

        public void renderMeshes(Camera c, Matrix4 m, AnimationClip clip, double time)
        {
            renderMeshes(c, m);
        }

        public AnimationClip fetchAnimation(string s)
        {
            // Nothing to return.
            return null;
        }

        public void addMesh(TriMesh m)
        {
            meshes.Add(m);
        }

        public void removeMesh(TriMesh m)
        {
            meshes.Remove(m);
        }
    }
}
