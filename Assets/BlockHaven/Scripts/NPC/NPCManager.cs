using System.Collections.Generic;
using BlockHaven.Save;
using UnityEngine;

namespace BlockHaven.NPC
{
    public class NPCManager : MonoBehaviour
    {
        private readonly List<NPCController> npcs = new List<NPCController>();
        private readonly string[] firstNames = { "Alex", "Sam", "Mia", "Noah", "Lina", "Jules", "Emma", "Leo" };
        private readonly string[] roles = { "Citizen", "Police", "Doctor", "Merchant", "Firefighter" };
        private float saveTimer;

        private void Start()
        {
            NpcSaveDataCollection data = SaveManager.Instance.LoadOrDefault(SaveManager.Instance.NpcsPath, new NpcSaveDataCollection());
            if (data.npcs.Count == 0)
            {
                for (int i = 0; i < 24; i++) data.npcs.Add(CreateDefaultNpc(i));
            }

            foreach (NpcSaveData npcData in data.npcs)
            {
                Spawn(npcData);
            }
        }

        private void Update()
        {
            saveTimer += Time.deltaTime;
            if (saveTimer > 15f)
            {
                saveTimer = 0f;
                SaveNpcs();
            }
        }

        public void SaveNpcs()
        {
            NpcSaveDataCollection data = new NpcSaveDataCollection();
            foreach (NPCController npc in npcs)
            {
                data.npcs.Add(npc.ToSaveData());
            }
            SaveManager.Instance.Save(SaveManager.Instance.NpcsPath, data);
        }

        private void Spawn(NpcSaveData data)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = $"NPC {data.displayName}";
            Renderer renderer = go.GetComponent<Renderer>();
            renderer.material.color = Color.HSVToRGB(Mathf.Abs(data.id.GetHashCode() % 100) / 100f, 0.7f, 0.9f);
            NPCController controller = go.AddComponent<NPCController>();
            Vector3 home = HomePosition(data.homeId);
            Vector3 work = WorkPosition(data.role);
            controller.Initialize(data, home, work);
            npcs.Add(controller);
        }

        private NpcSaveData CreateDefaultNpc(int index)
        {
            string role = roles[index % roles.Length];
            return new NpcSaveData
            {
                id = $"npc_{index:000}",
                displayName = firstNames[index % firstNames.Length] + " " + Random.Range(10, 99),
                role = role,
                homeId = $"house_{index % 4}",
                workId = role,
                money = Random.Range(150, 1200),
                mood = "Normal",
                state = "Idle",
                x = -30 + index * 2,
                y = 9,
                z = -8 + index % 7
            };
        }

        private Vector3 HomePosition(string id)
        {
            int hash = Mathf.Abs(id.GetHashCode() % 4);
            Vector3[] homes = { new Vector3(-20, 8, -16), new Vector3(34, 8, -20), new Vector3(-42, 8, 30), new Vector3(12, 8, 18) };
            return homes[hash];
        }

        private Vector3 WorkPosition(string role)
        {
            if (role == "Police") return new Vector3(-12, 8, 30);
            if (role == "Doctor") return new Vector3(26, 8, 24);
            if (role == "Merchant") return new Vector3(10, 8, -40);
            if (role == "Firefighter") return new Vector3(-4, 8, 8);
            return new Vector3(0, 8, 0);
        }

        private void OnApplicationQuit()
        {
            SaveNpcs();
        }
    }
}
