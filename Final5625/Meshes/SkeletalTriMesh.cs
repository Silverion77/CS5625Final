using System;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Materials;

namespace Chireiden.Meshes
{
    /// <summary>
    /// A triangle mesh where the vertices also deform based on bone positions.
    /// </summary>
    class SkeletalTriMesh : TriMesh
    {
        /// <summary>
        /// For each vertex ID, stores all of the bone IDs that affect that vertex.
        /// For vertex i, the relevant bone IDs are in a contiguous block, beginning at
        /// startOffsetPerVertex[i], and ending right before 
        /// startOffsetPerVertex[i + numBonesPerVertex[i]].
        /// 
        /// These bone IDs must be array indices of the skeleton that will be passed to
        /// the SkeletalMeshGroup containing this mesh.
        /// </summary>
        Vector4[] boneIDsPerVertex;

        /// <summary>
        /// For each vertex ID, stores all of the bone weights that affect that vertex,
        /// in the same order as they are stored in boneIDsPerVertex.
        /// </summary>
        Vector4[] boneWeightsPerVertex;

        int boneIDsVBOHandle;
        int boneWeightsVBOHandle;

        // This is a hell of a constructor
        public SkeletalTriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Vector4[] tans, Material mat,
            Vector4[] boneIDs, Vector4[] boneWeights)
            : base(vs, fs, ns, tcs, tans, mat, false)
        {
            boneIDsPerVertex = boneIDs;
            boneWeightsPerVertex = boneWeights;

            CreateVBOs();
            CreateVAOs();
        }

        protected override void CreateVBOs()
        {
            // Assuming that this has already created the buffers for position, normal, etc.
            base.CreateVBOs();
            
            // Create the VBO for bone ID vectors
            boneIDsVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, boneIDsVBOHandle);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(boneIDsPerVertex.Length * Vector4.SizeInBytes),
                boneIDsPerVertex, BufferUsageHint.StaticDraw);

            // Create the VBO for bone weight vectors
            boneWeightsVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, boneWeightsVBOHandle);
            GL.BufferData<Vector4>(BufferTarget.ArrayBuffer,
                new IntPtr(boneWeightsPerVertex.Length * Vector4.SizeInBytes),
                boneWeightsPerVertex, BufferUsageHint.StaticDraw);

            // Unbind our stuff to clean up
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        protected override void CreateVAOs()
        {
            base.CreateVAOs();
            
            GL.BindVertexArray(vaoHandle);

            // Use index 4 to refer to bone ID sets for vertices.
            GL.EnableVertexAttribArray(4);
            GL.BindBuffer(BufferTarget.ArrayBuffer, boneIDsVBOHandle);
            // Each one is a 4-vector
            GL.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, true, Vector4.SizeInBytes, 0);

            // Use index 5 to refer to the number of bones for vertices.
            GL.EnableVertexAttribArray(5);
            GL.BindBuffer(BufferTarget.ArrayBuffer, boneWeightsVBOHandle);
            GL.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, true, Vector4.SizeInBytes, 0);
             
            // Clean up
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Renders this particular section of the mesh.
        /// 
        /// We can assume that global uniforms such as transformation matrices, which don't
        /// change when we render different parts of the same object, have already been bound.
        /// So all we need to do here is use the VAO that is specific to this mesh,
        /// and bind the material properties for this mesh part.
        /// 
        /// In particular, we can assume that an array of bone transformation matrices
        /// is already there.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        /// <param name="program"></param>
        public override void renderMesh(Camera camera, Matrix4 toWorldMatrix, ShaderProgram program)
        {
            // Bind the stuff we need for this object (VAO, index buffer, program)
            GL.BindVertexArray(vaoHandle);
            material.useMaterialParameters(program);

            GL.DrawElements(PrimitiveType.Triangles, indexBuffer.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            material.unuseMaterialParameters(program);
            GL.BindVertexArray(0);
        }
    }
}
