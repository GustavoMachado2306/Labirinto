using UnityEngine;
using System.Collections; // Necessário para criar o "temporizador" do piscar

public class EnemyHealth : MonoBehaviour
{
    [Header("Status do Inimigo")]
    public int maxHealth = 5; // Aumentei para 5 pra dar tempo de ver piscando!
    private int _currentHealth;

    // Variáveis para guardar as cores do inimigo
    private Renderer[] _renderers;
    private Color[] _coresOriginais;

    private void Start()
    {
        _currentHealth = maxHealth;

        // Pega todas as malhas (MeshRenderers) do corpo do inimigo
        _renderers = GetComponentsInChildren<Renderer>();
        _coresOriginais = new Color[_renderers.Length];

        // Salva a cor original de cada parte do corpo dele
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty("_Color"))
            {
                _coresOriginais[i] = _renderers[i].material.color;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;

        // Chama a nossa animaçãozinha de piscar
        StartCoroutine(PiscarDano());

        if (_currentHealth <= 0)
        {
            Die();
        }
    }

    // Função que cria o efeito de piscar
    private IEnumerator PiscarDano()
    {
        // 1. Pinta tudo de vermelho
        for (int i = 0; i < _renderers.Length; i++)
        {
            if (_renderers[i].material.HasProperty("_Color"))
            {
                _renderers[i].material.color = Color.red;
            }
        }

        // 2. Espera 0.1 segundos (piscar rápido)
        yield return new WaitForSeconds(0.1f);

        // 3. Devolve as cores originais (se o inimigo ainda não tiver morrido)
        if (this != null) 
        {
            for (int i = 0; i < _renderers.Length; i++)
            {
                if (_renderers[i].material.HasProperty("_Color"))
                {
                    _renderers[i].material.color = _coresOriginais[i];
                }
            }
        }
    }

    private void Die()
    {
        if (ScoreManager.instance != null)
        {
            ScoreManager.instance.AddScore(10); 
        }

        EnemySpawner spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner != null)
        {
            spawner.OnEnemyDied();
        }

        Destroy(gameObject);
    }
}