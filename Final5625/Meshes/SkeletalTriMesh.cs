using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chireiden.Meshes
{
    /// <summary>
    /// A triangle mesh where the vertices also deform based on bone positions.
    /// </summary>
    class SkeletalTriMesh
    {
        /// <summary>
        /// For each vertex, stores the number of bones affecting this vertex.
        /// </summary>
        int[] numBonesPerVertex;

        /// <summary>
        /// Stores the bones in ascending order.
        /// </summary>
        int[] boneIDsPerVertex;
    }
}
