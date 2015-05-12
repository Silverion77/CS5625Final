using System;
using System.Diagnostics;
using System.Text;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes;
using Chireiden.Materials;
using Chireiden.Scenes.Stages;

namespace Chireiden.Scenes
{
    /// <summary>
    /// An object in the scene tree to be used as a surface for the game. E.g. walls, floor, etc.
    /// </summary>
    class SurfaceTile : StageTilesNode
    {
        public SurfaceTile(Vector3[] verts, int[] faces, Vector3[] normals, Vector2[] texCoords, Vector4[] tangents, Material mat)
            : base(verts, faces, normals, texCoords, tangents, mat) { }

        public override void render(Camera camera)
        {
            if (!(camera is LightCamera))
            {
                ShaderProgram program = ShaderLibrary.POMShader;

                Matrix4 viewMatrix = camera.getViewMatrix();
                Matrix4 projectionMatrix = camera.getProjectionMatrix();
                Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);
                Matrix3 normalMatrix = Utils.normalMatrix(modelView);

                program.use();
                // set shader uniforms
                program.setUniformMatrix4("modelMatrix", toWorldMatrix);
                program.setUniformMatrix4("viewMatrix", viewMatrix);
                program.setUniformMatrix4("modelViewMatrix", modelView);
                program.setUniformMatrix4("inverseViewMatrix", viewMatrix.Inverted());
                program.setUniformMatrix4("projectionMatrix", projectionMatrix);
                program.setUniformMatrix3("normalMatrix", normalMatrix);
                program.setUniformFloat3("camera_position", camera.getWorldSpacePos());
                camera.setPointLightUniforms(program);
                mesh.renderMesh(camera, toWorldMatrix, program, 0);
                program.unuse();
                renderChildren(camera);
            }
        }
    }
}
