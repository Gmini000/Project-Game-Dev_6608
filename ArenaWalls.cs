using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ArenaWalls : MonoBehaviour
{
    [SerializeField] private SpriteRenderer arenaArtwork;
    [SerializeField] private Sprite wallSprite;
    [SerializeField] private WallSlot[] wallSlots;
    [SerializeField, Min(0)] private int wallCount = 3;

    private static int? wallMask;
    private static int WallMask => wallMask ??= LayerMask.GetMask("ArenaWall");

    private readonly Dictionary<Vector2, NavigationGrid> grids = new();

    [Serializable]
    private struct WallSlot
    {
        public Vector2 position;
        public bool allowVertical;
    }

    private void Awake()
    {
        int wallLayer = LayerMask.NameToLayer("ArenaWall");
        var slots = new List<WallSlot>(wallSlots);
        var verticalSlots = slots.FindAll(slot => slot.allowVertical);
        if (verticalSlots.Count > 0)
        {
            WallSlot chosen = verticalSlots[Random.Range(0, verticalSlots.Count)];
            slots.Remove(chosen);
            slots.Insert(0, chosen);
        }

        for (int i = 0; i < wallCount && slots.Count > 0; i++)
        {
            bool vertical = i == 0 && slots[0].allowVertical;
            int slot = vertical ? 0 : Random.Range(0, slots.Count);
            Vector2 center = slots[slot].position + new Vector2(Random.Range(-.14f, .14f), Random.Range(-.12f, .12f));
            slots.RemoveAt(slot);
            Vector2 size = new Vector2(Random.Range(1.28f, 1.58f), Random.Range(.66f, .88f));

            var wall = new GameObject($"Wall {i + 1}") { layer = wallLayer };
            wall.transform.SetParent(transform, false);
            wall.transform.position = center;
            wall.AddComponent<BoxCollider2D>().size = vertical ? new Vector2(size.y, size.x) : size;

            var art = new GameObject("Wall Artwork").AddComponent<SpriteRenderer>();
            art.transform.SetParent(wall.transform, false);
            art.sprite = wallSprite;
            art.sortingOrder = -1;
            if (vertical)
                art.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            Vector3 spriteSize = wallSprite.bounds.size;
            art.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
        }

        Physics2D.SyncTransforms();
    }

    public static bool BlocksAttack(Vector2 from, Vector2 to)
    {
        return Physics2D.Linecast(from, to, WallMask).collider != null;
    }

    public static bool CanTravel(Vector2 from, Vector2 to, Vector2 bodySize)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < .0001f)
            return true;

        // Leave a small skin so bodies resting against a wall can still move away.
        Vector2 castSize = bodySize - Vector2.one * .03f;
        return Physics2D.BoxCast(from, castSize, 0f, delta.normalized, delta.magnitude, WallMask).collider == null;
    }

    public Vector2 NextWaypoint(Vector2 from, Vector2 to, Vector2 bodySize)
    {
        if (!grids.TryGetValue(bodySize, out NavigationGrid grid))
        {
            grid = new NavigationGrid(arenaArtwork.bounds, bodySize);
            grids.Add(bodySize, grid);
        }

        return grid.NextWaypoint(from, to);
    }

    private sealed class NavigationGrid
    {
        private const float CellSize = .4f;
        private static readonly Vector2Int[] Neighbors =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        private readonly Vector2 origin;
        private readonly Vector2 bodySize;
        private readonly int columns;
        private readonly int rows;
        private readonly bool[] open;
        private readonly int[] previous;
        private readonly Queue<int> search = new();

        public NavigationGrid(Bounds bounds, Vector2 bodySize)
        {
            this.bodySize = bodySize;
            origin = (Vector2)bounds.min + Vector2.one * (CellSize * .5f);
            columns = Mathf.FloorToInt(bounds.size.x / CellSize);
            rows = Mathf.FloorToInt(bounds.size.y / CellSize);
            open = new bool[columns * rows];
            previous = new int[open.Length];
            for (int i = 0; i < open.Length; i++)
                open[i] = Physics2D.OverlapBox(Position(i), bodySize + Vector2.one * .06f, 0f, WallMask) == null;
        }

        public Vector2 NextWaypoint(Vector2 from, Vector2 to)
        {
            int start = NearestCell(from, true);
            int goal = NearestCell(to, false);
            if (start < 0 || goal < 0)
                return from;

            Array.Fill(previous, -1);
            search.Clear();
            previous[start] = start;
            search.Enqueue(start);
            while (search.Count > 0 && previous[goal] < 0)
            {
                int current = search.Dequeue();
                var cell = new Vector2Int(current % columns, current / columns);
                foreach (Vector2Int direction in Neighbors)
                {
                    Vector2Int next = cell + direction;
                    if (!InBounds(next.x, next.y))
                        continue;

                    int index = next.y * columns + next.x;
                    if (open[index] && previous[index] < 0)
                    {
                        previous[index] = current;
                        search.Enqueue(index);
                    }
                }
            }

            if (previous[goal] < 0)
                return from;

            for (int cell = goal; cell != start; cell = previous[cell])
            {
                Vector2 position = Position(cell);
                if (CanTravel(from, position, bodySize))
                    return position;
            }

            return Position(start);
        }

        private int NearestCell(Vector2 position, bool mustReach)
        {
            Vector2 relative = (position - origin) / CellSize;
            int x = Mathf.RoundToInt(relative.x);
            int y = Mathf.RoundToInt(relative.y);
            int best = -1;
            float bestDistance = float.PositiveInfinity;
            for (int row = y - 4; row <= y + 4; row++)
            for (int column = x - 4; column <= x + 4; column++)
            {
                if (!InBounds(column, row))
                    continue;

                int index = row * columns + column;
                Vector2 candidate = Position(index);
                float distance = (candidate - position).sqrMagnitude;
                if (!open[index] || distance >= bestDistance ||
                    (mustReach && !CanTravel(position, candidate, bodySize)))
                    continue;

                best = index;
                bestDistance = distance;
            }

            return best;
        }

        private bool InBounds(int x, int y) => x >= 0 && x < columns && y >= 0 && y < rows;

        private Vector2 Position(int index) =>
            origin + new Vector2(index % columns, index / columns) * CellSize;
    }
}
