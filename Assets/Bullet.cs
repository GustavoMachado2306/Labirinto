using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Configurações")]
    public float speed = 20f;
    public float lifeTime = 3f;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.isKinematic = false; // Garante que a física está ativa
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);
        
        // Usando .velocity para garantir compatibilidade com qualquer versão do Unity
        _rb.linearVelocity = transform.forward * speed; 
    }

    // Se o Inspector estiver configurado como Is Trigger, o Unity chama isso:
    private void OnTriggerEnter(Collider other)
    {
        ProcessarImpacto(other.gameObject);
    }

    // Se o Inspector NÃO estiver como Trigger (colisão sólida), o Unity chama isso:
    private void OnCollisionEnter(Collision collision)
    {
        ProcessarImpacto(collision.gameObject);
    }

    // Nossa função central que decide o que fazer independente de como bateu
    private void ProcessarImpacto(GameObject objetoAtingido)
    {
        // NOVO: Ignora o jogador E também ignora outras balas!
        // Se o objeto atingido tiver o script "Bullet", significa que é outra bala.
        if (objetoAtingido.CompareTag("Player") || objetoAtingido.GetComponent<Bullet>() != null)
        {
            return; 
        }

        if (objetoAtingido.CompareTag("Enemy"))
        {
            EnemyHealth enemyHealth = objetoAtingido.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
            {
                enemyHealth = objetoAtingido.GetComponentInChildren<EnemyHealth>();
            }

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(1);
            }
            Destroy(gameObject); 
        }
        else if (objetoAtingido.layer != LayerMask.NameToLayer("Ignore Raycast"))
        {
            Destroy(gameObject); 
        }
    }
}