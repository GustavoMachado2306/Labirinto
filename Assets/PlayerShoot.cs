using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerShoot : MonoBehaviour
{
    [Header("Referências")]
    public GameObject bulletPrefab;
    public Transform firePoint;

    [Header("Metralhadora (Botão Esquerdo)")]
    public float fireRate = 0.1f; 
    private float _nextFireTime = 0f;

    [Header("Escopeta (Botão Direito)")]
    public float shotgunFireRate = 1.0f; 
    public int pelletsCount = 8;         
    public float spreadAngle = 8f;       
    private float _nextShotgunTime = 0f;

    [Header("Aimbot Leve (Magnetismo)")]
    [Tooltip("Liga ou desliga a ajuda de mira")]
    public bool useAimAssist = true;
    [Tooltip("O 'tamanho' do tubo invisível. Maior = mais fácil de acertar")]
    public float assistRadius = 1.5f; 
    [Tooltip("Qual a distância máxima que o aimbot funciona")]
    public float assistDistance = 50f;

    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (firePoint == null)
        {
            firePoint = transform.Find("PlayerCameraRoot/FirePoint");
            if (firePoint == null)
            {
                GameObject gObj = GameObject.Find("FirePoint");
                if (gObj != null) firePoint = gObj.transform;
            }
        }
    }

    private void Update()
    {
        if (Mouse.current.leftButton.isPressed && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + fireRate;
            Shoot();
        }

        if (Mouse.current.rightButton.wasPressedThisFrame && Time.time >= _nextShotgunTime)
        {
            _nextShotgunTime = Time.time + shotgunFireRate;
            ShootShotgun();
        }
    }

    // =======================================================
    // A MÁGICA DO AIMBOT ACONTECE AQUI
    // =======================================================
    private Quaternion GetAimRotation()
    {
        // Se a mira assistida estiver desligada, atira reto normalmente
        Quaternion defaultRotation = _mainCamera.transform.rotation;
        if (!useAimAssist) return defaultRotation;

        // Cria um raio saindo do centro da câmera
        Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
        
        // Dispara um "tubo grosso" (SphereCastAll) para achar tudo que está perto da mira
        RaycastHit[] hits = Physics.SphereCastAll(ray, assistRadius, assistDistance);

        float closestDistance = float.MaxValue;
        Collider bestTarget = null;

        // Procura nos objetos atingidos se algum deles é o inimigo
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.CompareTag("Enemy"))
            {
                // Pega o inimigo que estiver mais perto de você
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    bestTarget = hit.collider;
                }
            }
        }

        // Se encontrou um inimigo "raspando" na sua mira
        if (bestTarget != null)
        {
            // Calcula a direção exata do cano da arma (FirePoint) até o CENTRO do inimigo
            Vector3 directionToEnemy = bestTarget.bounds.center - firePoint.position;
            return Quaternion.LookRotation(directionToEnemy);
        }

        // Se não tinha inimigo perto da mira, atira reto
        return defaultRotation;
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null || _mainCamera == null) return;

        // Pega a rotação (Reta ou com Aimbot)
        Quaternion rotacaoTiro = GetAimRotation();

        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, rotacaoTiro);
        IgnorarColisaoComPlayer(bullet);
    }

    private void ShootShotgun()
    {
        if (bulletPrefab == null || firePoint == null || _mainCamera == null) return;

        // Pega a rotação (Reta ou com Aimbot focado no alvo)
        Quaternion rotacaoBase = GetAimRotation();

        for (int i = 0; i < pelletsCount; i++)
        {
            float randomX = Random.Range(-spreadAngle, spreadAngle);
            float randomY = Random.Range(-spreadAngle, spreadAngle);
            
            // Soma a bagunça com a rotação (que pode estar com o Aimbot ativado)
            Quaternion rotacaoEspalhada = rotacaoBase * Quaternion.Euler(randomX, randomY, 0);

            GameObject pellet = Instantiate(bulletPrefab, firePoint.position, rotacaoEspalhada);
            IgnorarColisaoComPlayer(pellet);
        }
    }

    private void IgnorarColisaoComPlayer(GameObject projetil)
    {
        Collider playerCollider = GetComponent<Collider>();
        Collider bulletCollider = projetil.GetComponent<Collider>();

        if (playerCollider != null && bulletCollider != null)
        {
            Physics.IgnoreCollision(playerCollider, bulletCollider);
        }
    }
}