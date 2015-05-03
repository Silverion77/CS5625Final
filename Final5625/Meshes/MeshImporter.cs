using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using OpenTK;

using Assimp;
using Assimp.Configs;

using Chireiden.Materials;

namespace Chireiden.Meshes
{
    public class MeshImporter
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

            foreach (Mesh m in model.Meshes) {
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
            Console.WriteLine("Bone to ID mapping:");
            for (int i = 0; i < bones.Length; i++)
            {
                string name = bones[i].Name;
                Console.WriteLine("  {0} -> {1}", name, i);
                boneDict.Add(name, i);
            }
        }

        public static void collectVertexWeights(Mesh m, Dictionary<string, int> boneDict)
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

            // We've gotten all the data on a per-vertex basis.
            // Now we just want to flatten out these arrays.
            // perVertexNumBones[i] will record how many bones are affecting vertex i.
            int[] vertexNumBones = new int[m.VertexCount];
            // perVertexStartOffset[i] will record the index from which vertex i should
            // start reading to see the bones affecting it.
            int[] vertexStartOffset = new int[m.VertexCount];
            int totalVertBonePairs = 0;
            for (int i = 0; i < m.VertexCount; i++)
            {
                vertexStartOffset[i] = totalVertBonePairs;
                int count = idsLists[i].Count;
                totalVertBonePairs += count;
                vertexNumBones[i] = count;
            }

            // Now we know how many vertex-bone pairs we will have to store,
            // so we can allocate an array of the right length and fill it.
            int[] vertexBoneIDs = new int[totalVertBonePairs];
            float[] vertexBoneWeights = new float[totalVertBonePairs];
            for (int i = 0; i < m.VertexCount; i++)
            {
                int startPointI = vertexStartOffset[i];
                List<int> boneIDs = idsLists[i];
                List<float> weights = weightsLists[i];

                // Iterate over both lists simultaneously; why no List.iter2
                using (var idIterator = boneIDs.GetEnumerator())
                using (var weightIterator = weights.GetEnumerator())
                {
                    int currentOffset = 0;
                    while (idIterator.MoveNext() && weightIterator.MoveNext())
                    {
                        int boneID = idIterator.Current;
                        float weight = weightIterator.Current;
                        vertexBoneIDs[startPointI + currentOffset] = boneID;
                        vertexBoneWeights[startPointI + currentOffset] = weight;
                        currentOffset++;
                    }
                }
            }

            int maxNumBones = 0;

            for (int i = 0; i < m.VertexCount; i++)
            {
                float totalW = 0;
                int numBones = vertexNumBones[i];

                maxNumBones = Math.Max(maxNumBones, numBones);
                /*
                int startPoint = vertexStartOffset[i];
                Console.WriteLine("Vertex {0} has {1} bones affecting it, and starts reading at {2}", i, numBones, startPoint);
                for (int j = startPoint; j < startPoint + numBones; j++)
                {
                    int boneID = vertexBoneIDs[j];
                    float weight = vertexBoneWeights[j];
                    Console.WriteLine("  weight {0} from bone {1}", weight, boneID);
                    totalW += weight;
                }
                Console.WriteLine("  Total weight = {0}", totalW);*/
            }

