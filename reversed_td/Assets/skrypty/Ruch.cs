using UnityEngine;
using UnityEngine.AI; 

public class FasolkaRuch : MonoBehaviour
{
    [Header("dok¹d (do k¹towni)")]
    public Transform cel;

    private NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (cel != null)
        {
            agent.SetDestination(cel.position);
        }
    }
}