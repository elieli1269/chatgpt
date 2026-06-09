using BlockHaven.Player;
using UnityEngine;
using UnityEngine.UI;

namespace BlockHaven.UI
{
    public class BlockHavenUIManager : MonoBehaviour
    {
        private Text moneyText;
        private Text helpText;
        private BlockHavenPlayerController player;

        private void Start()
        {
            player = FindObjectOfType<BlockHavenPlayerController>();
            Canvas canvas = new GameObject("Roblox Style HUD").AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvas.gameObject.AddComponent<GraphicRaycaster>();
            moneyText = CreateText(canvas.transform, "Money", new Vector2(20, -20), 28, TextAnchor.UpperLeft);
            helpText = CreateText(canvas.transform, "Help", new Vector2(20, -64), 16, TextAnchor.UpperLeft);
            CreatePanel(canvas.transform, new Vector2(16, -16), new Vector2(260, 140));
        }

        private void Update()
        {
            if (player == null) return;
            moneyText.text = $"$ {player.Money}";
            helpText.text = "ZQSD/WASD: move | Shift: sprint | Click: break/place | E: vehicle | C: creative | F1-F4 jobs";
        }

        private Text CreateText(Transform parent, string name, Vector2 pos, int size, TextAnchor anchor)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            Text text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = anchor;
            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(900, 40);
            return text;
        }

        private void CreatePanel(Transform parent, Vector2 pos, Vector2 size)
        {
            GameObject panel = new GameObject("Rounded Blue Panel Placeholder");
            panel.transform.SetParent(parent);
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.05f, 0.28f, 0.72f, 0.72f);
            RectTransform rect = image.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            panel.transform.SetAsFirstSibling();
        }
    }
}
