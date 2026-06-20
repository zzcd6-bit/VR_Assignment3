using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using System;
using System.Collections.Generic;

public class TriMirrorMazeGenerator : EditorWindow
{
    [Header("Shape")]
    private string heightProfile = "8,9,8,9,8,7,8,6,7,5,6,4,5,4,4";
    private float cellSize = 2.0f;

    [Header("Maze Algorithm")]
    private int mazeSeed = 123;
    [Range(0f, 0.5f)]
    private float extraOpeningChance = 0.08f;
    private bool showPathPreview = false;

    [Header("Wall Settings")]
    private float wallHeight = 2.8f;
    private float wallThickness = 0.12f;
    private Material mirrorMaterial;

    [Header("Post Settings")]
    private bool generatePosts = true;
    private bool postHasCollider = false;
    private float postRadius = 0.12f;
    private Material postMaterial;

    [Header("Floor")]
    private bool generateFloor = true;
    private Material floorMaterial;

    [Header("Reflection Probe")]
    private bool generateReflectionProbe = true;
    private bool realtimeReflectionProbe = false;
    private int reflectionProbeResolution = 128;
    private float reflectionProbeExtraPadding = 2f;

    [Header("Output")]
    private string parentName = "Generated_Tri_Mirror_Maze";
    private bool clearPrevious = true;

    private const string GeneratedFolder = "Assets/MirrorMazeGenerated";
    private const float SQRT3_OVER_2 = 0.8660254f;

    [MenuItem("Tools/VR Mirror Maze/Triangular Maze Generator")]
    public static void OpenWindow()
    {
        GetWindow<TriMirrorMazeGenerator>("Tri Mirror Maze");
    }

    private void OnGUI()
    {
        GUILayout.Label("Triangular VR Mirror Maze Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space(8);

        GUILayout.Label("Shape Profile", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Height Profile 控制每一列的高度。\n" +
            "例如：8,9,8,7,6 会生成左高右低的三角网格形状。",
            MessageType.Info
        );

        heightProfile = EditorGUILayout.TextArea(heightProfile, GUILayout.Height(45));
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);

        EditorGUILayout.Space(8);

        GUILayout.Label("Maze Algorithm", EditorStyles.boldLabel);
        mazeSeed = EditorGUILayout.IntField("Maze Seed", mazeSeed);
        extraOpeningChance = EditorGUILayout.Slider("Extra Opening Chance", extraOpeningChance, 0f, 0.5f);
        showPathPreview = EditorGUILayout.Toggle("Show Path Preview", showPathPreview);

        EditorGUILayout.Space(8);

        GUILayout.Label("Wall Settings", EditorStyles.boldLabel);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);
        mirrorMaterial = (Material)EditorGUILayout.ObjectField("Mirror Material", mirrorMaterial, typeof(Material), false);

        EditorGUILayout.Space(8);

        GUILayout.Label("Post Settings", EditorStyles.boldLabel);
        generatePosts = EditorGUILayout.Toggle("Generate Posts", generatePosts);
        postHasCollider = EditorGUILayout.Toggle("Post Has Collider", postHasCollider);
        postRadius = EditorGUILayout.FloatField("Post Radius", postRadius);
        postMaterial = (Material)EditorGUILayout.ObjectField("Post Material", postMaterial, typeof(Material), false);

        EditorGUILayout.Space(8);

        GUILayout.Label("Floor", EditorStyles.boldLabel);
        generateFloor = EditorGUILayout.Toggle("Generate Floor", generateFloor);
        floorMaterial = (Material)EditorGUILayout.ObjectField("Floor Material", floorMaterial, typeof(Material), false);

        EditorGUILayout.Space(8);

