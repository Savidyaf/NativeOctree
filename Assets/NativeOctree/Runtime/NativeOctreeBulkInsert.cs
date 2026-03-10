using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace NativeOctree
{
    public unsafe partial struct NativeOctree<T> where T : unmanaged
    {
        /// <summary>
        /// Clear the tree and insert all elements at once using morton code spatial indexing.
        /// This is the primary insertion path and is optimized for Burst compilation.
        /// </summary>
        /// <param name="incomingElements">Elements to insert. Positions should be within the octree bounds.</param>
        public void ClearAndBulkInsert(NativeArray<OctElement<T>> incomingElements)
        {
            Clear();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndBumpSecondaryVersion(m_Safety);
#endif

            if (elements->Capacity < incomingElements.Length)
            {
                elements->Resize(incomingElements.Length);
            }

            var mortonCodes = new NativeArray<int>(incomingElements.Length, Allocator.Temp);
            var depthExtentsScaling = LookupTables.DepthLookup.Data.Values[maxDepth] / bounds.Extents;

            for (var i = 0; i < incomingElements.Length; i++)
            {
                mortonCodes[i] = MortonCodeUtil.EncodeScaled(incomingElements[i].pos, bounds, depthExtentsScaling);
            }

            var lookupPtr = lookup->Ptr;
            for (var i = 0; i < mortonCodes.Length; i++)
            {
                int atIndex = 0;
                for (int depth = 0; depth <= maxDepth; depth++)
                {
                    lookupPtr[atIndex]++;
                    atIndex = IncrementIndex(depth, mortonCodes, i, atIndex);
                }
            }

            RecursivePrepareLeaves(1, 1);

            var nodesPtr = nodes->Ptr;
            var elementsPtr = elements->Ptr;
            for (var i = 0; i < incomingElements.Length; i++)
            {
                int atIndex = 0;
                for (int depth = 0; depth <= maxDepth; depth++)
                {
                    ref var node = ref nodesPtr[atIndex];
                    if (node.isLeaf)
                    {
                        elementsPtr[node.firstChildIndex + node.count] = incomingElements[i];
                        node.count++;
                        break;
                    }
                    atIndex = IncrementIndex(depth, mortonCodes, i, atIndex);
                }
            }

            mortonCodes.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int IncrementIndex(int depth, NativeArray<int> mortonCodes, int i, int atIndex)
        {
            var atDepth = math.max(0, maxDepth - depth);
            int shiftedMortonCode = (mortonCodes[i] >> ((atDepth - 1) * 3)) & 0b111;
            atIndex += LookupTables.DepthSizeLookup.Data.Values[atDepth] * shiftedMortonCode;
            atIndex++;
            return atIndex;
        }

        void RecursivePrepareLeaves(int prevOffset, int depth)
        {
            var lookupPtr = lookup->Ptr;
            var nodesPtr = nodes->Ptr;

            for (int l = 0; l < 8; l++)
            {
                var at = prevOffset + l * LookupTables.DepthSizeLookup.Data.Values[maxDepth - depth + 1];
                var elementCount = lookupPtr[at];

                if (elementCount > maxLeafElements && depth < maxDepth)
                {
                    RecursivePrepareLeaves(at + 1, depth + 1);
                }
                else if (elementCount != 0)
                {
                    nodesPtr[at] = new OctNode
                    {
                        firstChildIndex = elementsCount,
                        count = 0,
                        isLeaf = true
                    };
                    elementsCount += elementCount;
                }
            }
        }
    }
}
