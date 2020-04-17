using System.Collections;
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
    private FlocksHandler flocksHandler;
    
    // Start is called before the first frame update
    void Start()
    {
        mesh = GameObject.Find("Grid").GetComponent<NavigationMesh>();
        flocksHandler = GameObject.Find("Grid").GetComponent<FlocksHandler>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 targetPos = GetTargetPosition();

        Vector2 seekForce = Seek(targetPos);
        Vector2 seperationForce = Seperation();
        Vector2 otherFlockAvoid = SeperationOtherFlocks();
        Vector2 cohesionForce = Cohesion();
        Vector2 alignmentForce = Alignment();

        Vector2 totalForce = seekForce + seperationForce + otherFlockAvoid * 5.0f + cohesionForce * 0.1f + alignmentForce;

        velocity = velocity + totalForce * Time.deltaTime;

        if (velocity.magnitude > maxSpeed)
            velocity = velocity * (maxSpeed / velocity.magnitude);

        Vector2 dir = velocity.normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        transform.Translate(velocity * Time.deltaTime, Space.World);
    }

    private Vector2 GetTargetPosition()
    {
        Vector2 result = transform.position;
        /*int row = -1, col = -1;

        mesh.GetRowAndColFromGridSpacePosition(ref row, ref col, transform.position);

        if (flock.crowdFlowField != null && row >= 0 && row < mesh.NumRows && col >= 0 && col < mesh.NumCols)
            result = flock.crowdFlowField[row, col];

        return result;*/

        int[] flowField = flock.Flowfield;
        int currRegion;

        if(flowField != null && flowField.Length > 0)
        {
            currRegion = mesh.NavigationRegionIdFromGridSpacePosition(transform.position);

            // flowField[currRegion] is the region that we want to move towards FROM our
            // current region. So if flowField[currRegion] = currRegion, then our goal is in
            // the same region as we are. Thus we can move directly towards it.
            if (currRegion != -1 && flowField[currRegion] != currRegion)
                result = mesh.GetRegionCenterInGridSpace(flowField[currRegion]);
            else
                result = target.transform.position;
        }

        return result;
    }

    private Vector2 Seek(Vector2 targetPos)
    {
        Vector2 desiredVelocity = targetPos - (Vector2)transform.position;
        desiredVelocity = desiredVelocity.normalized * maxSpeed;

        Vector2 force = desiredVelocity - velocity;

        return force * (maxForce / maxSpeed);
    }

    private Vector2 Seperation()
    {
        Vector2 myPos2D = transform.position;
        Vector2 totalForce = Vector2.zero;

        List<Vector2> neighborPositions = flock.GetNeighborPositions(id, transform.position, 10.0f);
        //List<Vector2> neighborPositions = flocksHandler.GetNeighborPositions(id, flock.id, transform.position, 10.0f);

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

    private Vector2 SeperationOtherFlocks()
    {
        Vector2 myPos2D = transform.position;
        Vector2 totalForce = Vector2.zero;

        List<Vector2> neighborPositions = flocksHandler.GetNeighborPositionsOtherFlocks(id, flock.id, transform.position, 10.0f);
        //List<Vector2> neighborPositions = flocksHandler.GetNeighborPositions(id, flock.id, transform.position, 10.0f);

        if (neighborPositions.Count < 1)
            return Vector2.zero;

        foreach (Vector2 pos in neighborPositions)
        {
            Vector2 pushForce = myPos2D - pos;

            if (pushForce.magnitude < seperationDist && pushForce.magnitude != 0.0f)
            {
                totalForce += pushForce / seperationDist;
            }
        }

        totalForce = totalForce / neighborPositions.Count;

        return totalForce * maxForce;
    }

    private Vector2 Cohesion()
    {
        Vector2 myPos2D = transform.position;
        Vector2 centerOfMass = myPos2D;
        List<Vector2> neighborPos = flock.GetNeighborPositions(id, myPos2D, 10.0f);

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

    private Vector2 Alignment()
    {
        List<Vector2> neighborPos = flock.GetNeighborPositions(id, transform.position, 10.0f);
        List<Vector2> neighborForwards = flock.GetNeighborVelocity(id, transform.position, 10.0f);
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
