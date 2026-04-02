using UnityEngine;

/// <summary>
/// Вешается на триггер-зону двери.
/// При входе моба — открывает дверь, при выходе — закрывает.
/// </summary>
public class DoorTrigger : MonoBehaviour
{
    [SerializeField] private Door _door;
    [SerializeField] private string _mobTag = "Mob";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(_mobTag))
            _door.Open();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(_mobTag))
            _door.Close();
    }
}