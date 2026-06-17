using UnityEngine;
using UnityEngine.AI; 

public class InimigoIA : MonoBehaviour
{
    public Transform jogador; 
    private NavMeshAgent agente; 

    void Start()
    {
        agente = GetComponent<NavMeshAgent>(); 
    }

    void Update()
    {
        if (jogador != null)
        {
            agente.SetDestination(jogador.position);
        }
    }
}