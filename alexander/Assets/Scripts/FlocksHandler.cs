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

    // Update is called once per frame
    void Update()
    {
        
    }
}
