using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flock : MonoBehaviour
{
    public uint numAgents = 1;
    public GameObject agentType;
    public GameObject goal;

    private List<GameObject> flock = new List<GameObject>();

    private NavigationMesh mesh;

    public int[] Flowfield { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        Agent agentComponent = agentType.GetComponent<Agent>();
        agentComponent.target = goal;
        int spawnMag = 3;

        for(uint i = 0; i < numAgents; i++)
        {
            GameObject next = Instantiate(agentType);

            next.GetComponent<Agent>().target = goal;
            next.GetComponent<Agent>().id = (int)i;
            next.GetComponent<Agent>().flock = this;

            next.transform.position = transform.position + new Vector3(Random.Range(-spawnMag, spawnMag), Random.Range(-spawnMag, spawnMag), 0);

            flock.Add(next);
        }

        mesh = GameObject.Find("Grid").GetComponent<NavigationMesh>();
        Flowfield = null;
    }

    public List<Vector2> GetNeighborPositions(int id, Vector2 position, float radius)
    {
        List<Vector2> result = new List<Vector2>();
        Agent next;
        Vector2 nextPos;

        for(int i = 0; i < numAgents; i++)
        {
            next = flock[i].GetComponent<Agent>();
            nextPos = next.transform.position;

            if(next.id != id && (nextPos - position).magnitude <= radius)
            {
                result.Add(nextPos);
            }
        }

        return result;
    }

    public List<Vector2> GetNeighborVelocity(int id, Vector2 position, float radius)
    {
        List<Vector2> result = new List<Vector2>();
        Agent next;
        Vector2 nextPos;
        Vector2 nextForward;

        for(int i = 0; i < numAgents; i++)
        {
            next = flock[i].GetComponent<Agent>();
            nextPos = next.transform.position;
            nextForward = next.transform.forward;

            if(next.id != id && (nextPos - position).magnitude <= radius)
            {
                result.Add(nextForward);
            }
        }

        return result;
    }

    // Update is called once per frame
    void Update()
    {
        Flowfield = mesh.GetFlowfieldGraph(goal.transform.position);
    }
}
