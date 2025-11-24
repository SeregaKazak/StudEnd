using UnityEngine;
using UnityEngine.UI;

public class ZachetkaInteraction : MonoBehaviour
{
    [Header("Вставь сюда СПРАЙТ зачётки")]
    public Sprite zachetkaSprite;

    public KeyCode interactKey = KeyCode.E;

    private Canvas canvas;
    private Image background;
    private Image zachetkaImage;

    private bool isPlayerLooking = false;
    private bool isOpen = false;

    void Start()
    {
        // Canvas
        GameObject canvasObj = new GameObject("ZachetkaCanvas");
        canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Полупрозрачный фон
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform);
        background = bgObj.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.45f);

        RectTransform bgRect = background.rectTransform;
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Картинка зачётки
        GameObject imgObj = new GameObject("ZachetkaImage");
        imgObj.transform.SetParent(canvasObj.transform);
        zachetkaImage = imgObj.AddComponent<Image>();
        zachetkaImage.sprite = zachetkaSprite;
        zachetkaImage.preserveAspect = true;

        RectTransform imgRect = zachetkaImage.rectTransform;
        imgRect.sizeDelta = new Vector2(600, 400); // УВЕЛИЧИЛ
        imgRect.anchoredPosition = Vector2.zero;

        // Скрываем при старте
        background.gameObject.SetActive(false);
        zachetkaImage.gameObject.SetActive(false);
    }

    void Update()
    {
        // Открытие
        if (isPlayerLooking && Input.GetKeyDown(interactKey))
        {
            if (!isOpen)
                OpenZachetka();
        }

        // Закрытие на ESC
        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseZachetka();
        }
    }

    private void OpenZachetka()
    {
        background.gameObject.SetActive(true);
        zachetkaImage.gameObject.SetActive(true);
        isOpen = true;
    }

    private void CloseZachetka()
    {
        background.gameObject.SetActive(false);
        zachetkaImage.gameObject.SetActive(false);
        isOpen = false;
    }

    private void OnMouseEnter()
    {
        isPlayerLooking = true;
    }

    private void OnMouseExit()
    {
        isPlayerLooking = false;
    }
}
