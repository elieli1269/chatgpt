using System.Collections.Generic;
using BlockHaven.Player;
using BlockHaven.Save;
using UnityEngine;

namespace BlockHaven.Economy
{
    public class PropertySystem : MonoBehaviour
    {
        private readonly Dictionary<string, PropertySaveData> properties = new Dictionary<string, PropertySaveData>();

        private void Start()
        {
            RegisterDefaults();
            WorldSaveData world = SaveManager.Instance.LoadOrDefault(SaveManager.Instance.WorldPath, new WorldSaveData());
            foreach (PropertySaveData saved in world.properties)
            {
                properties[saved.id] = saved;
            }
        }

        public bool BuyHouse(BlockHavenPlayerController player, string houseId)
        {
            if (!properties.TryGetValue(houseId, out PropertySaveData property)) return false;
            if (!string.IsNullOrEmpty(property.owner)) return false;
            if (!player.SpendMoney(property.price)) return false;
            property.owner = "player";
            properties[houseId] = property;
            return true;
        }

        public List<PropertySaveData> Snapshot()
        {
            return new List<PropertySaveData>(properties.Values);
        }

        private void RegisterDefaults()
        {
            properties["starter_house"] = new PropertySaveData { id = "starter_house", owner = "player", price = 1200, x = -26, y = 7, z = -22 };
            properties["family_house"] = new PropertySaveData { id = "family_house", owner = string.Empty, price = 3500, x = 28, y = 7, z = -26 };
            properties["villa"] = new PropertySaveData { id = "villa", owner = string.Empty, price = 6500, x = -48, y = 7, z = 24 };
        }
    }
}
