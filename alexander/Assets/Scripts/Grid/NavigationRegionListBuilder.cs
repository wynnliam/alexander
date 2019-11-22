using System;
using System.Collections.Generic;
using UnityEngine;

public class NavigationRegionListBuilder
{
    public List<NavigationRegion> ConstructNavigationRegionList(int[,] gridMap, int numRow, int numColumn)
    {
        // List of regions created according to gridMap
        List<NavigationRegion> result = GenerateInitialNavigationRegions(gridMap, numRow, numColumn);
        // The key is the region to split. Value is the region to split by.
        List<KeyValuePair<int, int>> splitsToDo = new List<KeyValuePair<int, int>>();
        // Use this to give unique id's to each new region we add.
        int count = result.Count;

        // We assume there will always be splits to do.
        while(true)
        {
            // If a is adjacent to b, add a split that we want to do.
            foreach(NavigationRegion a in result)
            {
                foreach(NavigationRegion b in result)
                {
                    if (a == b)
                        continue;

                    if((a.RightOf(b) || a.LeftOf(b)) && (a.Row != b.Row || a.Height != b.Height))
                    {
                        if (a.Height > b.Height)
                            splitsToDo.Add(new KeyValuePair<int, int>(a.Id, b.Id));
                        else
                            splitsToDo.Add(new KeyValuePair<int, int>(b.Id, a.Id));
                    }

                    else if((a.Above(b) || a.Below(b)) && (a.Column != b.Column || a.Width != b.Width))
                    {
                        if (a.Width > b.Width)
                            splitsToDo.Add(new KeyValuePair<int, int>(a.Id, b.Id));
                        else
                            splitsToDo.Add(new KeyValuePair<int, int>(b.Id, a.Id));
                    }
                }
            }

            // Since there are no more splits to do, we exit the loop.
            if (splitsToDo.Count == 0)
                break;
            else
            {
                // Keep popping splits off the top of our splits to do list.
                // If both regions are still in the list, the split can be done (it is
                // possible we request a split to a region that was already split).
                // If the split is possible, we do it, remove the split region from
                // the list, and add the resulting components to the final list of regions.
                while(splitsToDo.Count > 0)
                {
                    KeyValuePair<int, int> split = splitsToDo[0];
                    splitsToDo.RemoveAt(0);

                    NavigationRegion toSplit = GetRegion(split.Key, result);
                    NavigationRegion splitBy = GetRegion(split.Value, result);

                    if(toSplit != null && splitBy != null)
                    {
                        List<NavigationRegion> splitResult;

                        if (toSplit.Above(splitBy) || toSplit.Below(splitBy))
                            splitResult = VerticalSplit(toSplit, splitBy);
                        else
                            splitResult = HorizontalSplit(toSplit, splitBy);

                        result.Remove(toSplit);
                        foreach(NavigationRegion component in splitResult)
                        {
                            component.Id = count;
                            count++;
                            result.Add(component);
                        }
                    }
                }
            }
        }

        // Fix the id's to be zero-based. This makes them much easier to manage when
        // building an adjacency matrix for them.
        for (int i = 0; i < result.Count; i++)
            result[i].Id = i;

        return result;
    }

    private NavigationRegion GetRegion(int id, List<NavigationRegion> regions)
    {
        NavigationRegion result = null;

        foreach(NavigationRegion r in regions)
        {
            if(r.Id == id)
            {
                result = r;
                break;
            }
        }

        return result;
    }

    private List<NavigationRegion> GenerateInitialNavigationRegions(int[,] gridMap, int numRow, int numColumn)
    {
        int numRegions = 0;
        List<NavigationRegion> result = new List<NavigationRegion>();

        for(int i = 0; i < numRow; i++)
        {
            // For each already existing navigation region, we increase its height.
            // If this means the region now contains a wall, we undo the height change.
            // Since we never create a region above an already existing one, we need
            // only to check for walls when expanding the height (we are guaranteed never
            // to overlap height-wise another region).
            foreach(NavigationRegion region in result)
            {
                region.Height += 1;

                if(region.ContainsWall(gridMap, numRow, numColumn))
                {
                    region.Height -= 1;
                }
            }

            // For each column given our current row, if the tile is not a wall nor
            // contained by another navigation region we place a new navigation region
            // at this point. We then continue to expand the width of the region
            // until we hit out of bounds, a wall, or another region. New regions
            // are guaranteed never to expand width-wise and overlap another region.
            for(int j = 0; j < numColumn; j++)
            {
                if(gridMap[i, j] == 0 && ContainingRegion(result, i, j) == -1)
                {
                    NavigationRegion region = new NavigationRegion(numRegions, i, j, 0, 1);
                    numRegions += 1;

                    while(j < numColumn && gridMap[i, j] == 0 && ContainingRegion(result, i, j) == -1)
                    {
                        j++;
                        region.Width++;
                    }

                    result.Add(region);
                }
            }
        }

        return result;
    }

