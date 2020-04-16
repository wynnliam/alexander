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
    }

    // Returns a flow field of size [mesh.numRows, mesh.numColumns]
    // where it would be the flock's flow field, except that the regions
    // that have agents that aren't in my flock will account for crowds.
    /*
     * - Basically compute a density map of all agents not in my flock
     * - Then do Eikonal fill, starting with the goal, where the cost
     * - is determined by number of agents. We only do the tiles that
     * - have agents on them.
     */
    public Vector3[,] GetCrowdFlowfield(int flockId, Vector3 goal)
    {
        if (mesh == null || flocks == null || mesh.NumRows < 1 || mesh.NumCols < 1 ||
           mesh.NumRegions() < 1 || flocks.NumFlocks() <= 1)
            return null;

        // The number of agents at each tile.
        int[,] densityField = new int[mesh.NumRows, mesh.NumCols];
        // If regionHasCrowds[x] == true, then region x has at least
        // one agent in a flock that is not ours. Thus, we need to do
        // eikonal search on the tiles in that region.
        bool[] regionHasCrowds = new bool[mesh.NumRegions()];
        List<Flock> crowds = flocks.GetAllExcept(flockId);

        if (crowds.Count < 1)
            return null;

        // Step 1: Compute the density field and regionHasCrowds.
        foreach(Flock crowd in crowds)
        {
            List<Vector3> agentPositions = crowd.GetAllPositions();
            foreach(Vector3 pos in agentPositions)
            {
                int regionIndex = mesh.NavigationRegionIdFromGridSpacePosition(pos);
                int row = -1, col = -1;
                // If the regionIndex != -1, then the agent is standing at a valid region.
                // So we can safely compute the row and col of the agent and update the density
                // value accordingly.
                if(regionIndex != -1)
                {
                    regionHasCrowds[regionIndex] = true;
                    mesh.GetRowAndColFromGridSpacePosition(ref row, ref col, pos);
                    densityField[row, col] += 1;
                }
            }
        }

        // Step 2: Using the densityField and regionHasCrowds,
        // compute a flow field from the goal to every index.
        // TODO: Finish me!

        return null;
    }

    // Update is called once per frame
    void Update()
    {
    }
}
