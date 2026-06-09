using System.Collections.Generic;
using BlockHaven.Blocks;
using BlockHaven.Save;
using BlockHaven.Vehicles;
using BlockHaven.World;
using UnityEngine;

namespace BlockHaven.Player
{
    [RequireComponent(typeof(CharacterController))]
    public class BlockHavenPlayerController : MonoBehaviour
    {
        public Camera playerCamera;
        public float walkSpeed = 5f;
        public float sprintSpeed = 8f;
        public float jumpForce = 7f;
        public float lookSensitivity = 2.2f;
        public float reach = 6f;
        public bool creativeMode = true;
        public int Money { get; private set; } = 2000;
        public Dictionary<BlockType, int> Inventory { get; } = new Dictionary<BlockType, int>();

        private CharacterController controller;
        private float verticalVelocity;
        private float pitch;
        private VehicleController currentVehicle;
        private float saveTimer;
        private BlockType selectedBlock = BlockType.Planks;

        private void Awake()
        {
            tag = "Player";
            controller = GetComponent<CharacterController>();
            if (playerCamera == null)
            {
                GameObject cameraObject = new GameObject("FPS Camera");
                cameraObject.transform.SetParent(transform);
                cameraObject.transform.localPosition = new Vector3(0, 1.65f, 0);
                playerCamera = cameraObject.AddComponent<Camera>();
                cameraObject.AddComponent<AudioListener>();
            }
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void Start()
        {
            LoadPlayer();
        }

        private void Update()
        {
            if (currentVehicle != null)
            {
                if (Input.GetKeyDown(KeyCode.E)) ExitVehicle();
                return;
            }

            Look();
            Move();
            HandleBlockInput();
            if (Input.GetKeyDown(KeyCode.C)) creativeMode = !creativeMode;
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedBlock = BlockType.Dirt;
            if (Input.GetKeyDown(KeyCode.Alpha2)) selectedBlock = BlockType.Stone;
            if (Input.GetKeyDown(KeyCode.Alpha3)) selectedBlock = BlockType.Wood;
            if (Input.GetKeyDown(KeyCode.Alpha4)) selectedBlock = BlockType.Planks;
            if (Input.GetKeyDown(KeyCode.E)) TryEnterVehicle();

            saveTimer += Time.deltaTime;
            if (saveTimer > 10f)
            {
                saveTimer = 0f;
                SavePlayer();
            }
        }

        public void AddMoney(int amount)
        {
            Money = Mathf.Max(0, Money + amount);
        }

        public bool SpendMoney(int amount)
        {
            if (Money < amount) return false;
            Money -= amount;
            return true;
        }

        private void Look()
        {
            float yaw = Input.GetAxis("Mouse X") * lookSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * lookSensitivity;
            pitch = Mathf.Clamp(pitch, -80f, 80f);
            transform.Rotate(Vector3.up * yaw);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
        }

        private void Move()
        {
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
            Vector3 input = transform.right * Input.GetAxis("Horizontal") + transform.forward * Input.GetAxis("Vertical");
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -1f;
            if (controller.isGrounded && Input.GetButtonDown("Jump")) verticalVelocity = jumpForce;
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            controller.Move((input * speed + Vector3.up * verticalVelocity) * Time.deltaTime);
        }

        private void HandleBlockInput()
        {
            if (!Physics.Raycast(playerCamera.transform.position, playerCamera.transform.forward, out RaycastHit hit, reach)) return;
            Vector3Int blockPos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
            Vector3Int placePos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.01f);
            if (Input.GetMouseButtonDown(0) && WorldManager.Instance.TryBreakBlock(blockPos))
            {
                if (!Inventory.ContainsKey(selectedBlock)) Inventory[selectedBlock] = 0;
                Inventory[selectedBlock]++;
            }
            if (Input.GetMouseButtonDown(1) && (creativeMode || Consume(selectedBlock)))
            {
                WorldManager.Instance.SetBlock(placePos, selectedBlock);
            }
        }

        private bool Consume(BlockType block)
        {
            if (!Inventory.TryGetValue(block, out int amount) || amount <= 0) return false;
            Inventory[block] = amount - 1;
            return true;
        }

        private void TryEnterVehicle()
        {
            Collider[] hits = Physics.OverlapSphere(transform.position, 3f);
            foreach (Collider hit in hits)
            {
                VehicleController vehicle = hit.GetComponentInParent<VehicleController>();
                if (vehicle != null)
                {
                    currentVehicle = vehicle;
                    vehicle.Enter(this);
                    controller.enabled = false;
                    return;
                }
            }
        }

        private void ExitVehicle()
        {
            transform.position = currentVehicle.transform.position + currentVehicle.transform.right * 2f + Vector3.up;
            controller.enabled = true;
            currentVehicle.Exit();
            currentVehicle = null;
        }

        private void LoadPlayer()
        {
            PlayerSaveData data = SaveManager.Instance.LoadOrDefault(SaveManager.Instance.PlayerPath, new PlayerSaveData());
            transform.position = data.py == 0f ? new Vector3(-20, 12, -16) : new Vector3(data.px, data.py, data.pz);
            transform.rotation = Quaternion.Euler(0, data.yaw, 0);
            Money = data.money <= 0 ? 2000 : data.money;
            Inventory.Clear();
            foreach (InventorySlotData slot in data.inventory)
            {
                Inventory[slot.blockType] = slot.amount;
            }
        }

        public void SavePlayer()
        {
            PlayerSaveData data = new PlayerSaveData { px = transform.position.x, py = transform.position.y, pz = transform.position.z, yaw = transform.eulerAngles.y, money = Money };
            foreach (KeyValuePair<BlockType, int> slot in Inventory)
            {
                data.inventory.Add(new InventorySlotData { blockType = slot.Key, amount = slot.Value });
            }
            SaveManager.Instance.Save(SaveManager.Instance.PlayerPath, data);
        }

        private void OnApplicationQuit()
        {
            SavePlayer();
        }
    }
}
