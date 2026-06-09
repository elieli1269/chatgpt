using System;
using System.Collections.Generic;
using System.IO;
using BlockHaven.Blocks;
using UnityEngine;

namespace BlockHaven.Save
{
    [Serializable]
    public class PlayerSaveData
    {
        public float px;
        public float py;
        public float pz;
        public float yaw;
        public int money = 2000;
        public string currentJob = "Citizen";
        public List<InventorySlotData> inventory = new List<InventorySlotData>();
        public List<string> ownedHouseIds = new List<string>();
        public List<string> ownedVehicleIds = new List<string>();
    }

    [Serializable]
    public class InventorySlotData
    {
        public BlockType blockType;
        public int amount;
    }

    [Serializable]
    public class WorldSaveData
    {
        public int seed;
        public double worldTime;
        public List<BlockRecord> modifiedBlocks = new List<BlockRecord>();
        public List<PropertySaveData> properties = new List<PropertySaveData>();
        public List<VehicleSaveData> vehicles = new List<VehicleSaveData>();
    }

    [Serializable]
    public class EconomySaveData
    {
        public int cityTreasury;
        public List<ShopPriceData> prices = new List<ShopPriceData>();
    }

    [Serializable]
    public class NpcSaveDataCollection
    {
        public List<NpcSaveData> npcs = new List<NpcSaveData>();
    }

    [Serializable]
    public class PropertySaveData
    {
        public string id;
        public string owner;
        public int price;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class VehicleSaveData
    {
        public string id;
        public string owner;
        public string type;
        public float x;
        public float y;
        public float z;
        public float yaw;
    }

    [Serializable]
    public class ShopPriceData
    {
        public string itemId;
        public int buyPrice;
        public int sellPrice;
    }

    [Serializable]
    public class NpcSaveData
    {
        public string id;
        public string displayName;
        public string role;
        public string homeId;
        public string workId;
        public string state;
        public string mood;
        public int money;
        public float x;
        public float y;
        public float z;
    }

    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public string SaveDirectory => Path.Combine(Application.persistentDataPath, "Saves");
        public string WorldPath => Path.Combine(SaveDirectory, "world.json");
        public string PlayerPath => Path.Combine(SaveDirectory, "player.json");
        public string EconomyPath => Path.Combine(SaveDirectory, "economy.json");
        public string NpcsPath => Path.Combine(SaveDirectory, "npcs.json");

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            Directory.CreateDirectory(SaveDirectory);
        }

        public T LoadOrDefault<T>(string path, T fallback)
        {
            if (!File.Exists(path))
            {
                return fallback;
            }

            string json = File.ReadAllText(path);
            return string.IsNullOrWhiteSpace(json) ? fallback : JsonUtility.FromJson<T>(json);
        }

        public void Save<T>(string path, T data)
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
    }
}
