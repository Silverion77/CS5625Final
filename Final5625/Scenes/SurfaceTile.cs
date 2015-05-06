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
    /// <summary>
    /// An object in the scene tree to be used as a surface for the game. E.g. walls, floor, etc.
    /// </summary>
    public class SurfaceTile : PlaceableObject
    {
        /// <summary>
        /// TriMesh square backing this surface tile
        /// </summary>
        private TriMesh square;
        public SurfaceTile(float s, Vector3 t, Quaternion r)
            : base(s, t, r)
        {
            Vector3 v00 = new Vector3(0, 0, 0);
            Vector3 v10 = new Vector3(1, 0, 0);
            Vector3 v01 = new Vector3(0, 1, 0);
            Vector3 v11 = new Vector3(1, 1, 0);
            Vector3[] vs = { v00, v01, v10, v11 };
            int[] fs = { 0, 1, 2, 3, 2, 1 };
            Vector3 n = new Vector3(0, 0, 1);
            Vector3[] ns = { n, n, n, n };
            Vector2 tc00 = new Vector2(0, 0);
            Vector2 tc10 = new Vector2(1, 0);
            Vector2 tc01 = new Vector2(0, 1);
            Vector2 tc11 = new Vector2(1, 1);
            Vector2[] tcs = { tc00, tc01, tc10, tc11 };
            Vector4 tan = new Vector4(1, 0, 0, 1);
            Vector4[] tans = { tan, tan, tan, tan };
            square = new TriMesh(vs, fs, ns, tcs, tans, new POMMaterial());
        }
        
        public SurfaceTile()
            : base (1, new Vector3(0, 0, 0), Quaternion.Identity)
        { }

        public override void render(Camera camera)
        {
            ShaderProgram program = ShaderLibrary.POMShader;

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);
            Matrix3 normalMatrix = Utils.normalMatrix(modelView);
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            program.use();
            // set shader uniforms
            program.setUniformMatrix4("modelMatrix", toWorldMatrix);
            program.setUniformMatrix4("viewMatrix", viewMatrix);
            program.setUniformMatrix4("projectionMatrix", projectionMatrix);
            program.setUniformMatrix3("normalMatrix", normalMatrix);
            program.setUniformFloat3("camera_position", camera.getWorldSpacePos());
            square.renderMesh(camera, toWorldMatrix, program, 0);
            program.unuse();
            renderChildren(camera);
        }
    }
}
