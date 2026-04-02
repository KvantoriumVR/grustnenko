using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Анимация")]
    [SerializeField] private Animator _animator;
    [SerializeField] private string _openAnimation = "DoorOpen";
    [SerializeField] private string _closeAnimation = "DoorClose";

    private bool _isOpen;

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
    }

    public void Open()
    {
        if (_isOpen) return;
        _isOpen = true;
        _animator.Play(_openAnimation);
    }

    public void Close()
    {
        if (!_isOpen) return;
        _isOpen = false;
        _animator.Play(_closeAnimation);
    }

    public void Toggle()
    {
        if (_isOpen) Close();
        else Open();
    }
}