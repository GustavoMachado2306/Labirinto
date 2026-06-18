using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Configuracoes")]
    [Tooltip("Tempo em segundos antes de reiniciar a cena apos morrer")]
    public float RestartDelay = 1.5f;

    private bool _isDead = false;

    public void Die()
    {
        if (_isDead) return;

        _isDead = true;
        Debug.Log("Jogador morreu! Reiniciando...");

        Invoke(nameof(RestartScene), RestartDelay);
    }

    private void RestartScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}