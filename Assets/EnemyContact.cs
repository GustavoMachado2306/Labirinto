using UnityEngine;

public class EnemyContact : MonoBehaviour
{
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Detecta quando o CharacterController do player bate no inimigo
        if (hit.gameObject.CompareTag("Enemy"))
        {
            PlayerHealth health = hit.gameObject.GetComponentInParent<PlayerHealth>();
            if (health == null)
                health = FindObjectOfType<PlayerHealth>();

            if (health != null)
                health.Die();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Detecta quando o inimigo encosta no player pelo trigger
        if (other.CompareTag("Player"))
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null)
                health = other.GetComponentInParent<PlayerHealth>();

            if (health != null)
                health.Die();
        }
    }
}