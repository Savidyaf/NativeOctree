using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace NativeOctree.Drawing
{
    /// <summary>
    /// Debug visualization utilities for NativeOctree. Draws elements and query results
    /// using Debug.DrawLine and a 2D texture projection.
    /// </summary>
    public static unsafe class NativeOctreeDrawing
    {
        public static void Draw<T>(NativeOctree<T> tree, NativeList<OctElement<T>> results, AABB range,
            Color[][] texture) where T : unmanaged
        {
            var treeBounds = tree.Bounds;
            var widthMult = texture.Length / treeBounds.Extents.x * 2 / 2 / 2;
            var heightMult = texture[0].Length / treeBounds.Extents.y * 2 / 2 / 2;

            var widthAdd = treeBounds.Center.x + treeBounds.Extents.x;
            var heightAdd = treeBounds.Center.y + treeBounds.Extents.y;

            var totalNodes = tree.NodeCount;
            var nodesPtr = tree.NodesPtr;
            var elementsPtr = tree.ElementsPtr;

            for (int i = 0; i < totalNodes; i++)
            {
                var node = nodesPtr[i];
                if (!node.isLeaf || node.count <= 0)
                    continue;

                for (int k = 0; k < node.count; k++)
                {
                    var element = elementsPtr[node.firstChildIndex + k];
                    DrawPoint(element, Color.red);
                    texture[(int)((element.pos.x + widthAdd) * widthMult)]
                           [(int)((element.pos.y + heightAdd) * heightMult)] = Color.red;
                }
            }

            foreach (var element in results)
            {
                DrawPoint(element, Color.green);
                texture[(int)((element.pos.x + widthAdd) * widthMult)]
                       [(int)((element.pos.y + heightAdd) * heightMult)] = Color.green;
            }

            DrawBounds(texture, range, treeBounds);
        }

        static void DrawPoint<T>(OctElement<T> element, Color color) where T : unmanaged
        {
            Debug.DrawLine(element.pos + (float3)Vector3.left, element.pos + (float3)Vector3.right, color, 15f);
            Debug.DrawLine(element.pos + (float3)Vector3.up, element.pos + (float3)Vector3.down, color, 15f);
            Debug.DrawLine(element.pos + (float3)Vector3.back, element.pos + (float3)Vector3.forward, color, 15f);
        }

        static void DrawBounds(Color[][] texture, AABB bounds, AABB treeBounds)
        {
            var widthMult = texture.Length / treeBounds.Extents.x * 2 / 2 / 2;
            var heightMult = texture[0].Length / treeBounds.Extents.y * 2 / 2 / 2;

            var widthAdd = treeBounds.Center.x + treeBounds.Extents.x;
            var heightAdd = treeBounds.Center.y + treeBounds.Extents.y;

            var left = new float2(bounds.Center.x - bounds.Extents.x, bounds.Center.y);

            for (int leftToRight = 0; leftToRight < bounds.Extents.x * 2; leftToRight++)
            {
                var posX = left.x + leftToRight;
                texture[(int)((posX + widthAdd) * widthMult)][(int)((bounds.Center.y + heightAdd + bounds.Extents.y) * heightMult)] = Color.blue;
                texture[(int)((posX + widthAdd) * widthMult)][(int)((bounds.Center.y + heightAdd - bounds.Extents.y) * heightMult)] = Color.blue;
            }

            var top = new float2(bounds.Center.x, bounds.Center.y - bounds.Extents.y);

            for (int topToBottom = 0; topToBottom < bounds.Extents.y * 2; topToBottom++)
            {
                var posY = top.y + topToBottom;
                texture[(int)((bounds.Center.x + widthAdd + bounds.Extents.x) * widthMult)][(int)((posY + heightAdd) * heightMult)] = Color.blue;
                texture[(int)((bounds.Center.x + widthAdd - bounds.Extents.x) * widthMult)][(int)((posY + heightAdd) * heightMult)] = Color.blue;
            }
        }
    }
}
