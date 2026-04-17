using UnityEngine;

/// <summary>
/// Взаимодействие игрока с объектами.
/// Для теста без VR использует клавишу E.
/// В VR вызывается TryInteract() из контроллера.
/// </summary>
public class PlayerInteract : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Дальность взаимодействия (метры)")]
    [SerializeField] private float _interactRange = 2.5f;

    [Tooltip("Камера игрока (откуда пускается луч)")]
    [SerializeField] private Camera _playerCamera;

    private void Awake()
    {
        _playerCamera = GetComponentInChildren<Camera>();
    }

    private void Update()
    {
        // Клавиша E (для теста без VR)
        if (Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, _interactRange))
            {
                Door door = hit.collider.GetComponentInParent<Door>();
                if (door != null) door.Toggle();
            }
        }
    }

    /// <summary>
    /// Метод для вызова из VR-контроллера
    /// </summary>
    public void TryInteract()
    {
        Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, _interactRange))
        {
            Door door = hit.collider.GetComponentInParent<Door>();
            if (door != null) door.Toggle();
        }
    }
}