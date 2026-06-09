using BlockHaven.Player;
using UnityEngine;

namespace BlockHaven.Economy
{
    public class JobSystem : MonoBehaviour
    {
        public string currentJob = "Citizen";
        private float shiftTimer;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) currentJob = "Police";
            if (Input.GetKeyDown(KeyCode.F2)) currentJob = "Doctor";
            if (Input.GetKeyDown(KeyCode.F3)) currentJob = "Firefighter";
            if (Input.GetKeyDown(KeyCode.F4)) currentJob = "Delivery";
            if (currentJob == "Citizen") return;
            shiftTimer += Time.deltaTime;
            if (shiftTimer > 60f)
            {
                shiftTimer = 0f;
                EconomyManager.Instance.PaySalary(GetComponent<BlockHavenPlayerController>(), currentJob);
            }
        }
    }
}
