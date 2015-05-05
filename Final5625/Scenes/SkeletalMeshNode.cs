using System;
using OpenTK;

using Chireiden.Meshes;
using Chireiden.Meshes.Animations;

namespace Chireiden.Scenes
{
    class SkeletalMeshNode : MeshNode
    {
        protected double animTime = 0;
        protected AnimationClip currentAnimation = null;

        public SkeletalMeshNode(MeshContainer m)
            : base(m)
        { }

        public SkeletalMeshNode(MeshContainer m, Vector3 loc)
            : base(m, loc)
        { }

        public void advanceAnimation(double delta) {
            if (currentAnimation == null || currentAnimation.Duration == 0) animTime = 0;
            else animTime = (animTime + delta) % currentAnimation.Duration;
        }

        public void clearAnimation()
        {
            currentAnimation = null;
            animTime = 0;
        }

        public void switchAnimation(string animation)
        {
            animTime = 0;
            currentAnimation = meshes.fetchAnimation(animation);
        }

        public void switchAnimationSmooth(string animation)
        {
            // If the current animation is the same as the one we're switching to,
            // don't actually do anything.
            if (currentAnimation == null || !currentAnimation.Name.Equals(animation))
            {
                // TODO: add a slight interpolation time between the two animations
                switchAnimation(animation);
            }
        }

        public override void render(Camera camera)
        {
            meshes.renderMeshes(camera, toWorldMatrix, currentAnimation, animTime);
            renderChildren(camera);
        }
    }
}