    private int ContainingRegion(List<NavigationRegion> regions, int row, int column)
    {
        int result = -1;

        foreach(NavigationRegion region in regions)
        {
            if(region.Contains(row, column))
            {
                result = region.Id;
                break;
            }
        }

        return result;
    }

    private List<NavigationRegion> VerticalSplit(NavigationRegion toSplit, NavigationRegion splitBy)
    {
        List<NavigationRegion> result = new List<NavigationRegion>();

        // Case 1: splitBy is between the left and right edges of toSplit. This creates 3 sub-regions out
        // of toSplit.
        if(toSplit.Column < splitBy.Column && (splitBy.Column + splitBy.Width < toSplit.Column + toSplit.Width))
        {
            NavigationRegion region1, region2, region3;

            region1 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column, splitBy.Column - toSplit.Column, toSplit.Height);
            result.Add(region1);

            region2 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column + region1.Width, splitBy.Width, toSplit.Height);
            result.Add(region2);

            region3 = new NavigationRegion(toSplit.Id, toSplit.Row, region2.Column + splitBy.Width, toSplit.Width - (region1.Width + region2.Width), toSplit.Height);
            result.Add(region3);
        }

        // Case 2: splitBy shares the left edge with toSplit, but is strictly smaller in width. Creates
        // the second 2 regions from Case 1.
        else if(splitBy.Column == toSplit.Column && (splitBy.Column + splitBy.Width) < (toSplit.Column + toSplit.Width))
        {
            NavigationRegion region1, region2, region3;

            // Don't add region 1.
            region1 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column, splitBy.Column - toSplit.Column, toSplit.Height);

            region2 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column + region1.Width, splitBy.Width, toSplit.Height);
            result.Add(region2);

            region3 = new NavigationRegion(toSplit.Id, toSplit.Row, region2.Column + splitBy.Width, toSplit.Width - (region1.Width + region2.Width), toSplit.Height);
            result.Add(region3);
        }

        // Case 3: splitBy's right edge is the same as toSplit's right edge. However, splitBy is smaller, so it's left
        // edge is greater than toSplit's. Return regions 1 and 2 from case 1.
        else if(toSplit.Column < splitBy.Column && (splitBy.Column + splitBy.Width) == (toSplit.Column + toSplit.Width))
        {
            NavigationRegion region1, region2;
            region1 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column, splitBy.Column - toSplit.Column, toSplit.Height);
            result.Add(region1);

            region2 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column + region1.Width, splitBy.Width, toSplit.Height);
            result.Add(region2);
        }

        // Case 4: splitBy is slightly to the left of toSplit. However, splitBy's right edge is between the left and right edges to toSplit.
        // In this case, we construct a sub region of splitBy whose left edge is equal to toSplit and do case 2 on that.
        else if(splitBy.Column < toSplit.Column && toSplit.Column < (splitBy.Column + splitBy.Width) && (splitBy.Column + splitBy.Width) < (toSplit.Column + toSplit.Width))
        {
            NavigationRegion temp = new NavigationRegion(splitBy.Id, splitBy.Row, toSplit.Column, splitBy.Column + splitBy.Width - toSplit.Column, splitBy.Height);
            return VerticalSplit(toSplit, temp);
        }

        // Case 5: splitBy is slightly to the right of toSplit. splitBy's left edge is between toSplit's right and left edges, but splitBy's right edge
        // is to the right of toSplit's right edge. We make a smaller region of splitBy whose right edge is eqal to toSplit's right edge and do
        // case 3 on that.
        else if(toSplit.Column < splitBy.Column && splitBy.Column < (toSplit.Column + toSplit.Width) && (toSplit.Column + toSplit.Width) < (splitBy.Column + splitBy.Width))
        {
            NavigationRegion temp = new NavigationRegion(splitBy.Id, splitBy.Row, splitBy.Column, toSplit.Column + toSplit.Width - splitBy.Column, splitBy.Height);
            return VerticalSplit(toSplit, temp);
        }

        return result;
    }

    private List<NavigationRegion> HorizontalSplit(NavigationRegion toSplit, NavigationRegion splitBy)
    {
        List<NavigationRegion> result = new List<NavigationRegion>();

        // Case 1: splitBy sits in the middle of splitBy.
        if(toSplit.Row < splitBy.Row && (splitBy.Row + splitBy.Height) < (toSplit.Row + toSplit.Height))
        {
            NavigationRegion region1, region2, region3;

            region1 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column, toSplit.Width, splitBy.Row - toSplit.Row);
            result.Add(region1);

            region2 = new NavigationRegion(toSplit.Id, splitBy.Row, toSplit.Column, toSplit.Width, splitBy.Height);
            result.Add(region2);

            region3 = new NavigationRegion(toSplit.Id, splitBy.Row + splitBy.Height, toSplit.Column, toSplit.Width, toSplit.Height - (region1.Height + region2.Height));
            result.Add(region3);
        }

        // Case 2: splitBy's top is the same as toSplit's top edge, but toSplit has a larger height.
        // Returns regions 2 and 3 from case 1.
        else if(splitBy.Row == toSplit.Row && (splitBy.Row + splitBy.Height) < (toSplit.Row + toSplit.Height))
        {
            NavigationRegion region1, region2, region3;

            region1 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column, toSplit.Width, splitBy.Row - toSplit.Row);

            region2 = new NavigationRegion(toSplit.Id, splitBy.Row, toSplit.Column, toSplit.Width, splitBy.Height);
            result.Add(region2);

            region3 = new NavigationRegion(toSplit.Id, splitBy.Row + splitBy.Height, toSplit.Column, toSplit.Width, toSplit.Height - (region1.Height + region2.Height));
            result.Add(region3);

        }

        // Case 3: splitBy and toSplit share the same bottom edge, but toSplit's top edge is higher than splitBy's.
        // Returns regions 1 and 2 from case 1.
        else if(toSplit.Row < splitBy.Row && (toSplit.Row + toSplit.Height) == (splitBy.Row + splitBy.Height))
        {
            NavigationRegion region1, region2;

            region1 = new NavigationRegion(toSplit.Id, toSplit.Row, toSplit.Column, toSplit.Width, splitBy.Row - toSplit.Row);
            result.Add(region1);

            region2 = new NavigationRegion(toSplit.Id, splitBy.Row, toSplit.Column, toSplit.Width, splitBy.Height);
            result.Add(region2);
        }

        // Case 4: splitBy os slightly below toSplit, so its top edge is between the top and bottom edges of toSplit. We construct a sub
        // region of splitBy that we split toSplit with.
        else if(toSplit.Row < splitBy.Row && splitBy.Row < (toSplit.Row + toSplit.Height) && (toSplit.Row + toSplit.Height) < (splitBy.Row + splitBy.Height))
        {
            NavigationRegion temp = new NavigationRegion(splitBy.Id, splitBy.Row, splitBy.Column, splitBy.Width, (toSplit.Row + toSplit.Height) - splitBy.Row);
            return HorizontalSplit(toSplit, temp);
        }

        // Case 5: splitBy is slighly above toSplit such that splitBy's row is above toSplit's. However, splitBy's bottom edge is between the top
        // and bottom edges of toSplit.
        else if(splitBy.Row < toSplit.Row && toSplit.Row < (splitBy.Row + splitBy.Height) && (splitBy.Row + splitBy.Height) < (toSplit.Row + toSplit.Height))
        {
            NavigationRegion temp = new NavigationRegion(splitBy.Id, toSplit.Row, splitBy.Column, splitBy.Width, (splitBy.Row + splitBy.Height) - toSplit.Row);
            return HorizontalSplit(toSplit, temp);
        }

        return result;
    }
}