        GUILayout.Label("Reflection Probe", EditorStyles.boldLabel);
        generateReflectionProbe = EditorGUILayout.Toggle("Generate Reflection Probe", generateReflectionProbe);
        realtimeReflectionProbe = EditorGUILayout.Toggle("Realtime Probe", realtimeReflectionProbe);
        reflectionProbeResolution = EditorGUILayout.IntField("Probe Resolution", reflectionProbeResolution);
        reflectionProbeExtraPadding = EditorGUILayout.FloatField("Probe Extra Padding", reflectionProbeExtraPadding);

        EditorGUILayout.Space(8);

        GUILayout.Label("Output", EditorStyles.boldLabel);
        parentName = EditorGUILayout.TextField("Parent Name", parentName);
        clearPrevious = EditorGUILayout.Toggle("Clear Previous", clearPrevious);

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Generate Mirror Maze", GUILayout.Height(35)))
        {
            GenerateMaze();
        }
    }

    private void GenerateMaze()
    {
        int[] heights = ParseHeightProfile(heightProfile);

        if (heights == null || heights.Length <= 1)
        {
            Debug.LogError("Height Profile 无效。请使用类似：8,9,8,7,6,5");
            return;
        }

        if (clearPrevious)
        {
            GameObject old = GameObject.Find(parentName);
            if (old != null)
            {
                DestroyImmediate(old);
            }
        }

        GameObject parent = new GameObject(parentName);
        Undo.RegisterCreatedObjectUndo(parent, "Generate Tri Mirror Maze");

        Material wallMat = mirrorMaterial != null ? mirrorMaterial : GetOrCreateMirrorMaterial();
        Material pMat = postMaterial != null ? postMaterial : GetOrCreatePostMaterial();
        Material fMat = floorMaterial != null ? floorMaterial : GetOrCreateFloorMaterial();

        Dictionary<Vector2Int, Vector3> nodes = GenerateNodes(heights);
        HashSet<EdgeKey> fullEdges = GenerateFullEdges(nodes, heights);

        List<TriangleCell> cells = GenerateTriangleCells(nodes, fullEdges);
        Dictionary<EdgeKey, EdgeSegment> edgeSegments = BuildEdgeSegments(cells);

        if (cells.Count == 0)
        {
            Debug.LogError("没有生成有效的三角形 Cell，请检查 Height Profile。");
            DestroyImmediate(parent);
            return;
        }

        List<NeighborLink>[] neighborLinks = BuildNeighborLinks(cells.Count, edgeSegments);

        HashSet<int> boundaryCells = GetBoundaryCells(edgeSegments);
        int startCellId = PickStartCell(cells, boundaryCells);
        HashSet<EdgeKey> openings = CarveMazeByDFS(cells.Count, startCellId, neighborLinks, mazeSeed);

        AddExtraOpenings(openings, edgeSegments, extraOpeningChance, mazeSeed + 999);

        int exitCellId = PickExitCellByFarthestPath(startCellId, cells, boundaryCells, neighborLinks, openings);

        if (TryPickBoundaryEdgeForCell(startCellId, edgeSegments, nodes, true, out EdgeKey entranceEdge))
        {
            openings.Add(entranceEdge);
        }

        if (TryPickBoundaryEdgeForCell(exitCellId, edgeSegments, nodes, false, out EdgeKey exitEdge))
        {
            openings.Add(exitEdge);
        }

        foreach (KeyValuePair<EdgeKey, EdgeSegment> pair in edgeSegments)
        {
            if (openings.Contains(pair.Key))
            {
                continue;
            }

            Vector3 a = nodes[pair.Key.a];
            Vector3 b = nodes[pair.Key.b];

            CreateWall(a, b, parent.transform, wallMat);
        }

        if (generatePosts)
        {
            foreach (KeyValuePair<Vector2Int, Vector3> node in nodes)
            {
                CreatePost(node.Value, parent.transform, pMat);
            }
        }

        if (generateFloor)
        {
            CreateFloor(nodes, parent.transform, fMat);
        }

        if (generateReflectionProbe)
        {
            CreateReflectionProbe(nodes, parent.transform);
        }

        if (showPathPreview)
        {
            List<int> path = FindPath(startCellId, exitCellId, neighborLinks, openings);
            CreatePathPreview(path, cells, parent.transform);
        }

        CreateStartExitMarkers(cells[startCellId].center, cells[exitCellId].center, parent.transform);

        Selection.activeGameObject = parent;

        Debug.Log("Mirror Maze generated. A valid path from entrance to exit is guaranteed.");
    }

    private int[] ParseHeightProfile(string profile)
    {
        string[] parts = profile.Split(',', ';', ' ', '\n', '\t');
        List<int> values = new List<int>();

        foreach (string part in parts)
        {
            if (int.TryParse(part, out int value))
            {
                values.Add(Mathf.Max(1, value));
            }
        }

        return values.ToArray();
    }

    private Dictionary<Vector2Int, Vector3> GenerateNodes(int[] heights)
    {
        Dictionary<Vector2Int, Vector3> nodes = new Dictionary<Vector2Int, Vector3>();

        float xSpacing = cellSize * SQRT3_OVER_2;
        float zSpacing = cellSize;

        for (int col = 0; col < heights.Length; col++)
        {
            float zOffset = col % 2 == 0 ? 0f : zSpacing * 0.5f;

            for (int row = 0; row <= heights[col]; row++)
            {
                Vector3 pos = new Vector3(
                    col * xSpacing,
                    0f,
                    row * zSpacing + zOffset
                );

                nodes.Add(new Vector2Int(col, row), pos);
            }
        }

        return nodes;
    }

    private HashSet<EdgeKey> GenerateFullEdges(Dictionary<Vector2Int, Vector3> nodes, int[] heights)
    {
        HashSet<EdgeKey> edges = new HashSet<EdgeKey>();

        for (int col = 0; col < heights.Length; col++)
        {
            for (int row = 0; row < heights[col]; row++)
            {
                edges.Add(new EdgeKey(new Vector2Int(col, row), new Vector2Int(col, row + 1)));
            }
        }

        float targetZDistance = cellSize * 0.5f;
        float tolerance = cellSize * 0.1f;

        for (int col = 0; col < heights.Length - 1; col++)
        {
            for (int rowA = 0; rowA <= heights[col]; rowA++)
            {
                Vector2Int a = new Vector2Int(col, rowA);
                Vector3 posA = nodes[a];

                for (int rowB = 0; rowB <= heights[col + 1]; rowB++)
                {
                    Vector2Int b = new Vector2Int(col + 1, rowB);
                    Vector3 posB = nodes[b];

                    float zDistance = Mathf.Abs(posA.z - posB.z);

                    if (Mathf.Abs(zDistance - targetZDistance) <= tolerance)
                    {
                        edges.Add(new EdgeKey(a, b));
                    }
                }
            }
        }

        return edges;
    }

    private List<TriangleCell> GenerateTriangleCells(Dictionary<Vector2Int, Vector3> nodes, HashSet<EdgeKey> fullEdges)
    {
        List<Vector2Int> nodeKeys = new List<Vector2Int>(nodes.Keys);
        List<TriangleCell> cells = new List<TriangleCell>();

        int id = 0;

        for (int i = 0; i < nodeKeys.Count; i++)
        {
            for (int j = i + 1; j < nodeKeys.Count; j++)
            {
                for (int k = j + 1; k < nodeKeys.Count; k++)
                {
                    Vector2Int a = nodeKeys[i];
                    Vector2Int b = nodeKeys[j];
                    Vector2Int c = nodeKeys[k];

                    EdgeKey ab = new EdgeKey(a, b);
                    EdgeKey bc = new EdgeKey(b, c);
                    EdgeKey ca = new EdgeKey(c, a);

                    if (!fullEdges.Contains(ab) || !fullEdges.Contains(bc) || !fullEdges.Contains(ca))
                    {
                        continue;
                    }

                    Vector3 pa = nodes[a];
                    Vector3 pb = nodes[b];
                    Vector3 pc = nodes[c];

                    float area = Vector3.Cross(pb - pa, pc - pa).magnitude * 0.5f;

                    if (area < 0.01f)
                    {
                        continue;
                    }

                    TriangleCell cell = new TriangleCell();
                    cell.id = id;
                    cell.vertices = new Vector2Int[] { a, b, c };
                    cell.center = (pa + pb + pc) / 3f;

                    cell.edges.Add(ab);
                    cell.edges.Add(bc);
                    cell.edges.Add(ca);

                    cells.Add(cell);
                    id++;
                }
            }
        }

        return cells;
    }

    private Dictionary<EdgeKey, EdgeSegment> BuildEdgeSegments(List<TriangleCell> cells)
    {
        Dictionary<EdgeKey, EdgeSegment> segments = new Dictionary<EdgeKey, EdgeSegment>();

        foreach (TriangleCell cell in cells)
        {
            foreach (EdgeKey edge in cell.edges)
            {
                if (!segments.ContainsKey(edge))
                {
                    segments.Add(edge, new EdgeSegment(edge));
                }

                segments[edge].cellIds.Add(cell.id);
            }
        }

        return segments;
    }

    private List<NeighborLink>[] BuildNeighborLinks(int cellCount, Dictionary<EdgeKey, EdgeSegment> edgeSegments)
    {
        List<NeighborLink>[] links = new List<NeighborLink>[cellCount];

        for (int i = 0; i < cellCount; i++)
        {
            links[i] = new List<NeighborLink>();
        }

        foreach (KeyValuePair<EdgeKey, EdgeSegment> pair in edgeSegments)
        {
            EdgeSegment segment = pair.Value;

            if (segment.cellIds.Count != 2)
            {
                continue;
            }

            int a = segment.cellIds[0];
            int b = segment.cellIds[1];

            links[a].Add(new NeighborLink(b, pair.Key));
            links[b].Add(new NeighborLink(a, pair.Key));
        }

        return links;
    }

    private HashSet<int> GetBoundaryCells(Dictionary<EdgeKey, EdgeSegment> edgeSegments)
    {
        HashSet<int> boundaryCells = new HashSet<int>();

        foreach (KeyValuePair<EdgeKey, EdgeSegment> pair in edgeSegments)
        {
            if (pair.Value.cellIds.Count == 1)
            {
                boundaryCells.Add(pair.Value.cellIds[0]);
            }
        }

        return boundaryCells;
    }

    private int PickStartCell(List<TriangleCell> cells, HashSet<int> boundaryCells)
    {
        int bestId = 0;
        float bestScore = float.MaxValue;

        foreach (int id in boundaryCells)
        {
            Vector3 c = cells[id].center;
            float score = c.x + c.z * 0.2f;

            if (score < bestScore)
            {
                bestScore = score;
                bestId = id;
            }
        }

        return bestId;
    }

    private int PickExitCellByFarthestPath(
        int startCellId,
        List<TriangleCell> cells,
        HashSet<int> boundaryCells,
        List<NeighborLink>[] neighborLinks,
        HashSet<EdgeKey> openings)
    {
        int[] distance = new int[cells.Count];

        for (int i = 0; i < distance.Length; i++)
        {
            distance[i] = -1;
        }

        Queue<int> queue = new Queue<int>();
        queue.Enqueue(startCellId);
        distance[startCellId] = 0;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            foreach (NeighborLink link in neighborLinks[current])
            {
                if (!openings.Contains(link.edge))
                {
                    continue;
                }

                if (distance[link.cellId] != -1)
                {
                    continue;
                }

                distance[link.cellId] = distance[current] + 1;
                queue.Enqueue(link.cellId);
            }
        }

        int bestId = startCellId;
        int bestDistance = -1;

        foreach (int id in boundaryCells)
        {
            if (distance[id] > bestDistance)
            {
                bestDistance = distance[id];
                bestId = id;
            }
        }

        return bestId;
    }

    private HashSet<EdgeKey> CarveMazeByDFS(
        int cellCount,
        int startCellId,
        List<NeighborLink>[] neighborLinks,
        int seed)
    {
        HashSet<EdgeKey> openings = new HashSet<EdgeKey>();
        bool[] visited = new bool[cellCount];

        System.Random random = new System.Random(seed);
        Stack<int> stack = new Stack<int>();

        visited[startCellId] = true;
        stack.Push(startCellId);

        while (stack.Count > 0)
        {
            int current = stack.Peek();

            List<NeighborLink> unvisitedNeighbors = new List<NeighborLink>();

            foreach (NeighborLink link in neighborLinks[current])
            {
                if (!visited[link.cellId])
                {
                    unvisitedNeighbors.Add(link);
                }
            }

            if (unvisitedNeighbors.Count == 0)
            {
                stack.Pop();
                continue;
            }

            NeighborLink chosen = unvisitedNeighbors[random.Next(unvisitedNeighbors.Count)];

            openings.Add(chosen.edge);
            visited[chosen.cellId] = true;
            stack.Push(chosen.cellId);
        }

        return openings;
    }

    private void AddExtraOpenings(
        HashSet<EdgeKey> openings,
        Dictionary<EdgeKey, EdgeSegment> edgeSegments,
        float chance,
        int seed)
    {
        if (chance <= 0f)
        {
            return;
        }

        System.Random random = new System.Random(seed);

        foreach (KeyValuePair<EdgeKey, EdgeSegment> pair in edgeSegments)
        {
            if (pair.Value.cellIds.Count != 2)
            {
                continue;
            }

            if (openings.Contains(pair.Key))
            {
                continue;
            }

            if (random.NextDouble() < chance)
            {
                openings.Add(pair.Key);
            }
        }
    }

    private bool TryPickBoundaryEdgeForCell(
        int cellId,
        Dictionary<EdgeKey, EdgeSegment> edgeSegments,
        Dictionary<Vector2Int, Vector3> nodes,
        bool isEntrance,
        out EdgeKey result)
    {
        result = default;
        bool found = false;

        float bestScore = isEntrance ? float.MaxValue : float.MinValue;

        foreach (KeyValuePair<EdgeKey, EdgeSegment> pair in edgeSegments)
        {
            EdgeSegment segment = pair.Value;

            if (segment.cellIds.Count != 1)
            {
                continue;
            }

            if (segment.cellIds[0] != cellId)
            {
                continue;
            }

            Vector3 a = nodes[pair.Key.a];
            Vector3 b = nodes[pair.Key.b];
            Vector3 mid = (a + b) * 0.5f;

            float score = mid.x + mid.z * 0.15f;

            if (isEntrance)
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    result = pair.Key;
                    found = true;
                }
            }
            else
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    result = pair.Key;
                    found = true;
                }
            }
        }

        return found;
    }

    private List<int> FindPath(
        int start,
        int exit,
        List<NeighborLink>[] neighborLinks,
        HashSet<EdgeKey> openings)
    {
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();
        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == exit)
            {
                break;
            }

            foreach (NeighborLink link in neighborLinks[current])
            {
                if (!openings.Contains(link.edge))
                {
                    continue;
                }

                if (visited.Contains(link.cellId))
                {
                    continue;
                }

                visited.Add(link.cellId);
                cameFrom[link.cellId] = current;
                queue.Enqueue(link.cellId);
            }
        }

        List<int> path = new List<int>();

        if (!cameFrom.ContainsKey(exit) && start != exit)
        {
            return path;
        }

        int node = exit;
        path.Add(node);

        while (node != start)
        {
            node = cameFrom[node];
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    private void CreateWall(Vector3 start, Vector3 end, Transform parent, Material material)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        Vector3 center = (start + end) * 0.5f;
        center.y = wallHeight * 0.5f;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Mirror_Wall";
        wall.transform.SetParent(parent);
        wall.transform.position = center;
        wall.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        wall.transform.localScale = new Vector3(wallThickness, wallHeight, length);

        if (material != null)
        {
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        Collider col = wall.GetComponent<Collider>();
        col.isTrigger = false;
    }

    private void CreatePost(Vector3 position, Transform parent, Material material)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Mirror_Post";
        post.transform.SetParent(parent);
        post.transform.position = new Vector3(position.x, wallHeight * 0.5f, position.z);
        post.transform.localScale = new Vector3(postRadius * 2f, wallHeight * 0.5f, postRadius * 2f);

        if (material != null)
        {
            post.GetComponent<Renderer>().sharedMaterial = material;
        }

        Collider col = post.GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = false;
            col.enabled = postHasCollider;
        }
    }

    private void CreateFloor(Dictionary<Vector2Int, Vector3> nodes, Transform parent, Material material)
    {
        GetBounds(nodes, out float minX, out float maxX, out float minZ, out float maxZ);

        float padding = cellSize * 1.5f;

        Vector3 center = new Vector3(
            (minX + maxX) * 0.5f,
            -0.05f,
            (minZ + maxZ) * 0.5f
        );

        Vector3 size = new Vector3(
            maxX - minX + padding * 2f,
            0.1f,
            maxZ - minZ + padding * 2f
        );

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Maze_Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = center;
        floor.transform.localScale = size;

        if (material != null)
        {
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private void CreateReflectionProbe(Dictionary<Vector2Int, Vector3> nodes, Transform parent)
    {
        GetBounds(nodes, out float minX, out float maxX, out float minZ, out float maxZ);

        float padding = reflectionProbeExtraPadding;

        Vector3 center = new Vector3(
            (minX + maxX) * 0.5f,
            wallHeight * 0.5f,
            (minZ + maxZ) * 0.5f
        );

        Vector3 size = new Vector3(
            maxX - minX + padding * 2f,
            wallHeight + padding,
            maxZ - minZ + padding * 2f
        );

        GameObject probeObject = new GameObject("Maze_Reflection_Probe");
        probeObject.transform.SetParent(parent);
        probeObject.transform.position = center;

        ReflectionProbe probe = probeObject.AddComponent<ReflectionProbe>();
        probe.size = size;
        probe.center = Vector3.zero;
        probe.resolution = Mathf.Max(32, reflectionProbeResolution);
        probe.intensity = 1f;
        probe.boxProjection = true;
        probe.hdr = true;

        if (realtimeReflectionProbe)
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.OnAwake;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
        }
        else
        {
            probe.mode = ReflectionProbeMode.Baked;
        }
    }

    private void CreatePathPreview(List<int> path, List<TriangleCell> cells, Transform parent)
    {
        if (path == null || path.Count <= 1)
        {
            return;
        }

        GameObject lineObject = new GameObject("Guaranteed_Path_Preview");
        lineObject.transform.SetParent(parent);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = path.Count;
        line.widthMultiplier = 0.08f;
        line.useWorldSpace = true;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material mat = new Material(shader);

        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", Color.green);
        }
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", Color.green);
        }

        line.sharedMaterial = mat;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = cells[path[i]].center;
            p.y = 0.08f;
            line.SetPosition(i, p);
        }
    }

    private void CreateStartExitMarkers(Vector3 start, Vector3 exit, Transform parent)
    {
        GameObject startMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        startMarker.name = "START_Position";
        startMarker.transform.SetParent(parent);
        startMarker.transform.position = new Vector3(start.x, 0.03f, start.z);
        startMarker.transform.localScale = new Vector3(0.6f, 0.03f, 0.6f);

        GameObject exitMarker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        exitMarker.name = "EXIT_Position";
        exitMarker.transform.SetParent(parent);
        exitMarker.transform.position = new Vector3(exit.x, 0.03f, exit.z);
        exitMarker.transform.localScale = new Vector3(0.6f, 0.03f, 0.6f);

        Collider startCollider = startMarker.GetComponent<Collider>();
        Collider exitCollider = exitMarker.GetComponent<Collider>();

        if (startCollider != null) startCollider.enabled = false;
        if (exitCollider != null) exitCollider.enabled = false;
    }

    private void GetBounds(
        Dictionary<Vector2Int, Vector3> nodes,
        out float minX,
        out float maxX,
        out float minZ,
        out float maxZ)
    {
        minX = float.MaxValue;
        maxX = float.MinValue;
        minZ = float.MaxValue;
        maxZ = float.MinValue;

        foreach (Vector3 pos in nodes.Values)
        {
            minX = Mathf.Min(minX, pos.x);
            maxX = Mathf.Max(maxX, pos.x);
            minZ = Mathf.Min(minZ, pos.z);
            maxZ = Mathf.Max(maxZ, pos.z);
        }
    }

    private Material GetOrCreateMirrorMaterial()
    {
        EnsureGeneratedFolder();

        string path = GeneratedFolder + "/M_Mirror_Auto.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        mat.name = "M_Mirror_Auto";

        SetMaterialColor(mat, new Color(0.75f, 0.85f, 0.9f, 1f));

        if (mat.HasProperty("_Metallic"))
        {
            mat.SetFloat("_Metallic", 1f);
        }

        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", 0.96f);
        }

        if (mat.HasProperty("_Glossiness"))
        {
            mat.SetFloat("_Glossiness", 0.96f);
        }

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private Material GetOrCreatePostMaterial()
    {
        EnsureGeneratedFolder();

        string path = GeneratedFolder + "/M_Post_Auto.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        mat.name = "M_Post_Auto";

        SetMaterialColor(mat, new Color(0.08f, 0.08f, 0.09f, 1f));

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private Material GetOrCreateFloorMaterial()
    {
        EnsureGeneratedFolder();

        string path = GeneratedFolder + "/M_Floor_Auto.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (mat != null)
        {
            return mat;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");

        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        mat = new Material(shader);
        mat.name = "M_Floor_Auto";

        SetMaterialColor(mat, new Color(0.12f, 0.12f, 0.14f, 1f));

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
        {
            mat.SetColor("_BaseColor", color);
        }
        else if (mat.HasProperty("_Color"))
        {
            mat.SetColor("_Color", color);
        }
    }

    private void EnsureGeneratedFolder()
    {
        if (!AssetDatabase.IsValidFolder(GeneratedFolder))
        {
            AssetDatabase.CreateFolder("Assets", "MirrorMazeGenerated");
        }
    }

    private class TriangleCell
    {
        public int id;
        public Vector2Int[] vertices;
        public Vector3 center;
        public List<EdgeKey> edges = new List<EdgeKey>();
    }

    private class EdgeSegment
    {
        public EdgeKey key;
        public List<int> cellIds = new List<int>();

        public EdgeSegment(EdgeKey key)
        {
            this.key = key;
        }
    }

    private class NeighborLink
    {
        public int cellId;
        public EdgeKey edge;

        public NeighborLink(int cellId, EdgeKey edge)
        {
            this.cellId = cellId;
            this.edge = edge;
        }
    }

    private struct EdgeKey : IEquatable<EdgeKey>
    {
        public Vector2Int a;
        public Vector2Int b;

        public EdgeKey(Vector2Int p1, Vector2Int p2)
        {
            if (p1.x < p2.x || p1.x == p2.x && p1.y <= p2.y)
            {
                a = p1;
                b = p2;
            }
            else
            {
                a = p2;
                b = p1;
            }
        }

        public bool Equals(EdgeKey other)
        {
            return a.Equals(other.a) && b.Equals(other.b);
        }

        public override bool Equals(object obj)
        {
            return obj is EdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (a.GetHashCode() * 397) ^ b.GetHashCode();
            }
        }
    }
}