using UnityEngine;

public class OlharParaJogador : MonoBehaviour
{
    void Update()
    {
        // Faz a imagem 2D girar e ficar sempre de frente para a câmera principal
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }
}