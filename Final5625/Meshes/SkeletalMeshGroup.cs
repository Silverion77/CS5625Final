using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Chireiden.Meshes.Animations;

namespace Chireiden.Meshes
{
    /// <summary>
    /// A group of meshes that share the same skeleton, and therefore deform as a unit,
    /// as well as sharing the same world-space transformations.
    /// </summary>
    class SkeletalMeshGroup : MeshContainer {

        List<SkeletalTriMesh> meshes;
        ArmatureBone rootBone;
        ArmatureBone[] bonesByID;

        Matrix4[] boneTransforms;
        MatrixTexture matrixTexture;

        float[] blendShapeWeights;

        Dictionary<string, int> morphLibrary;
        Dictionary<string, AnimationClip> animationLibrary;

        public int NumMorphs { get { return morphLibrary.Count; } }

        void initialize(List<SkeletalTriMesh> meshes, ArmatureBone root, ArmatureBone[] bones, Dictionary<string, int> morphDictionary)
        {
            this.meshes = meshes;
            rootBone = root;
            bonesByID = bones;
            rootBone.computeAllRestTransforms();
            animationLibrary = new Dictionary<string, AnimationClip>();
            boneTransforms = new Matrix4[bonesByID.Length];

            matrixTexture = new MatrixTexture(boneTransforms);

            blendShapeWeights = new float[ShaderProgram.MAX_MORPHS];

            if (morphDictionary == null)
            {
                morphLibrary = new Dictionary<string, int>();
                Console.WriteLine("No morphs");
            }
            else
            {
                morphLibrary = morphDictionary;
                Console.WriteLine("Registered morphs:");
                foreach (var name in morphLibrary.Keys) {
                    Console.WriteLine("  {0}", name);
                }
            }
        }

        public SkeletalMeshGroup(ArmatureBone root, ArmatureBone[] bones, Dictionary<string, int> morphDictionary)
        {
            initialize(new List<SkeletalTriMesh>(), root, bones, morphDictionary);
        }

        public SkeletalMeshGroup(List<SkeletalTriMesh> meshes, ArmatureBone root, ArmatureBone[] bones, Dictionary<string, int> morphDictionary)
        {
            initialize(meshes, root, bones, morphDictionary);
        }

        public void clearPose()
        {
            foreach (ArmatureBone b in bonesByID) {
                b.setPoseToRest();
            }
        }

        public AnimationClip fetchAnimation(string animName)
        {
            AnimationClip clip;
            if (!animationLibrary.TryGetValue(animName, out clip))
            {
                throw new Exception("ERROR: Tried to change animation to \"" + animName + "\", which does not exist");
            }
            return clip;
        }

        public bool hasAnimation(string s)
        {
            return animationLibrary.ContainsKey(s);
        }

        public void addAnimation(AnimationClip c)
        {
            animationLibrary.Add(c.Name, c);
        }

        public void addAnimations(List<AnimationClip> cs)
        {
            foreach (var c in cs) addAnimation(c);
        }

        public void addMesh(SkeletalTriMesh m)
        {
            meshes.Add(m);
        }

        public void removeMesh(SkeletalTriMesh m)
        {
            meshes.Remove(m);
        }

        public void renderMeshes(Camera c, Matrix4 m)
        {
            renderMeshes(c, m, null, 0);
        }

        public int idOfMorph(string name) {
            int id;
            if (!morphLibrary.TryGetValue(name, out id))
            {
                throw new Exception("Morph \"" + name + "\" does not exist");
            }
            return id;
        }

        public void setMorphWeight(int id, float weight)
        {
            blendShapeWeights[id] = weight;
        }

        public float getMorphWeight(int id)
        {
            return blendShapeWeights[id];
        }

        /// <summary>
        /// Renders all the meshes in this group.
        /// 
        /// This also binds the global uniforms that don't change, such as viewing
        /// transformation matrices, to avoid computing the same thing for every
        /// piece of the mesh.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        public void renderMeshes(Camera camera, Matrix4 toWorldMatrix, Clip clip, double time)
        {
            ShaderProgram program = ShaderLibrary.AnimationShader;

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            // Compute transformation matrices
            Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);
            Matrix3 normalMatrix = Utils.normalMatrix(modelView);

            // Apply animated pose if supplied
            if (clip != null)
                clip.applyAnimationToSkeleton(bonesByID, time);
            else
                clearPose();

            // Compute bone transformations
            rootBone.computeAllTransforms();
            // Copy bone matrices into our array, which will be bound as a uniform
            for (int i = 0; i < bonesByID.Length; i++)
            {
                boneTransforms[i] = bonesByID[i].getBoneMatrix();
            }
            int numBones = bonesByID.Length;

            program.use();

            // set shader uniforms, incl. modelview and projection matrices
            program.setUniformMatrix4("projectionMatrix", projectionMatrix);
            program.setUniformMatrix4("modelViewMatrix", modelView);
            program.setUniformMatrix4("viewMatrix", viewMatrix);
            program.setUniformMatrix3("normalMatrix", normalMatrix);

            program.setUniformMat4Texture("bone_matrices", 1, boneTransforms, matrixTexture);
            // Bind morph texture data
            program.setUniformFloatArray("morph_weights", blendShapeWeights);
            program.setUniformInt1("bone_textureWidth", matrixTexture.Width);
            program.setUniformInt1("bone_textureHeight", matrixTexture.Height);

            camera.setPointLightUniforms(program);

            foreach (SkeletalTriMesh m in meshes)
            {
                m.renderMesh(camera, toWorldMatrix, ShaderLibrary.AnimationShader, 2);
            }

            program.unbindTextureRect(1);
            program.unuse();
        }
    }
}
