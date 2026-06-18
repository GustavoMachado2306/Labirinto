using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    // O Singleton: cria uma referência global e única desse script
    public static ScoreManager instance;

    [Header("Configurações")]
    public Text scoreText; // Arraste seu texto de UI aqui no Inspector!
    private int _score = 0;

    private void Awake()
    {
        // Garante que só existe um ScoreManager na cena
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        UpdateScoreText();
    }

    // Função que os inimigos vão chamar quando morrerem
    public void AddScore(int points)
    {
        _score += points;
        UpdateScoreText();
        Debug.Log($"[SCORE] Pontos ganhos! Pontuação atual: {_score}");
    }

    private void UpdateScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = "Pontos: " + _score;
        }
    }
}