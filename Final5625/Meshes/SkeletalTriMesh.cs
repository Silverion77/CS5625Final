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

        /// <summary>
        /// For each vertex, stores a block of displacements representing the maximum
        /// displacement of that vertex by each relevant blend shape.
        /// (x,y,z) give the displacement, while w gives the ID of the morph.
        /// </summary>
        Vector4[] blendShapeDisplacements;
        Vector4Texture blendShapeTexture;

        /// <summary>
        /// For each vertex, stores the first index in blendShapeDisplacements that refers
        /// to this vertex.
        /// </summary>
        float[] blendShapeStartIndices;

        /// <summary>
        /// For each vertex, stores the number of values in blendShapeDisplacements that
        /// affect this vertex.
        /// </summary>
        float[] blendShapeCount;

        bool hasBlendShapes = false;

        int boneIDsVBOHandle;
        int boneWeightsVBOHandle;

        int blendShapeStartVBOHandle;
        int blendShapeCountVBOHandle;

        // This is a hell of a constructor
        public SkeletalTriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Vector4[] tans, Material mat,
            Vector4[] boneIDs, Vector4[] boneWeights)
            : base(vs, fs, ns, tcs, tans, mat, false)
        {
            // Bones are required for this class
            boneIDsPerVertex = boneIDs;
            boneWeightsPerVertex = boneWeights;

            // Blend shapes are optional; put some default values
            blendShapeStartIndices = new float[vertices.Length];
            blendShapeCount = new float[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                blendShapeCount[i] = 0;
            }
            blendShapeDisplacements = new Vector4[1];
            blendShapeTexture = new Vector4Texture(blendShapeDisplacements);

            CreateVBOs();
            CreateVAOs();
        }

        public SkeletalTriMesh(Vector3[] vs, int[] fs, Vector3[] ns, Vector2[] tcs, Vector4[] tans, Material mat,
            Vector4[] boneIDs, Vector4[] boneWeights, Vector3[][] displacements)
            : base(vs, fs, ns, tcs, tans, mat, false)
        {
            // Bones are required for this class
            boneIDsPerVertex = boneIDs;
            boneWeightsPerVertex = boneWeights;

            setupBlendShapes(displacements);
            blendShapeTexture = new Vector4Texture(blendShapeDisplacements);

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

            // VBO for blend shape start indices
            blendShapeStartVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, blendShapeStartVBOHandle);
            GL.BufferData<float>(BufferTarget.ArrayBuffer,
                new IntPtr(blendShapeStartIndices.Length * sizeof(float)),
                blendShapeStartIndices, BufferUsageHint.StaticDraw);

            // VBO for blend shape counts
            blendShapeCountVBOHandle = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, blendShapeCountVBOHandle);
            GL.BufferData<float>(BufferTarget.ArrayBuffer,
                new IntPtr(blendShapeCount.Length * sizeof(float)),
                blendShapeCount, BufferUsageHint.StaticDraw);

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

            // Use index 6 to refer to the start index for each vertex in the blend shape array.
            GL.EnableVertexAttribArray(6);
            GL.BindBuffer(BufferTarget.ArrayBuffer, blendShapeStartVBOHandle);
            GL.VertexAttribPointer(6, 1, VertexAttribPointerType.Float, false, sizeof(float), 0);
             
            // Use index 7 to refer to the number of affecting morphs for each vertex.
            GL.EnableVertexAttribArray(7);
            GL.BindBuffer(BufferTarget.ArrayBuffer, blendShapeCountVBOHandle);
            GL.VertexAttribPointer(7, 1, VertexAttribPointerType.Float, false, sizeof(float), 0);

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
        public override void renderMesh(Camera camera, Matrix4 toWorldMatrix, ShaderProgram program, int startTexUnit)
        {
            // Bind the stuff we need for this object (VAO, index buffer, program)
            GL.BindVertexArray(vaoHandle);
            program.bindTextureRect("morph_displacements", startTexUnit, blendShapeTexture);
            material.useMaterialParameters(program, startTexUnit + 1);

            GL.DrawElements(PrimitiveType.Triangles, indexBuffer.Length, DrawElementsType.UnsignedInt, IntPtr.Zero);

            // Clean up
            material.unuseMaterialParameters(program, startTexUnit + 1);
            program.unbindTextureRect(startTexUnit);
            GL.BindVertexArray(0);
        }

        /// <summary>
        /// Given an array where displacements[m][v] gives the displacement that morph m exerts
        /// on vertex v, sets this mesh's local fields to reflect that.
        /// </summary>
        /// <param name="displacements"></param>
        public void setupBlendShapes(Vector3[][] displacements)
        {
            hasBlendShapes = true;
            List<Vector4> morphDisps = new List<Vector4>();
            int totalMorphs = 0;

            blendShapeStartIndices = new float[vertices.Length];
            blendShapeCount = new float[vertices.Length];

            // Now we can populate the other arrays by vertex
            for (int vertID = 0; vertID < vertices.Length; vertID++)
            {
                blendShapeStartIndices[vertID] = totalMorphs;
                int numMorphs = 0;
                for (int morphID = 0; morphID < displacements.Length; morphID++)
                {
                    if (displacements[morphID] == null) continue;
                    Vector3 displacement = displacements[morphID][vertID];
                    if (displacement.Equals(Vector3.Zero)) continue;
                    Vector4 dispAndID = new Vector4(displacement, morphID);
                    morphDisps.Add(dispAndID);
                    numMorphs++;
                }
                blendShapeCount[vertID] = numMorphs;
                totalMorphs += numMorphs;
            }
            blendShapeDisplacements = morphDisps.ToArray();
            if (blendShapeDisplacements.Length == 0)
                blendShapeDisplacements = new Vector4[1];
        }
    }
}
