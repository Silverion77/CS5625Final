using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using OpenTK;

using Assimp;
using Assimp.Configs;

using Chireiden.Materials;

namespace Chireiden.Meshes
{
    public partial class MeshImporter
    {
        /// <summary>
        /// Finds the root bone in the armature. This should be the child
        /// of the node named "Armature".
        /// </summary>
        /// <param name="rootNode"></param>
        /// <returns></returns>
        public static Node findArmature(Node rootNode)
        {
            if (rootNode.Name.Equals("Armature"))
            {
                if (rootNode.HasChildren)
                {
                    Node child = rootNode.Children[0];
                    Console.WriteLine("Root bone is {0}", child.Name);
                    return child;
                }
            }
            else
            {
                foreach (Node c in rootNode.Children)
                {
                    Node result = findArmature(c);
                    if (result != null) return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Given a set of nodes, the root node of a tree, and a node searchTarget that we're looking for,
        /// finds searchTarget in the tree, adds it to the set, and adds the transitive closure of its
        /// parents to the set. Returns true if and only if we were able to find searchTarget.
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="rootNode"></param>
        /// <param name="searchTarget"></param>
        /// <returns></returns>
        public static bool addBranchToSet(HashSet<Node> nodes, Node rootNode, Bone searchTarget)
        {
            // If we are at the search target, then we just add the root and are done
            if (rootNode.Name.Equals(searchTarget.Name))
            {
                nodes.Add(rootNode);
                return true;
            }
            else
                // If the search target is in one of our subtrees, then we find that
                // subtree, add all of the target's parents in that subtree,
                // and also add ourselves (since we're also a parent).
                foreach (Node child in rootNode.Children)
                {
                    if (addBranchToSet(nodes, child, searchTarget))
                    {
                        nodes.Add(rootNode);
                        return true;
                    }
                }
            // Otherwise the search target is neither at the root nor in any subtree, so
            // it must not be in the tree.
            return false;
        }

        public static void printNodeTree(Node rootNode, int level)
        {
            string spaces = new string(' ', level * 2);
            Console.WriteLine("{0}{1}", spaces, rootNode.Name);
            foreach (Node child in rootNode.Children)
            {
                printNodeTree(child, level + 1);
            }
        }

        /// <summary>
        /// Converts an AssImp matrix into an OpenTK matrix.
        /// </summary>
        /// <param name="inputMat"></param>
        /// <returns></returns>
        public static Matrix4 convertMatrix(Matrix4x4 inputMat)
        {
            // This is the best code I've ever written
            Matrix4 matrix = new Matrix4();
            matrix.M11 = inputMat.A1;
            matrix.M12 = inputMat.A2;
            matrix.M13 = inputMat.A3;
            matrix.M14 = inputMat.A4;

            matrix.M21 = inputMat.B1;
            matrix.M22 = inputMat.B2;
            matrix.M23 = inputMat.B3;
            matrix.M24 = inputMat.B4;

            matrix.M31 = inputMat.C1;
            matrix.M32 = inputMat.C2;
            matrix.M33 = inputMat.C3;
            matrix.M34 = inputMat.C4;

            matrix.M41 = inputMat.D1;
            matrix.M42 = inputMat.D2;
            matrix.M43 = inputMat.D3;
            matrix.M44 = inputMat.D4;

            // Correction: the above is the worst code, and this is the best code
            matrix.Transpose();

            // nb. AssImp stores matrices in row-major form, but OpenTK
            // stores them in column-major form (so the translations in translation
            // matrices are at the bottom, and not at the right).
            // Column-major is also why the result of Mult(A,B) actually applies
            // the transformation of A before B.

            return matrix;
        }

        /// <summary>
        /// Given the set of nodes that we know are actually in the skeleton,
        /// constructs the hierarchy of ArmatureBones from the Nodes read by AssImp.
        /// </summary>
        /// <param name="neededNodes"></param>
        /// <param name="root"></param>
        /// <returns></returns>
        public static ArmatureBone constructArmature(HashSet<Node> neededNodes, Node root)
        {
            ArmatureBone rootBone = null;
            if (neededNodes.Contains(root))
            {
                rootBone = new ArmatureBone(convertMatrix(root.Transform), root.Name);
                foreach (Node child in root.Children)
                {
                    ArmatureBone childBone = constructArmature(neededNodes, child);
                    rootBone.addChild(childBone);
                }
            }
            return rootBone;
        }

        /// <summary>
        /// Flattens the given ArmatureBone hierarchy into an array, starting at the given offset.
        /// The array must be large enough to hold all the bones. Returns the number
        /// of bones we added to the array.
        /// </summary>
        /// <param name="root"></param>
        /// <param name="array"></param>
        /// <param name="startIndex"></param>
        /// <returns></returns>
        public static int placeBonesInArray(ArmatureBone root, ArmatureBone[] array, int startIndex)
        {
            array[startIndex] = root;
            root.BoneID = startIndex;
            int offset = 1;
            foreach (ArmatureBone child in root.Children)
            {
                int numAddedChild = placeBonesInArray(child, array, startIndex + offset);
                offset += numAddedChild;
            }
            return offset;
        }

        /// <summary>
        /// Given an imported model, constructs the hierarchy of ArmatureBones that will make up
        /// its skeleton.
        /// </summary>
        /// <param name="model">The imported model.</param>
        /// <param name="root">The bone hierarchy itself.</param>
        /// <param name="bones">An array containing the same set of bones as the returned hierarchy.</param>
        /// <param name="boneDict">A dictionary mapping names of bones to array indices.</param>
        public static void importBones(Scene model, out ArmatureBone root, out ArmatureBone[] bones, out Dictionary<string, int> boneDict)
        {
            // First collect the set of all bones that are used by any meshes.
            HashSet<Node> neededNodes = new HashSet<Node>();
            Node armatureRoot = findArmature(model.RootNode);
            if (armatureRoot == null)
            {
                root = null;
                bones = null;
                boneDict = null;
                return;
            }

            foreach (Mesh m in model.Meshes)
            {
                foreach (Bone b in m.Bones)
                {
                    addBranchToSet(neededNodes, armatureRoot, b);
                }
            }

            // Now that we know which bones are actually used, we can make the hierarchy.
            root = constructArmature(neededNodes, armatureRoot);
            // Now flatten it out into an array, which we will want to be able to set animation frames easily.
            bones = new ArmatureBone[neededNodes.Count];
            placeBonesInArray(root, bones, 0);
            // Lastly establish a mapping from bone names to the above array indices; this will
            // help us when we go to read in the animation data.
            boneDict = new Dictionary<string, int>();
            for (int i = 0; i < bones.Length; i++)
            {
                string name = bones[i].Name;
                boneDict.Add(name, i);
            }
        }

        public static void collectVertexWeights(Mesh m, Dictionary<string, int> boneDict,
            out Vector4[] vertexBoneIDs, out Vector4[] vertexBoneWeights)
        {
            List<int>[] idsLists = new List<int>[m.VertexCount];
            List<float>[] weightsLists = new List<float>[m.VertexCount];
            for (int i = 0; i < idsLists.Length; i++)
            {
                idsLists[i] = new List<int>();
                weightsLists[i] = new List<float>();
            }
            foreach (Bone b in m.Bones)
            {
                foreach (VertexWeight vw in b.VertexWeights)
                {
                    // Get the array index of this bone
                    int boneIndex;
                    if (!boneDict.TryGetValue(b.Name, out boneIndex))
                        throw new Exception("Nonexistent bone " + b.Name + " referenced by mesh " + m.Name);
                    // Now we want to record that the vertex specified by vw.VertexID
                    // is affected by this bone, with weight vw.Weight.
                    idsLists[vw.VertexID].Add(boneIndex);
                    weightsLists[vw.VertexID].Add(vw.Weight);
                }
            }

            // Now we know how many vertex-bone pairs we will have to store,
            // so we can allocate an array of the right length and fill it.
            int[][] vertBonesArr = new int[m.VertexCount][];
            float[][] vertWtsArr = new float[m.VertexCount][];
            for (int i = 0; i < m.VertexCount; i++)
            {
                vertBonesArr[i] = new int[ShaderProgram.MAX_BONES_PER_VERTEX];
                vertWtsArr[i] = new float[ShaderProgram.MAX_BONES_PER_VERTEX];
            }

            for (int i = 0; i < m.VertexCount; i++)
            {
                List<int> boneIDs = idsLists[i];
                List<float> weights = weightsLists[i];

                // Iterate over both lists simultaneously; why no List.iter2
                using (var idIterator = boneIDs.GetEnumerator())
                using (var weightIterator = weights.GetEnumerator())
                {
                    for (int currentOffset = 0; currentOffset < ShaderProgram.MAX_BONES_PER_VERTEX; currentOffset++)
                    {
                        if (idIterator.MoveNext() && weightIterator.MoveNext())
                        {
                            int boneID = idIterator.Current;
                            float weight = weightIterator.Current;
                            vertBonesArr[i][currentOffset] = boneID;
                            vertWtsArr[i][currentOffset] = weight;
                        }
                        else
                        {
                            vertBonesArr[i][currentOffset] = -1;
                            vertWtsArr[i][currentOffset] = 0;
                        }
                    }
                }
            }
            vertexBoneIDs = new Vector4[m.VertexCount];
            vertexBoneWeights = new Vector4[m.VertexCount];
            for (int i = 0; i < m.VertexCount; i++)
            {
                vertexBoneIDs[i] = new Vector4(vertBonesArr[i][0], vertBonesArr[i][1], vertBonesArr[i][2], vertBonesArr[i][3]);
                vertexBoneWeights[i] = new Vector4(vertWtsArr[i][0], vertWtsArr[i][1], vertWtsArr[i][2], vertWtsArr[i][3]);
            }
        }
    }
}
