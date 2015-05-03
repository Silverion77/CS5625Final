using System;
using OpenTK;

using Chireiden.Meshes;
using Chireiden.Meshes.Animations;

namespace Chireiden.Scenes
{
    class SkeletalMeshNode : MeshNode
    {
        public SkeletalMeshNode(MeshContainer m)
            : base(m)
        { }

        public SkeletalMeshNode(MeshContainer m, Vector3 loc)
            : base(m, loc)
        { }

        public void advanceAnimation(double delta) {
            meshes.advanceAnimation(delta);
        }

        public void clearAnimation()
        {
            meshes.clearAnimation();
        }

        public void switchAnimation(string animation)
        {
            meshes.setCurrentAnimation(animation);
        }
    }
}
