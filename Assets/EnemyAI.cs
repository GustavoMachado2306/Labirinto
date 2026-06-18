using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement; // Necessário para reiniciar a fase

public class EnemyAI : MonoBehaviour
{
    [Header("Configuracoes de Perseguicao")]
    [Tooltip("Distancia que o inimigo comeca a perseguir o player")]
    public float DetectionRange = 15f;
    [Tooltip("Distancia para o inimigo te matar (tamanho do braco dele)")]
    public float KillDistance = 1.5f; // NOVO: A distancia do "Game Over"
    [Tooltip("Velocidade do inimigo quando esta cacando o player")]
    public float MoveSpeed = 3.5f;

    [Header("Configuracoes de Patrulha")]
    [Tooltip("Velocidade do inimigo quando esta apenas vagando")]
    public float PatrolSpeed = 2f;
    [Tooltip("Distancia maxima que ele anda procurando um novo ponto")]
    public float WanderRadius = 10f;
    [Tooltip("De quantos em quantos segundos ele escolhe um novo caminho")]
    public float WanderTimer = 4f;

    private NavMeshAgent _agent;
    private Transform _player;
    private float _timer;

    void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _timer = WanderTimer;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
        else
            Debug.LogWarning("Player nao encontrado! Coloca a tag 'Player' no PlayerCapsule.");
    }

    void Update()
    {
        if (_player == null) return;

        // Calcula a distancia exata entre o inimigo e voce
        float distance = Vector3.Distance(transform.position, _player.position);

        // =========================================================
        // NOVO: CHECAGEM DE MORTE (MATH-BASED)
        // =========================================================
        if (distance <= KillDistance)
        {
            Debug.Log("[GAME OVER] O inimigo te pegou! Reiniciando a fase...");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return; // Para o codigo aqui para nao tentar andar mais
        }

        // O inimigo esta perto? CAÇAR!
        if (distance <= DetectionRange)
        {
            _agent.speed = MoveSpeed; 
            _agent.SetDestination(_player.position);
        }
        // O inimigo esta longe? PATRULHAR!
        else
        {
            _agent.speed = PatrolSpeed; 
            _timer += Time.deltaTime;

            if (_timer >= WanderTimer)
            {
                Vector3 novaPosicao = PontoAleatorioNoChao(transform.position, WanderRadius, -1);
                _agent.SetDestination(novaPosicao);
                _timer = 0; 
            }
        }
    }

    private Vector3 PontoAleatorioNoChao(Vector3 origem, float distancia, int layerMask)
    {
        Vector3 direcaoAleatoria = Random.insideUnitSphere * distancia;
        direcaoAleatoria += origem;
        
        NavMeshHit navHit;
        if (NavMesh.SamplePosition(direcaoAleatoria, out navHit, distancia, layerMask))
        {
            return navHit.position;
        }
        
        return origem; 
    }
}