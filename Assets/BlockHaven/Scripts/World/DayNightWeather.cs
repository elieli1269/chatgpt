using UnityEngine;

namespace BlockHaven.World
{
    public class DayNightWeather : MonoBehaviour
    {
        public Light sun;
        public float dayLengthSeconds = 900f;
        private float weatherTimer;
        private Color clearAmbient = new Color(0.45f, 0.50f, 0.58f);
        private Color rainyAmbient = new Color(0.22f, 0.25f, 0.30f);
        private bool rainy;

        private void Start()
        {
            if (sun == null) sun = FindObjectOfType<Light>();
        }

        private void Update()
        {
            float normalizedDay = (float)((WorldManager.Instance != null ? WorldManager.Instance.WorldTime : Time.time) % dayLengthSeconds) / dayLengthSeconds;
            if (sun != null)
            {
                sun.transform.rotation = Quaternion.Euler(normalizedDay * 360f - 90f, -30f, 0);
                sun.intensity = Mathf.Lerp(0.12f, 1.15f, Mathf.Clamp01(Mathf.Sin(normalizedDay * Mathf.PI)));
            }

            weatherTimer += Time.deltaTime;
            if (weatherTimer > 180f)
            {
                weatherTimer = 0f;
                rainy = Random.value > 0.65f;
            }
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, rainy ? rainyAmbient : clearAmbient, Time.deltaTime * 0.2f);
        }
    }
}
