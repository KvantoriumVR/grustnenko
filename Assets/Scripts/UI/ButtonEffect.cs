using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private Image buttonImage;
    [SerializeField] private Image glowImage;
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite pressedSprite;

    private Button _button;
    private bool _isPressed = false;

    private void Awake()
    {
        // Получаем компонент Button
        _button = GetComponent<Button>();

        if (glowImage != null)
            glowImage.raycastTarget = false;

        SetNormal();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Только если кнопка активна
        if (_button != null && !_button.interactable) return;

        if (glowImage != null)
            glowImage.enabled = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (glowImage != null)
            glowImage.enabled = false;

        // Если кнопка была нажата и мы вышли - возвращаем нормальный вид
        if (_isPressed)
        {
            SetNormal();
            _isPressed = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // Только если кнопка активна
        if (_button != null && !_button.interactable) return;

        _isPressed = true;

        if (buttonImage != null && pressedSprite != null)
            buttonImage.sprite = pressedSprite;

        if (glowImage != null)
            glowImage.enabled = false;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
        SetNormal();
    }

    private void SetNormal()
    {
        if (buttonImage != null && normalSprite != null)
            buttonImage.sprite = normalSprite;

        if (glowImage != null)
            glowImage.enabled = false;
    }
}