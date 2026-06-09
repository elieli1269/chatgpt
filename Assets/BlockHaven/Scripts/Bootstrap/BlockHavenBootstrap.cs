using BlockHaven.Economy;
using BlockHaven.NPC;
using BlockHaven.Player;
using BlockHaven.Save;
using BlockHaven.UI;
using BlockHaven.Vehicles;
using BlockHaven.World;
using UnityEngine;

namespace BlockHaven.Bootstrap
{
    public static class BlockHavenBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void CreateGame()
        {
            if (Object.FindObjectOfType<SaveManager>() != null) return;
            new GameObject("SaveSystem").AddComponent<SaveManager>();
            new GameObject("WorldManager").AddComponent<WorldManager>();
            new GameObject("EconomyManager").AddComponent<EconomyManager>();
            new GameObject("PropertySystem").AddComponent<PropertySystem>();
            new GameObject("NPCManager").AddComponent<NPCManager>();
            new GameObject("UIManager").AddComponent<BlockHavenUIManager>();
            new GameObject("DayNightWeather").AddComponent<DayNightWeather>();
            CreatePlayer();
            CreateVehicles();
            CreateLighting();
        }

        private static void CreatePlayer()
        {
            GameObject player = new GameObject("BlockHaven Player");
            player.AddComponent<CharacterController>();
            player.AddComponent<BlockHavenPlayerController>();
            player.AddComponent<JobSystem>();
        }

        private static void CreateVehicles()
        {
            CreateVehicle("starter_car", "Car", new Vector3(-8, 9, -8), Color.red);
            CreateVehicle("moto_01", "Moto", new Vector3(8, 9, -8), Color.cyan);
            VehicleController traffic = CreateVehicle("npc_traffic_01", "Car", new Vector3(14, 9, 4), Color.yellow);
            traffic.npcTraffic = true;
        }

        private static VehicleController CreateVehicle(string id, string type, Vector3 pos, Color color)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = id;
            go.transform.position = pos;
            go.transform.localScale = type == "Moto" ? new Vector3(0.9f, 0.8f, 2.2f) : new Vector3(2f, 1f, 3.5f);
            go.GetComponent<Renderer>().material.color = color;
            VehicleController vehicle = go.AddComponent<VehicleController>();
            vehicle.vehicleId = id;
            vehicle.vehicleType = type;
            return vehicle;
        }

        private static void CreateLighting()
        {
            GameObject sun = new GameObject("Day Night Sun");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(0.45f, 0.50f, 0.58f);
        }
    }
}
