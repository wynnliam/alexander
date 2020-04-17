using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlocksHandler : MonoBehaviour
{
    // Initialize here so we are guaranteed to have
    // an accessible list once every Flock has their Start called.
    private List<Flock> flocks = new List<Flock>();

    // Start is called before the first frame update
    void Start()
    {
    }

    public void AddFlock(Flock flock)
    {
        flocks.Add(flock);

        Debug.Log(flock.id);
    }

    public int NumFlocks()
    {
        return flocks.Count;
    }

    public List<Flock> GetAllExcept(int exceptId)
    {
        Predicate<Flock> predicate = delegate (Flock f) { return f.id != exceptId; };

        return flocks.FindAll(predicate);
    }

    public Flock GetFlock(int flockID)
    {
        Predicate<Flock> predicate = delegate (Flock f) { return f.id == flockID; };

        return flocks.Find(predicate);
    }

    public List<Vector2> GetNeighborPositions(int agentId, int flockId, Vector2 position, float radius)
    {
        List<Vector2> result = new List<Vector2>();
        int useId;

        foreach(Flock flock in flocks)
        {
            useId = flock.id == flockId ? agentId : -1;
            result.AddRange(flock.GetNeighborPositions(useId, position, radius));
        }

        return result;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
