﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public int id;

    public GameObject target;
    public Flock flock;

    // Rate of acceleration.
    public float maxForce = 5.0f;
    // Movement in units/seconds.
    public float maxSpeed = 4.0f;
    // The amount of seperation between me and another unit in my flock
    public float seperationDist = 4.0f;
    // For each neigbhor that is within this distance, we want to stay with them.
    public float cohesionDist = 4.0f;

    private Vector2 velocity = Vector2.zero;

    private NavigationMesh mesh;
    
    // Start is called before the first frame update
    void Start()
    {
        mesh = GameObject.Find("Grid").GetComponent<NavigationMesh>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 seekForce = seek();
        Vector2 seperationForce = seperation();
        Vector2 cohesionForce = cohesion();
        Vector2 alignmentForce = alignment();

        Vector2 totalForce = seekForce + seperationForce + cohesionForce * 0.1f + alignmentForce;

        velocity = velocity + totalForce * Time.deltaTime;

        if (velocity.magnitude > maxSpeed)
            velocity = velocity * (maxSpeed / velocity.magnitude);

        Vector2 dir = velocity.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    // TODO: Have seek take a target vector
    private Vector2 seek()
    {
        Vector2 desiredVelocity = Vector2.zero;
        int[] flowField = flock.Flowfield;

        if(flowField != null && flowField.Length > 0)
        {
            int currRegion = mesh.NavigationRegionIdFromTilePosition(transform.position);
            if(currRegion != -1)
            {
                if(flowField[currRegion] != currRegion)
                {
                    Vector3 tempDesiredVel = mesh.GetRegionCenter(flowField[currRegion]);
                    tempDesiredVel -= transform.position;
                    desiredVelocity = new Vector2(tempDesiredVel.x, tempDesiredVel.y);
                }

                else
                    desiredVelocity = target.transform.position - transform.position;
            }
        }

        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        Vector2 force = desiredVelocity - velocity;

        return force * (maxForce / maxSpeed);
    }

    private Vector2 seperation()
    {
        Vector2 myPos2D = transform.position;
        Vector2 totalForce = Vector2.zero;

        List<Vector2> neighborPositions = flock.getNeighborPositions(id, transform.position, 10.0f);

        if (neighborPositions.Count < 1)
            return Vector2.zero;

        foreach(Vector2 pos in neighborPositions)
        {
            Vector2 pushForce = myPos2D - pos;

            if(pushForce.magnitude < seperationDist && pushForce.magnitude != 0.0f)
            {
                totalForce += pushForce / seperationDist;
            }
        }

        totalForce = totalForce / neighborPositions.Count;

        return totalForce * maxForce;
    }

    private Vector2 cohesion()
    {
        Vector2 myPos2D = transform.position;
        Vector2 centerOfMass = myPos2D;
        List<Vector2> neighborPos = flock.getNeighborPositions(id, myPos2D, 10.0f);

        if (neighborPos.Count <= 1)
            return Vector2.zero;

        foreach(Vector2 pos in neighborPos)
        {
            if((pos - myPos2D).magnitude <= cohesionDist)
            {
                centerOfMass += pos;
            }
        }

        centerOfMass /= neighborPos.Count;

        Vector2 desiredVelocity = centerOfMass - myPos2D;
        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        Vector2 force = desiredVelocity - velocity;

        return force * (maxForce / maxSpeed);
    }

    private Vector2 alignment()
    {
        List<Vector2> neighborPos = flock.getNeighborPositions(id, transform.position, 10.0f);
        List<Vector2> neighborForwards = flock.getNeighborVelocity(id, transform.position, 10.0f);
        Vector2 averageHeading = Vector2.zero;
        Vector2 myPos2d = transform.position;

        if (neighborPos.Count < 1)
            return Vector2.zero;

        for(int i = 0; i < neighborPos.Count; i++)
        {
            if((neighborPos[i] - myPos2d).magnitude <= cohesionDist)
            {
                averageHeading += neighborForwards[i];
            }
        }

        averageHeading /= neighborPos.Count;
        Vector2 desired = averageHeading * maxSpeed;
        Vector2 force = desired - velocity;

        return force * (maxForce / maxSpeed);
    }
}
