using UnityEngine;

public class Crosshair : MonoBehaviour
{
    [Header("Configuracoes da Mira")]
    [Tooltip("Tamanho de cada linha da mira em pixels")]
    public float LineLength = 10f;

    [Tooltip("Espessura das linhas")]
    public float LineWidth = 2f;

    [Tooltip("Espaco vazio no centro da mira")]
    public float CenterGap = 4f;

    [Tooltip("Cor da mira")]
    public Color CrosshairColor = Color.white;

    private Texture2D _dot;

    void Start()
    {
        _dot = new Texture2D(1, 1);
        _dot.SetPixel(0, 0, Color.white);
        _dot.Apply();
    }

    void OnGUI()
    {
        GUI.color = CrosshairColor;

        float cx = Screen.width / 2f;
        float cy = Screen.height / 2f;

        // Linha horizontal esquerda
        GUI.DrawTexture(new Rect(cx - CenterGap - LineLength, cy - LineWidth / 2f, LineLength, LineWidth), _dot);

        // Linha horizontal direita
        GUI.DrawTexture(new Rect(cx + CenterGap, cy - LineWidth / 2f, LineLength, LineWidth), _dot);

        // Linha vertical cima
        GUI.DrawTexture(new Rect(cx - LineWidth / 2f, cy - CenterGap - LineLength, LineWidth, LineLength), _dot);

        // Linha vertical baixo
        GUI.DrawTexture(new Rect(cx - LineWidth / 2f, cy + CenterGap, LineWidth, LineLength), _dot);
    }
}