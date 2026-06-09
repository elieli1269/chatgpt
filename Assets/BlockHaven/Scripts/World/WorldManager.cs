using System.Collections.Generic;
using BlockHaven.Blocks;
using BlockHaven.Save;
using BlockHaven.Vehicles;
using BlockHaven.Economy;
using UnityEngine;

namespace BlockHaven.World
{
    public enum BiomeType { Forest, Mountain, River, City }

    public class WorldManager : MonoBehaviour
    {
        public static WorldManager Instance { get; private set; }

        private readonly Dictionary<Vector3Int, BlockType> modifiedBlocks = new Dictionary<Vector3Int, BlockType>();
        private readonly Dictionary<Vector2Int, VoxelChunk> chunks = new Dictionary<Vector2Int, VoxelChunk>();
        private Material chunkMaterial;
        private Transform player;
        private int seed;
        private float autoSaveTimer;

        public double WorldTime { get; private set; }

        private void Awake()
        {
            Instance = this;
            chunkMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            chunkMaterial.enableInstancing = true;
        }

        private void Start()
        {
            WorldSaveData save = SaveManager.Instance.LoadOrDefault(SaveManager.Instance.WorldPath, new WorldSaveData { seed = Random.Range(10000, 999999) });
            seed = save.seed == 0 ? Random.Range(10000, 999999) : save.seed;
            WorldTime = save.worldTime;
            foreach (BlockRecord record in save.modifiedBlocks)
            {
                modifiedBlocks[record.Position] = record.type;
            }

            CityGenerator.BuildSpawnCity(this);
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
            EnsureChunksAround(Vector3.zero);
        }

        private void Update()
        {
            WorldTime += Time.deltaTime;
            if (player != null)
            {
                EnsureChunksAround(player.position);
            }

            autoSaveTimer += Time.deltaTime;
            if (autoSaveTimer > 20f)
            {
                autoSaveTimer = 0f;
                SaveWorld();
            }
        }

        public BlockType GetBlock(Vector3Int position)
        {
            if (position.y < 0 || position.y >= BlockSystem.ChunkHeight)
            {
                return BlockType.Air;
            }

            if (modifiedBlocks.TryGetValue(position, out BlockType edited))
            {
                return edited;
            }

            return GenerateBlock(position);
        }

        public void SetBlock(Vector3Int position, BlockType type, bool rebuild = true)
        {
            modifiedBlocks[position] = type;
            if (rebuild)
            {
                RebuildChunkAt(position);
            }
        }

        public bool TryBreakBlock(Vector3Int position)
        {
            if (GetBlock(position) == BlockType.Air)
            {
                return false;
            }

            SetBlock(position, BlockType.Air);
            return true;
        }

        public void SaveWorld()
        {
            WorldSaveData data = new WorldSaveData { seed = seed, worldTime = WorldTime };
            foreach (KeyValuePair<Vector3Int, BlockType> entry in modifiedBlocks)
            {
                data.modifiedBlocks.Add(new BlockRecord(entry.Key, entry.Value));
            }

            PropertySystem propertySystem = FindObjectOfType<PropertySystem>();
            if (propertySystem != null)
            {
                data.properties = propertySystem.Snapshot();
            }

            foreach (VehicleController vehicle in FindObjectsOfType<VehicleController>())
            {
                data.vehicles.Add(vehicle.ToSaveData());
            }

            SaveManager.Instance.Save(SaveManager.Instance.WorldPath, data);
        }

        private BlockType GenerateBlock(Vector3Int p)
        {
            if (p.y == 0)
            {
                return BlockType.Stone;
            }

            int height = TerrainHeight(p.x, p.z);
            if (p.y > height)
            {
                return IsRiver(p.x, p.z) && p.y <= 7 ? BlockType.Water : BlockType.Air;
            }

            if (p.y == height)
            {
                return IsRiver(p.x, p.z) ? BlockType.Dirt : BlockType.Grass;
            }

            if (p.y > height - 3)
            {
                return BlockType.Dirt;
            }

            return BlockType.Stone;
        }

        private int TerrainHeight(int x, int z)
        {
            if (Mathf.Abs(x) < 85 && Mathf.Abs(z) < 85)
            {
                return 6;
            }

            float forest = Mathf.PerlinNoise((x + seed) * 0.025f, (z - seed) * 0.025f) * 9f;
            float mountains = Mathf.PerlinNoise((x - seed) * 0.008f, (z + seed) * 0.008f) * 22f;
            return Mathf.RoundToInt(5 + forest + (mountains > 13f ? mountains : 0f));
        }

        private bool IsRiver(int x, int z)
        {
            float curve = Mathf.Sin(x * 0.045f) * 18f;
            return Mathf.Abs(z - curve) < 4f && Mathf.Abs(x) > 95;
        }

        private void EnsureChunksAround(Vector3 worldPosition)
        {
            Vector2Int center = WorldToChunk(worldPosition);
            for (int dx = -BlockSystem.ViewDistanceInChunks; dx <= BlockSystem.ViewDistanceInChunks; dx++)
            {
                for (int dz = -BlockSystem.ViewDistanceInChunks; dz <= BlockSystem.ViewDistanceInChunks; dz++)
                {
                    Vector2Int coord = new Vector2Int(center.x + dx, center.y + dz);
                    if (!chunks.ContainsKey(coord))
                    {
                        CreateChunk(coord);
                    }
                }
            }
        }

        private void CreateChunk(Vector2Int coord)
        {
            GameObject go = new GameObject($"Chunk {coord.x},{coord.y}");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(coord.x * BlockSystem.ChunkSize, 0, coord.y * BlockSystem.ChunkSize);
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = chunkMaterial;
            VoxelChunk chunk = go.AddComponent<VoxelChunk>();
            chunks[coord] = chunk;
            chunk.Initialize(this, coord);
        }

        private void RebuildChunkAt(Vector3Int position)
        {
            Vector2Int coord = WorldToChunk(position);
            if (chunks.TryGetValue(coord, out VoxelChunk chunk))
            {
                chunk.Rebuild();
            }
        }

        private Vector2Int WorldToChunk(Vector3 position)
        {
            return new Vector2Int(Mathf.FloorToInt(position.x / BlockSystem.ChunkSize), Mathf.FloorToInt(position.z / BlockSystem.ChunkSize));
        }
    }
}
