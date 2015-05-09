using System;
using System.Collections.Generic;

using OpenTK;

using Chireiden.Meshes;
using Chireiden.Materials;

namespace Chireiden.Scenes.Stages
{
    class StageTilesNode : SceneTreeNode
    {
        TriMesh mesh;

        public StageTilesNode(Vector3[] verts, int[] faces, Vector3[] normals, Vector2[] texCoords, Vector4[] tangents, Material mat)
        {
            mesh = new TriMesh(verts, faces, normals, texCoords, tangents, mat);

            // We will maintain coordinates for stage elements in world space directly,
            // since they will never change
            toParentMatrix = Matrix4.Identity;
            toWorldMatrix = Matrix4.Identity;
        }

        public override void update(FrameEventArgs e, Matrix4 parentToWorldMatrix)
        {
            // Nothing needs to be done here
        }

        public override void render(Camera camera)
        {
            ShaderProgram program = ShaderLibrary.LambertianShader;

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            // Compute transformation matrices
            // In this case we always fix the model matrix as the identity, so the modelview is just the view matrix
            Matrix4 modelView = viewMatrix;
            Matrix3 normalMatrix = Utils.normalMatrix(modelView);

            program.use();

            // set shader uniforms, incl. modelview and projection matrices
            program.setUniformMatrix4("projectionMatrix", projectionMatrix);
            program.setUniformMatrix4("modelViewMatrix", modelView);
            program.setUniformMatrix3("normalMatrix", normalMatrix);

            camera.setPointLightUniforms(program);

            mesh.renderMesh(camera, toWorldMatrix, program, 0);

            program.unuse();
        }
    }
}
