using System.Collections.Generic;
using BlockHaven.Blocks;
using UnityEngine;

namespace BlockHaven.World
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class VoxelChunk : MonoBehaviour
    {
        private readonly List<Vector3> vertices = new List<Vector3>(4096);
        private readonly List<int> triangles = new List<int>(4096);
        private readonly List<Color> colors = new List<Color>(4096);
        private Mesh mesh;
        private WorldManager world;
        private Vector2Int chunkCoord;

        private static readonly Vector3Int[] Directions =
        {
            Vector3Int.forward, Vector3Int.back, Vector3Int.left, Vector3Int.right, Vector3Int.up, Vector3Int.down
        };

        public void Initialize(WorldManager owner, Vector2Int coord)
        {
            world = owner;
            chunkCoord = coord;
            mesh = new Mesh { name = $"Chunk_{coord.x}_{coord.y}" };
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            GetComponent<MeshFilter>().sharedMesh = mesh;
            Rebuild();
        }

        public void Rebuild()
        {
            vertices.Clear();
            triangles.Clear();
            colors.Clear();

            int baseX = chunkCoord.x * BlockSystem.ChunkSize;
            int baseZ = chunkCoord.y * BlockSystem.ChunkSize;
            for (int x = 0; x < BlockSystem.ChunkSize; x++)
            {
                for (int z = 0; z < BlockSystem.ChunkSize; z++)
                {
                    for (int y = 0; y < BlockSystem.ChunkHeight; y++)
                    {
                        Vector3Int pos = new Vector3Int(baseX + x, y, baseZ + z);
                        BlockType type = world.GetBlock(pos);
                        if (type == BlockType.Air)
                        {
                            continue;
                        }

                        for (int face = 0; face < Directions.Length; face++)
                        {
                            if (BlockSystem.IsTransparent(world.GetBlock(pos + Directions[face])))
                            {
                                AddFace(new Vector3(x, y, z), face, BlockSystem.ColorFor(type));
                            }
                        }
                    }
                }
            }

            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(colors);
            mesh.RecalculateNormals();
            GetComponent<MeshCollider>().sharedMesh = mesh;
        }

        private void AddFace(Vector3 origin, int direction, Color color)
        {
            int start = vertices.Count;
            Vector3[] quad = FaceVertices(origin, direction);
            vertices.AddRange(quad);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
        }

        private static Vector3[] FaceVertices(Vector3 o, int direction)
        {
            switch (direction)
            {
                case 0: return new[] { o + new Vector3(0, 0, 1), o + new Vector3(1, 0, 1), o + new Vector3(1, 1, 1), o + new Vector3(0, 1, 1) };
                case 1: return new[] { o + new Vector3(1, 0, 0), o + new Vector3(0, 0, 0), o + new Vector3(0, 1, 0), o + new Vector3(1, 1, 0) };
                case 2: return new[] { o + new Vector3(0, 0, 0), o + new Vector3(0, 0, 1), o + new Vector3(0, 1, 1), o + new Vector3(0, 1, 0) };
                case 3: return new[] { o + new Vector3(1, 0, 1), o + new Vector3(1, 0, 0), o + new Vector3(1, 1, 0), o + new Vector3(1, 1, 1) };
                case 4: return new[] { o + new Vector3(0, 1, 1), o + new Vector3(1, 1, 1), o + new Vector3(1, 1, 0), o + new Vector3(0, 1, 0) };
                default: return new[] { o + new Vector3(0, 0, 0), o + new Vector3(1, 0, 0), o + new Vector3(1, 0, 1), o + new Vector3(0, 0, 1) };
            }
        }
    }
}
