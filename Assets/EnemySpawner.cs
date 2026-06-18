using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Configurações de Spawn")]
    public GameObject enemyPrefab;
    public int maxEnemies = 5;       
    public float spawnRadius = 30f;  
    public float respawnDelay = 3f;  

    private void Start()
    {
        Debug.Log("[SPAWNER] Iniciando o jogo... Tentando spawnar os primeiros inimigos.");
        for (int i = 0; i < maxEnemies; i++)
        {
            SpawnEnemy();
        }
    }

    public void OnEnemyDied()
    {
        Debug.Log("[SPAWNER] Inimigo abatido! Contando " + respawnDelay + " segundos para o próximo...");
        Invoke(nameof(SpawnEnemy), respawnDelay);
    }

    private void SpawnEnemy()
    {
        // Trava 1: Verifica se o Prefab está lá
        if (enemyPrefab == null)
        {
            Debug.LogError("[SPAWNER] ERRO CRÍTICO: O 'Enemy Prefab' sumiu do Inspector do Spawner!");
            return;
        }

        // Escolhe um ponto aleatório
        Vector3 randomDirection = Random.insideUnitSphere * spawnRadius;
        randomDirection += transform.position;
        NavMeshHit hit;

        // Trava 2: Tenta achar um ponto válido no chão (NavMesh)
        // Usamos NavMesh.AllAreas para garantir que ele procure em todos os tipos de chão navegável
        if (NavMesh.SamplePosition(randomDirection, out hit, spawnRadius, NavMesh.AllAreas))
        {
            Instantiate(enemyPrefab, hit.position, Quaternion.identity);
            Debug.Log("[SPAWNER] SUCESSO! Inimigo criado na posição: " + hit.position);
        }
        else
        {
            Debug.LogWarning("[SPAWNER] AVISO: Não achei um ponto válido no NavMesh. Tentando de novo...");
            Invoke(nameof(SpawnEnemy), 0.5f); // Tenta de novo meio segundo depois
        }
    }
}