            Console.WriteLine("Max num bones on any vertex = {0}", maxNumBones);
        }

        /// <summary>
        /// Reads all the meshes contained in the given file and returns them in a list,
        /// having also created the appropriate instances of materials, and loaded the
        /// necessary textures for these meshes.
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static MeshGroup importFromFile(string filename)
        {
            // Create a new importer
            AssimpContext importer = new AssimpContext();

            //This is how we add a configuration (each config is its own class)
            NormalSmoothingAngleConfig config = new NormalSmoothingAngleConfig(66.0f);
            importer.SetConfig(config);

            //This is how we add a logging callback 
            LogStream logstream = new LogStream(delegate(String msg, String userData)
            {
                Console.WriteLine(msg);
            });
            logstream.Attach();

            //Import the model. All configs are set. The model
            //is imported, loaded into managed memory. Then the unmanaged memory is released, and everything is reset.
            Scene model = importer.ImportFile(filename, PostProcessPreset.TargetRealTimeMaximumQuality);

            Console.WriteLine("number of meshes = {0}", model.Meshes.Count);
            Console.WriteLine("number of animations = {0}", model.AnimationCount);
            foreach (Animation a in model.Animations)
            {
                Console.WriteLine("Duration in seconds: {0}", a.DurationInTicks / a.TicksPerSecond);
            }

            // Get the direction where the textures will be
            string directory = System.IO.Path.GetDirectoryName(filename);
            Console.WriteLine("Looking in directory {0}", directory);

            List<TriMesh> outMeshes = new List<TriMesh>();

            //Console.WriteLine("Original node hierarchy:");
            //printNodeTree(model.RootNode, 0);

            // Import the skeleton
            ArmatureBone rootBone;
            ArmatureBone[] boneArray;
            Dictionary<string, int> boneNameDict;
            importBones(model, out rootBone, out boneArray, out boneNameDict);

            //Console.WriteLine("Imported bone hierarchy");
            //rootBone.printBoneTree();

            foreach (Mesh m in model.Meshes)
            {
                // We should only be interested in meshes with vertices
                if (!m.HasVertices) continue;
                var verts = m.Vertices;
                int numVerts = m.VertexCount;

                Vector3[] vertArr = new Vector3[numVerts];

                int i = 0;
                foreach (Vector3D v in verts)
                {
                    vertArr[i] = new Vector3(v.X, v.Y, v.Z);
                    i++;
                }

                // Import the faces
                // Not really sure what to do if there aren't faces, will think about it later
                // if it's actually a problem
                var faces = m.Faces;
                int numFaces = m.FaceCount;
                int[] faceArr = new int[numFaces * 3];
                i = 0;
                foreach (Face f in faces)
                {
                    var indices = f.Indices;
                    if (indices.Count != 3)
                    {
                        Console.WriteLine("ERROR LOADING MESH: THIS AIN'T NO TRIANGLE");
                    }
                    faceArr[i] = indices[0];
                    faceArr[i + 1] = indices[1];
                    faceArr[i + 2] = indices[2];
                    i += 3;
                }

                // Import the normals
                Vector3[] normalArr;
                if (m.HasNormals)
                {
                    var normals = m.Normals;
                    // One normal per vertex
                    normalArr = new Vector3[numVerts];
                    i = 0;
                    foreach (Vector3D n in normals)
                    {
                        normalArr[i] = new Vector3(n.X, n.Y, n.Z);
                        i++;
                    }
                }
                else
                {
                    normalArr = new Vector3[0];
                }

                // This importer supports multiple texture coordinates per vertex,
                // but to keep things simple, we're only going to support one texture
                // coordinate per vertex.
                Vector2[] texCoordArr;
                if (m.TextureCoordinateChannelCount > 0)
                {
                    if (m.UVComponentCount[0] != 2)
                    {
                        Console.WriteLine("ERROR LOADING MESH: UV COORDINATES DON'T HAVE 2 COMPONENTS");
                    }
                    // Just grab the first UV coordinate list
                    var texCoords = m.TextureCoordinateChannels[0];
                    // There should be 1 UV coordinate per vertex
                    texCoordArr = new Vector2[numVerts];
                    i = 0;
                    foreach (Vector3D texC in texCoords)
                    {
                        // Since we're assuming UV coords, the Z component is unused
                        // Also, because OpenGL has texture coordinate (0,0) in the bottom left, but other
                        // programs have (0,0) in the top left, we need to flip Y.
                        texCoordArr[i] = new Vector2(texC.X, 1 - texC.Y);
                        i++;
                    }
                }
                else
                {
                    texCoordArr = new Vector2[0];
                }

                Vector4[] tangentArr;
                if (m.HasNormals && m.HasTangentBasis)
                {
                    tangentArr = new Vector4[numVerts];
                    i = 0;
                    foreach (Vector3D tan in m.Tangents)
                    {
                        tangentArr[i] = new Vector4(tan.X, tan.Y, tan.Z, 1);
                        i++;
                    }
                    // Compute handedness of each tangent
                    i = 0;
                    foreach (Vector3D bit in m.BiTangents)
                    {
                        Vector3 b = new Vector3(bit.X, bit.Y, bit.Z);
                        Vector3 n = normalArr[i];
                        Vector3 t = tangentArr[i].Xyz;
                        // Compare bitangent from cross product with stored one
                        Vector3 crossed_b = Vector3.Cross(n, t);
                        // If they point in opposite directions then handedness is negative
                        if (Vector3.Dot(b, crossed_b) < 0)
                        {
                            tangentArr[i].W = -1;
                        }
                        i++;
                    }
                }
                else
                {
                    tangentArr = new Vector4[0];
                }

                // Get the vertex weights for bones, if there are bones
                if (m.HasBones)
                {
                    collectVertexWeights(m, boneNameDict);
                }

                // Now manufacture the associated material with this mesh
                int matID = m.MaterialIndex;
                Assimp.Material mat = model.Materials[matID];
                BlenderMaterial ourMat = new BlenderMaterial(mat, directory);

                TriMesh outMesh = new TriMesh(vertArr, faceArr, normalArr, texCoordArr, tangentArr, ourMat);
                outMeshes.Add(outMesh);
            }

            MeshGroup group = new MeshGroup(outMeshes);

            //End of example
            importer.Dispose();

            return group;
        }
    }
}
