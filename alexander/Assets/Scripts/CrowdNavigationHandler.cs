using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrowdNavigationHandler : MonoBehaviour
{
    private FlocksHandler flocks;
    private NavigationMesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        flocks = GetComponent<FlocksHandler>();
        mesh = GetComponent<NavigationMesh>();

        if (flocks != null && mesh != null)
            Debug.Log("Got the crowd components");
        else
            Debug.Log("Barf!");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
