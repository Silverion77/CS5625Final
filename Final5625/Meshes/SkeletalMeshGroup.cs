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
    class SkeletalMeshGroup : MeshContainer
    {
        List<SkeletalTriMesh> meshes;
        ArmatureBone rootBone;
        ArmatureBone[] bonesByID;

        Matrix4[] boneTransforms;
        MatrixTexture matrixTexture;

        Dictionary<string, AnimationClip> animationLibrary;

        void initialize(List<SkeletalTriMesh> meshes, ArmatureBone root, ArmatureBone[] bones)
        {
            this.meshes = meshes;
            rootBone = root;
            bonesByID = bones;
            rootBone.computeAllRestTransforms();
            animationLibrary = new Dictionary<string, AnimationClip>();
            boneTransforms = new Matrix4[bonesByID.Length];

            matrixTexture = new MatrixTexture(boneTransforms);
        }

        public SkeletalMeshGroup(ArmatureBone root, ArmatureBone[] bones)
        {
            initialize(new List<SkeletalTriMesh>(), root, bones);
        }

        public SkeletalMeshGroup(List<SkeletalTriMesh> meshes, ArmatureBone root, ArmatureBone[] bones)
        {
            initialize(meshes, root, bones);
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

        public void addAnimation(AnimationClip c)
        {
            Console.WriteLine("Added animation {0} to skeletal mesh group, duration {1}", c.Name, c.Duration);
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

        /// <summary>
        /// Renders all the meshes in this group.
        /// 
        /// This also binds the global uniforms that don't change, such as viewing
        /// transformation matrices, to avoid computing the same thing for every
        /// piece of the mesh.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        public void renderMeshes(Camera camera, Matrix4 toWorldMatrix, AnimationClip clip, double time)
        {
            ShaderProgram program = Shaders.AnimationShader;

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            // Compute transformation matrices
            Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);

            if (clip != null && clip.Name.Equals("raise_arm"))
            {
                //Console.WriteLine("OKUU CLIP: DURATION {0}", clip.Duration);
            }

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

            // Bind bone texture data
            program.setUniformMat4Texture("bone_matrices", 0, boneTransforms, matrixTexture);
            program.setUniformInt1("bone_textureWidth", matrixTexture.Width);
            program.setUniformInt1("bone_textureHeight", matrixTexture.Height);

            camera.setPointLightUniforms(program);

            foreach (SkeletalTriMesh m in meshes)
            {
                m.renderMesh(camera, toWorldMatrix, Shaders.AnimationShader, 1);
            }

            program.unbindTextureRect(0);
            program.unuse();
        }
    }
}
