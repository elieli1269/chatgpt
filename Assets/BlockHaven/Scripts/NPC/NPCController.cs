using BlockHaven.Save;
using UnityEngine;
using UnityEngine.AI;

namespace BlockHaven.NPC
{
    public enum NpcState { Idle, Travel, Work, Shop, Sleep }
    public enum NpcMood { Happy, Normal, Tired }

    [RequireComponent(typeof(CapsuleCollider))]
    public class NPCController : MonoBehaviour
    {
        public string npcId;
        public string displayName;
        public string role;
        public string homeId;
        public string workId;
        public int money = 300;
        public NpcState state;
        public NpcMood mood;
        public string dialogue = "Bonjour ! Bienvenue à BlockHaven.";

        private NavMeshAgent agent;
        private Vector3 homePosition;
        private Vector3 workPosition;
        private float decisionTimer;

        public void Initialize(NpcSaveData data, Vector3 home, Vector3 work)
        {
            npcId = data.id;
            displayName = data.displayName;
            role = data.role;
            homeId = data.homeId;
            workId = data.workId;
            money = data.money;
            state = string.IsNullOrEmpty(data.state) ? NpcState.Idle : (NpcState)System.Enum.Parse(typeof(NpcState), data.state);
            mood = string.IsNullOrEmpty(data.mood) ? NpcMood.Normal : (NpcMood)System.Enum.Parse(typeof(NpcMood), data.mood);
            homePosition = home;
            workPosition = work;
            transform.position = data.y == 0 ? home + Vector3.up : new Vector3(data.x, data.y, data.z);
        }

        private void Awake()
        {
            agent = gameObject.AddComponent<NavMeshAgent>();
            agent.speed = 2.6f;
        }

        private void Update()
        {
            decisionTimer -= Time.deltaTime;
            if (decisionTimer <= 0f)
            {
                decisionTimer = Random.Range(4f, 9f);
                PickRoutineStep();
            }

            if (!agent.isOnNavMesh)
            {
                transform.position += new Vector3(Mathf.Sin(Time.time + transform.GetInstanceID()), 0, Mathf.Cos(Time.time)) * Time.deltaTime;
            }
        }

        public NpcSaveData ToSaveData()
        {
            return new NpcSaveData { id = npcId, displayName = displayName, role = role, homeId = homeId, workId = workId, state = state.ToString(), mood = mood.ToString(), money = money, x = transform.position.x, y = transform.position.y, z = transform.position.z };
        }

        public string Talk()
        {
            return $"{displayName} ({role}): {dialogue}";
        }

        private void PickRoutineStep()
        {
            int hour = Mathf.FloorToInt((Time.time / 30f) % 24f);
            Vector3 target;
            if (hour < 7 || hour > 21)
            {
                state = NpcState.Sleep;
                mood = NpcMood.Tired;
                target = homePosition;
            }
            else if (hour < 16)
            {
                state = NpcState.Work;
                mood = NpcMood.Normal;
                target = workPosition;
            }
            else if (hour < 19)
            {
                state = NpcState.Shop;
                target = new Vector3(8, 8, -38);
            }
            else
            {
                state = NpcState.Travel;
                mood = NpcMood.Happy;
                target = homePosition;
            }

            if (agent.isOnNavMesh) agent.SetDestination(target);
        }
    }
}
