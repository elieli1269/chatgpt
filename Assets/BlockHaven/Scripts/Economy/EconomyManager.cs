using System.Collections.Generic;
using BlockHaven.Player;
using BlockHaven.Save;
using UnityEngine;

namespace BlockHaven.Economy
{
    public class EconomyManager : MonoBehaviour
    {
        public static EconomyManager Instance { get; private set; }
        public int CityTreasury { get; private set; } = 100000;
        public Dictionary<string, ShopPriceData> Prices { get; } = new Dictionary<string, ShopPriceData>();

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            EconomySaveData data = SaveManager.Instance.LoadOrDefault(SaveManager.Instance.EconomyPath, DefaultEconomy());
            CityTreasury = data.cityTreasury;
            foreach (ShopPriceData price in data.prices) Prices[price.itemId] = price;
        }

        public bool Buy(BlockHavenPlayerController player, string itemId)
        {
            if (!Prices.TryGetValue(itemId, out ShopPriceData price) || !player.SpendMoney(price.buyPrice)) return false;
            CityTreasury += Mathf.RoundToInt(price.buyPrice * 0.08f);
            SaveEconomy();
            return true;
        }

        public void PaySalary(BlockHavenPlayerController player, string jobId)
        {
            int salary = jobId == "Police" ? 180 : jobId == "Doctor" ? 220 : jobId == "Delivery" ? 120 : 90;
            player.AddMoney(salary);
            CityTreasury -= salary;
            SaveEconomy();
        }

        public void SaveEconomy()
        {
            EconomySaveData data = new EconomySaveData { cityTreasury = CityTreasury };
            foreach (ShopPriceData price in Prices.Values) data.prices.Add(price);
            SaveManager.Instance.Save(SaveManager.Instance.EconomyPath, data);
        }

        private EconomySaveData DefaultEconomy()
        {
            return new EconomySaveData
            {
                cityTreasury = 100000,
                prices = new List<ShopPriceData>
                {
                    new ShopPriceData { itemId = "starter_house", buyPrice = 1200, sellPrice = 850 },
                    new ShopPriceData { itemId = "family_house", buyPrice = 3500, sellPrice = 2500 },
                    new ShopPriceData { itemId = "car", buyPrice = 900, sellPrice = 600 },
                    new ShopPriceData { itemId = "moto", buyPrice = 550, sellPrice = 350 }
                }
            };
        }
    }
}
