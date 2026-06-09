using BlockHaven.Player;
using BlockHaven.Save;
using UnityEngine;

namespace BlockHaven.Vehicles
{
    [RequireComponent(typeof(Rigidbody), typeof(BoxCollider))]
    public class VehicleController : MonoBehaviour
    {
        public string vehicleId = "starter_car";
        public string vehicleType = "Car";
        public float motorForce = 900f;
        public float turnForce = 90f;
        public bool npcTraffic;

        private Rigidbody body;
        private bool occupied;
        private BlockHavenPlayerController driver;

        private void Start()
        {
            WorldSaveData world = SaveManager.Instance.LoadOrDefault(SaveManager.Instance.WorldPath, new WorldSaveData());
            foreach (VehicleSaveData saved in world.vehicles)
            {
                if (saved.id != vehicleId) continue;
                transform.position = new Vector3(saved.x, saved.y, saved.z);
                transform.rotation = Quaternion.Euler(0, saved.yaw, 0);
                break;
            }
        }

        public VehicleSaveData ToSaveData()
        {
            return new VehicleSaveData { id = vehicleId, owner = string.Empty, type = vehicleType, x = transform.position.x, y = transform.position.y, z = transform.position.z, yaw = transform.eulerAngles.y };
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.mass = vehicleType == "Moto" ? 350f : 900f;
            body.centerOfMass = new Vector3(0, -0.45f, 0);
            GetComponent<BoxCollider>().size = vehicleType == "Moto" ? new Vector3(0.9f, 1f, 2.2f) : new Vector3(2f, 1.2f, 3.8f);
        }

        private void FixedUpdate()
        {
            if (!occupied && !npcTraffic) return;
            float throttle = occupied ? Input.GetAxis("Vertical") : 0.35f;
            float steer = occupied ? Input.GetAxis("Horizontal") : Mathf.Sin(Time.time * 0.4f) * 0.25f;
            body.AddForce(transform.forward * throttle * motorForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            body.MoveRotation(body.rotation * Quaternion.Euler(0, steer * turnForce * Time.fixedDeltaTime, 0));
        }

        public void Enter(BlockHavenPlayerController player)
        {
            occupied = true;
            driver = player;
            player.transform.SetParent(transform);
            player.transform.localPosition = Vector3.up * 1.2f;
        }

        public void Exit()
        {
            if (driver != null) driver.transform.SetParent(null);
            driver = null;
            occupied = false;
        }
    }
}
