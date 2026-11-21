using UnityEngine;

public class RooftopStorm : MonoBehaviour
{
    [SerializeField] private SpriteRenderer rooftop;
    [SerializeField] private Material weatherMaterial;
    [SerializeField] private Vector2 lightningRod;

    private const int DropCount = 180;
    private const float StrikeTime = .30f;
    private const float FlashDuration = .95f;
    private static readonly Vector2 RainVelocity = new(-2.7f, -9f);
    private static readonly Color32 RainColor = new(152, 201, 243, 105);

    private readonly System.Random random = new();
    private readonly Vector2[] drops = new Vector2[DropCount];
    private readonly Vector3[] rainVertices = new Vector3[DropCount * 4];
    private Bounds area;
    private Mesh rainMesh;
    private Mesh strikeGlowMesh;
    private MeshRenderer glow;
    private LineRenderer bolt;
    private LineRenderer branch;
    private float stormAge;
    private float nextFlash = 1.8f;

    private void Awake()
    {
        area = rooftop.bounds;
        rainMesh = CreateMesh("Rain", 18, out _);
        rainMesh.MarkDynamic();
        var triangles = new int[DropCount * 6];
        var colors = new Color32[rainVertices.Length];
        for (int i = 0; i < DropCount; i++)
        {
            drops[i] = new Vector2(Range(area.min.x, area.max.x), Range(area.min.y, area.max.y));
            int vertex = i * 4;
            int index = i * 6;
            triangles[index] = vertex;
            triangles[index + 1] = vertex + 1;
            triangles[index + 2] = vertex + 2;
            triangles[index + 3] = vertex;
            triangles[index + 4] = vertex + 2;
            triangles[index + 5] = vertex + 3;
            colors[vertex] = colors[vertex + 1] = RainColor;
            colors[vertex + 2] = colors[vertex + 3] = new Color32(RainColor.r, RainColor.g, RainColor.b, 0);
        }
        UpdateRain(0f);
        rainMesh.triangles = triangles;
        rainMesh.colors32 = colors;

        strikeGlowMesh = CreateMesh("Sky Flash", 19, out glow);
        strikeGlowMesh.vertices = new[]
        {
            new Vector3(area.min.x, area.min.y), new Vector3(area.min.x, area.max.y),
            new Vector3(area.max.x, area.max.y), new Vector3(area.max.x, area.min.y)
        };
        strikeGlowMesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        glow.enabled = false;

        Vector3 rod = lightningRod;
        bolt = CreateBolt("Lightning", .062f, new[]
        {
            rod + new Vector3(1.08f, 1.50f), rod + new Vector3(.74f, 1.03f),
            rod + new Vector3(.83f, .85f), rod + new Vector3(.31f, .67f),
            rod + new Vector3(.37f, .50f), rod + new Vector3(.06f, .23f), rod
        });
        branch = CreateBolt("Lightning Branch", .027f, new[]
        {
            rod + new Vector3(.83f, .85f), rod + new Vector3(.08f, 1.04f),
            rod + new Vector3(-.37f, .90f), rod + new Vector3(-.92f, .95f),
            rod + new Vector3(-1.37f, .64f)
        });
    }

    private void Update()
    {
        float delta = Time.deltaTime;
        if (delta == 0f)
            return;

        UpdateRain(delta);
        stormAge += delta;
        float elapsed = stormAge - nextFlash;
        bool striking = elapsed >= StrikeTime && elapsed < StrikeTime + .16f;
        bolt.enabled = branch.enabled = striking;

        float skyFlash = .10f * Mathf.Clamp01(1f - Mathf.Abs(elapsed - .12f) / .12f);
        float strikeFlash = elapsed >= StrikeTime
            ? .22f * Mathf.Pow(Mathf.Clamp01(1f - (elapsed - StrikeTime) / .55f), 2f)
            : 0f;
        glow.enabled = elapsed >= 0f && elapsed < FlashDuration;
        if (glow.enabled)
        {
            var color = new Color(.63f, .79f, 1f, skyFlash + strikeFlash);
            flashColors[0] = flashColors[1] = flashColors[2] = flashColors[3] = color;
            strikeGlowMesh.colors = flashColors;
        }

        if (elapsed >= FlashDuration)
            nextFlash = stormAge + Range(4f, 7f);
    }

    private readonly Color[] flashColors = new Color[4];

    private void UpdateRain(float delta)
    {
        Vector2 tail = -RainVelocity * .038f;
        for (int i = 0; i < drops.Length; i++)
        {
            Vector2 head = drops[i] + RainVelocity * delta;
            head.x = area.min.x + Mathf.Repeat(head.x - area.min.x, area.size.x);
            head.y = area.min.y + Mathf.Repeat(head.y - area.min.y, area.size.y);
            drops[i] = head;
            int vertex = i * 4;
            rainVertices[vertex] = head;
            rainVertices[vertex + 1] = head + Vector2.right * .019f;
            rainVertices[vertex + 2] = head + tail + Vector2.right * .019f;
            rainVertices[vertex + 3] = head + tail;
        }
        rainMesh.vertices = rainVertices;
        rainMesh.bounds = new Bounds(area.center, area.size + Vector3.one);
    }

    private Mesh CreateMesh(string layerName, int order, out MeshRenderer renderer)
    {
        var layer = new GameObject(layerName);
        layer.transform.SetParent(transform, false);
        layer.transform.position = Vector3.zero;
        var mesh = new Mesh { name = layerName };
        layer.AddComponent<MeshFilter>().sharedMesh = mesh;
        renderer = layer.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = weatherMaterial;
        renderer.sortingOrder = order;
        return mesh;
    }

    private LineRenderer CreateBolt(string layerName, float width, Vector3[] points)
    {
        var layer = new GameObject(layerName);
        layer.transform.SetParent(transform, false);
        var line = layer.AddComponent<LineRenderer>();
        line.sharedMaterial = weatherMaterial;
        line.sortingOrder = 16;
        line.startWidth = width;
        line.endWidth = width * .35f;
        line.startColor = new Color(.73f, .90f, 1f);
        line.endColor = new Color(.91f, .98f, 1f);
        line.positionCount = points.Length;
        line.SetPositions(points);
        line.enabled = false;
        return line;
    }

    private float Range(float min, float max) => Mathf.Lerp(min, max, (float)random.NextDouble());

    private void OnDestroy()
    {
        Destroy(rainMesh);
        Destroy(strikeGlowMesh);
    }
}
