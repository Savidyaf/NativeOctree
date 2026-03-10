using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NativeOctree
{
    public unsafe partial struct NativeOctree<T> where T : unmanaged
    {
        struct OctreeRangeQuery
        {
            NativeOctree<T> tree;
            UnsafeList<OctElement<T>>* fastResults;
            int count;
            AABB bounds;

            public void Query(NativeOctree<T> tree, AABB bounds, NativeList<OctElement<T>> results)
            {
                this.tree = tree;
                this.bounds = bounds;
                count = 0;

                fastResults = results.GetUnsafeList();

                RecursiveRangeQuery(tree.bounds, false, 1, 1);

                fastResults->Length = count;
            }

            void RecursiveRangeQuery(AABB parentBounds, bool parentContained, int prevOffset, int depth)
            {
                var requiredCapacity = count + 8 * tree.maxLeafElements;
                if (requiredCapacity > fastResults->Capacity)
                {
                    fastResults->Resize(math.max(fastResults->Capacity * 2, requiredCapacity));
                }

                var depthSize = LookupTables.DepthSizeLookup.Data.Values[tree.maxDepth - depth + 1];

                for (int l = 0; l < 8; l++)
                {
                    var childBounds = OctreeMath.GetChildBounds(parentBounds, l);

                    var contained = parentContained;
                    if (!contained)
                    {
                        if (OctreeMath.Contains(bounds, childBounds))
                        {
                            contained = true;
                        }
                        else if (!OctreeMath.Intersects(bounds, childBounds))
                        {
                            continue;
                        }
                    }

                    var at = prevOffset + l * depthSize;
                    var elementCount = tree.lookup->Ptr[at];

                    if (elementCount > tree.maxLeafElements && depth < tree.maxDepth)
                    {
                        RecursiveRangeQuery(childBounds, contained, at + 1, depth + 1);
                    }
                    else if (elementCount != 0)
                    {
                        var node = tree.nodes->Ptr[at];

                        if (contained)
                        {
                            UnsafeUtility.MemCpy(
                                fastResults->Ptr + count,
                                tree.elements->Ptr + node.firstChildIndex,
                                node.count * UnsafeUtility.SizeOf<OctElement<T>>());
                            count += node.count;
                        }
                        else
                        {
                            for (int k = 0; k < node.count; k++)
                            {
                                var element = tree.elements->Ptr[node.firstChildIndex + k];
                                if (OctreeMath.Contains(bounds, element.pos))
                                {
                                    fastResults->Ptr[count++] = element;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
