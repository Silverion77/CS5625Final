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

        Dictionary<string, AnimationClip> animationLibrary;

        AnimationClip current;
        double animTime;

        void initialize(List<SkeletalTriMesh> meshes, ArmatureBone root, ArmatureBone[] bones)
        {
            this.meshes = meshes;
            rootBone = root;
            bonesByID = bones;
            rootBone.computeAllRestTransforms();
            animationLibrary = new Dictionary<string, AnimationClip>();
            current = null;
            animTime = 0;
            boneTransforms = new Matrix4[ShaderProgram.MAX_BONES];
        }

        public SkeletalMeshGroup(ArmatureBone root, ArmatureBone[] bones)
        {
            initialize(new List<SkeletalTriMesh>(), root, bones);
        }

        public SkeletalMeshGroup(List<SkeletalTriMesh> meshes, ArmatureBone root, ArmatureBone[] bones)
        {
            initialize(meshes, root, bones);
            root.printBoneTree();
        }

        public void clearAnimation()
        {
            current = null;
            animTime = 0;
            foreach (ArmatureBone b in bonesByID) {
                b.setPoseRotation(Matrix4.Identity);
                b.setPoseTranslation(Matrix4.Identity);
            }
        }

        public void setCurrentAnimation(string animName)
        {
            AnimationClip clip;
            if (!animationLibrary.TryGetValue(animName, out clip))
            {
                Console.WriteLine("ERROR: Tried to change animation to \"{0}\", which does not exist");
                return;
            }
            current = clip;
            animTime = 0;
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

        /// <summary>
        /// Renders all the meshes in this group.
        /// 
        /// This also binds the global uniforms that don't change, such as viewing
        /// transformation matrices, to avoid computing the same thing for every
        /// piece of the mesh.
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="toWorldMatrix"></param>
        public void renderMeshes(Camera camera, Matrix4 toWorldMatrix)
        {
            ShaderProgram program = Shaders.AnimationShader;

            Matrix4 viewMatrix = camera.getViewMatrix();
            Matrix4 projectionMatrix = camera.getProjectionMatrix();

            // Compute transformation matrices
            Matrix4 modelView = Matrix4.Mult(toWorldMatrix, viewMatrix);

            if (current != null)
            {
                current.applyAnimationToSkeleton(bonesByID, animTime);
            }
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

            // TODO: bind num_bones uniform
            // TODO: bind bone_matrices array uniform
            program.setUniformInt1("bone_count", numBones);
            program.setUniformMat4Array("bone_matrices", boneTransforms);

            // TODO: SET BONE ARRAY UNIFORMS

            camera.setPointLightUniforms(program);

            foreach (SkeletalTriMesh m in meshes)
            {
                m.renderMesh(camera, toWorldMatrix, Shaders.AnimationShader);
            }
            program.unuse();
        }

        public void advanceAnimation(float delta)
        {
            if (current == null) return;
            double newTime = animTime + delta;
            if (current.Wrap)
                newTime = Math.IEEERemainder(newTime, current.Duration);
        }
    }
}
