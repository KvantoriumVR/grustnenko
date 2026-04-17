using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class HandInteraction : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float _interactRange = 2.5f;

    [Header("Вибрация (можно отключить)")]
    [SerializeField] private bool _enableHaptic = true;      // ← Снять в инспекторе галочку, чтобы отключить вибрацию, если не фурычит
    [SerializeField] private ushort _hapticStrength = 500;   // Сила вибрации (100-3000)

    private Hand _hand;
    private SteamVR_Action_Boolean _interactAction;

    private void Awake()
    {
        _hand = GetComponent<Hand>();

        _interactAction = SteamVR_Input.GetBooleanAction("InteractUI");
        if (_interactAction == null)
            _interactAction = SteamVR_Input.GetBooleanAction("default", "InteractUI");
    }

    private void Update()
    {
        if (_hand == null || !_hand.isActive) return;
        if (_interactAction == null) return;

        if (_interactAction.GetStateDown(_hand.handType))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, _interactRange))
        {
            Door door = hit.collider.GetComponentInParent<Door>();
            if (door != null)
            {
                door.Toggle();

                // Вибрация — можно закомментировать или отключить через _enableHaptic
                if (_enableHaptic)
                {
                    Vibrate();
                }
            }
        }
    }

    private void Vibrate()
    {
        if (_hand != null)
        {
            // Вроде правильный способ вибрации, если что - можно отключить
            _hand.TriggerHapticPulse(_hapticStrength);
        }
    }
}