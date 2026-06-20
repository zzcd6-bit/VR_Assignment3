using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

public class TriMazeGameplayGenerator : EditorWindow
{
    [Header("Shape")]
    private string heightProfile = "8,9,8,9,8,7,8,6,7,5,6,4,5,4,4";
    private float cellSize = 2.2f;

    [Header("Maze Algorithm")]
    private int mazeSeed = 123;
    [Range(0f, 0.35f)]
    private float extraOpeningChance = 0.06f;
    [Range(0.3f, 0.8f)]
    private float keyDoorPathPercent = 0.6f;
    private bool showMainPathPreview = true;
    private bool showKeyPathPreview = true;

    [Header("Path Preview")]
    private float mainPathOffset = 0.12f;   // 黄线偏右一点
    private float keyPathOffset = -0.12f;   // 红线偏左一点
    private float pathLineWidth = 0.08f;
    private float pathLineY = 0.12f;

    [Header("Wall")]
    private float wallHeight = 2.8f;
    private float wallThickness = 0.15f;
    private Material wallMaterial;

    [Header("Door Prefabs")]
    private GameObject normalDoorPrefab;
    private GameObject keyDoorPrefab;
    private GameObject doorButtonPrefab;

    private bool autoScaleDoorToOpening = true;
    private int normalDoorCount = 8;

    private bool createButtonsOnBothSides = true;
    private float doorButtonHeight = 1.25f;
    private float doorButtonForwardOffset = 0.25f;
    private float doorButtonSize = 0.25f;

    private string keyId = "GoldKey";

    [Header("Key Prefab")]
    private GameObject keyPrefab;
    private float keySpawnHeight = 0.6f;

    [Header("Post")]
    private bool generatePosts = true;
    private float postRadius = 0.12f;
    private Material postMaterial;

    [Header("Floor")]
    private bool generateFloor = true;
    private Material floorMaterial;

    [Header("Output")]
    private string parentName = "Generated_Tri_Gameplay_Maze";
    private bool clearPrevious = true;

    private const string GeneratedFolder = "Assets/MazeGameplayGenerated";
    private const float SQRT3_OVER_2 = 0.8660254f;

    private Vector2 scrollPosition;

    [MenuItem("Tools/VR Maze/Gameplay Triangular Maze Generator")]
    public static void OpenWindow()
    {
        GetWindow<TriMazeGameplayGenerator>("Gameplay Maze");
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("VR Gameplay Maze Generator", EditorStyles.boldLabel);

        EditorGUILayout.Space(8);

        GUILayout.Label("Shape", EditorStyles.boldLabel);
        heightProfile = EditorGUILayout.TextArea(heightProfile, GUILayout.Height(45));
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);

        EditorGUILayout.Space(8);

        GUILayout.Label("Algorithm", EditorStyles.boldLabel);
        mazeSeed = EditorGUILayout.IntField("Maze Seed", mazeSeed);
        extraOpeningChance = EditorGUILayout.Slider("Extra Opening Chance", extraOpeningChance, 0f, 0.35f);
        keyDoorPathPercent = EditorGUILayout.Slider("Key Door Path Percent", keyDoorPathPercent, 0.3f, 0.8f);
        showMainPathPreview = EditorGUILayout.Toggle("Show Main Path Preview", showMainPathPreview);
        showKeyPathPreview = EditorGUILayout.Toggle("Show Key Path Preview", showKeyPathPreview);

        mainPathOffset = EditorGUILayout.FloatField("Main Path Offset", mainPathOffset);
        keyPathOffset = EditorGUILayout.FloatField("Key Path Offset", keyPathOffset);
        pathLineWidth = EditorGUILayout.FloatField("Path Line Width", pathLineWidth);
        pathLineY = EditorGUILayout.FloatField("Path Line Y", pathLineY);

        EditorGUILayout.Space(8);

        GUILayout.Label("Wall", EditorStyles.boldLabel);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        wallThickness = EditorGUILayout.FloatField("Wall Thickness", wallThickness);
        wallMaterial = (Material)EditorGUILayout.ObjectField("Wall Material", wallMaterial, typeof(Material), false);

        EditorGUILayout.Space(8);

        GUILayout.Label("Doors", EditorStyles.boldLabel);
        normalDoorPrefab = (GameObject)EditorGUILayout.ObjectField("Normal Door Prefab", normalDoorPrefab, typeof(GameObject), false);
        keyDoorPrefab = (GameObject)EditorGUILayout.ObjectField("Key Door Prefab", keyDoorPrefab, typeof(GameObject), false);
        doorButtonPrefab = (GameObject)EditorGUILayout.ObjectField("Door Button Prefab", doorButtonPrefab, typeof(GameObject), false);

        normalDoorCount = EditorGUILayout.IntSlider("Normal Door Count", normalDoorCount, 0, 30);
        autoScaleDoorToOpening = EditorGUILayout.Toggle("Auto Scale Door To Opening", autoScaleDoorToOpening);

        createButtonsOnBothSides = EditorGUILayout.Toggle("Buttons On Both Sides", createButtonsOnBothSides);
        doorButtonHeight = EditorGUILayout.FloatField("Door Button Height", doorButtonHeight);
        doorButtonForwardOffset = EditorGUILayout.FloatField("Door Button Forward Offset", doorButtonForwardOffset);
        doorButtonSize = EditorGUILayout.FloatField("Door Button Size", doorButtonSize);

        keyId = EditorGUILayout.TextField("Key Id", keyId);

        EditorGUILayout.Space(8);

        GUILayout.Label("Key", EditorStyles.boldLabel);
        keyPrefab = (GameObject)EditorGUILayout.ObjectField("Key Prefab", keyPrefab, typeof(GameObject), false);
        keySpawnHeight = EditorGUILayout.FloatField("Key Spawn Height", keySpawnHeight);

        EditorGUILayout.Space(8);

        GUILayout.Label("Post", EditorStyles.boldLabel);
        generatePosts = EditorGUILayout.Toggle("Generate Posts", generatePosts);
        postRadius = EditorGUILayout.FloatField("Post Radius", postRadius);
        postMaterial = (Material)EditorGUILayout.ObjectField("Post Material", postMaterial, typeof(Material), false);

        EditorGUILayout.Space(8);

        GUILayout.Label("Floor", EditorStyles.boldLabel);
        generateFloor = EditorGUILayout.Toggle("Generate Floor", generateFloor);
        floorMaterial = (Material)EditorGUILayout.ObjectField("Floor Material", floorMaterial, typeof(Material), false);

        EditorGUILayout.Space(8);

        GUILayout.Label("Output", EditorStyles.boldLabel);
        parentName = EditorGUILayout.TextField("Parent Name", parentName);
        clearPrevious = EditorGUILayout.Toggle("Clear Previous", clearPrevious);

        EditorGUILayout.Space(12);

        EditorGUILayout.HelpBox(
            "算法会自动保证：Start 到 Exit 有路；Key Door 在主路径关键节点；Key 一定在 Key Door 前面的可到达区域。",
            MessageType.Info
        );

        if (GUILayout.Button("Generate Gameplay Maze", GUILayout.Height(36)))
        {
            GenerateMaze();
        }

        EditorGUILayout.EndScrollView();
    }

    private void GenerateMaze()
    {
        int[] heights = ParseHeightProfile(heightProfile);

        if (heights == null || heights.Length <= 1)
        {
            Debug.LogError("Height Profile 无效。");
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
        Undo.RegisterCreatedObjectUndo(parent, "Generate Gameplay Maze");

        Material wallMat = wallMaterial != null ? wallMaterial : GetOrCreateWallMaterial();
        Material pMat = postMaterial != null ? postMaterial : GetOrCreatePostMaterial();
        Material fMat = floorMaterial != null ? floorMaterial : GetOrCreateFloorMaterial();

        Dictionary<Vector2Int, Vector3> nodes = GenerateNodes(heights);
        HashSet<EdgeKey> fullEdges = GenerateFullEdges(nodes, heights);
        List<TriangleCell> cells = GenerateTriangleCells(nodes, fullEdges);
        Dictionary<EdgeKey, EdgeSegment> edgeSegments = BuildEdgeSegments(cells);
        List<NeighborLink>[] neighborLinks = BuildNeighborLinks(cells.Count, edgeSegments);

        if (cells.Count <= 2)
        {
            Debug.LogError("Cell 数量太少，无法生成玩法迷宫。");
            DestroyImmediate(parent);
            return;
        }

        HashSet<int> boundaryCells = GetBoundaryCells(edgeSegments);

        int startCellId = PickStartCell(cells, boundaryCells);
        HashSet<EdgeKey> openings = CarveMazeByDFS(cells.Count, startCellId, neighborLinks, mazeSeed);

        AddExtraOpenings(openings, edgeSegments, extraOpeningChance, mazeSeed + 999);

        int exitCellId = PickFarthestBoundaryCell(startCellId, cells, boundaryCells, neighborLinks, openings);

        List<int> mainPath = FindPath(startCellId, exitCellId, neighborLinks, openings);

        if (mainPath.Count < 4)
        {
            Debug.LogError("主路径太短，建议调大 Height Profile 或 Cell Size。");
            DestroyImmediate(parent);
            return;
        }

        int doorPathIndex = Mathf.Clamp(
            Mathf.RoundToInt(mainPath.Count * keyDoorPathPercent),
            1,
            mainPath.Count - 2
        );

        EdgeKey keyDoorEdge;

        if (!TryGetSharedEdge(mainPath[doorPathIndex], mainPath[doorPathIndex + 1], neighborLinks, out keyDoorEdge))
        {
            Debug.LogError("无法找到 Key Door 的边。");
            DestroyImmediate(parent);
            return;
        }

        openings.Add(keyDoorEdge);

        HashSet<int> startSideCells = GetReachableCells(startCellId, neighborLinks, openings, keyDoorEdge);
        ForceKeyDoorAsCriticalEdge(openings, edgeSegments, startSideCells, keyDoorEdge);

        mainPath = FindPath(startCellId, exitCellId, neighborLinks, openings);

        int keyCellId = PickKeyCell(startCellId, mainPath, doorPathIndex, startSideCells, neighborLinks, openings, keyDoorEdge);
        Vector3 keyPosition = cells[keyCellId].center + Vector3.up * keySpawnHeight;

        List<int> keyPath = FindPathAvoidingEdge(
            startCellId,
            keyCellId,
            neighborLinks,
            openings,
            keyDoorEdge
        );

        EdgeKey entranceEdge;
        EdgeKey exitEdge;

        if (TryPickBoundaryEdgeForCell(startCellId, edgeSegments, nodes, true, out entranceEdge))
        {
            openings.Add(entranceEdge);
        }

        if (TryPickBoundaryEdgeForCell(exitCellId, edgeSegments, nodes, false, out exitEdge))
        {
            openings.Add(exitEdge);
        }

        HashSet<EdgeKey> normalDoorEdges = PickNormalDoorEdges(
            openings,
            edgeSegments,
            keyDoorEdge,
            normalDoorCount,
            mazeSeed + 2026
        );

        foreach (KeyValuePair<EdgeKey, EdgeSegment> pair in edgeSegments)
        {
            EdgeKey edge = pair.Key;

            Vector3 a = nodes[edge.a];
            Vector3 b = nodes[edge.b];

            if (edge.Equals(keyDoorEdge))
            {
                CreateKeyDoor(a, b, parent.transform);
                continue;
            }

            if (normalDoorEdges.Contains(edge))
            {
                CreateNormalDoor(a, b, parent.transform);
                continue;
            }

            if (openings.Contains(edge))
            {
                continue;
            }

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

        CreateKey(keyPosition, parent.transform);
        CreateMarkers(cells[startCellId].center, cells[exitCellId].center, parent.transform);

        if (showMainPathPreview)
        {
            CreateOffsetPathPreview(
                "Main_Path_Preview",
                mainPath,
                cells,
                parent.transform,
                Color.yellow,
                mainPathOffset
            );
        }

        if (showKeyPathPreview)
        {
            CreateOffsetPathPreview(
                "Key_Path_Preview",
                keyPath,
                cells,
                parent.transform,
                Color.red,
                keyPathOffset
            );
        }

        Selection.activeGameObject = parent;

        Debug.Log("Gameplay Maze generated. Key door is critical and key is reachable before the locked door.");
    }

    private List<int> FindPathAvoidingEdge(
    int start,
    int goal,
    List<NeighborLink>[] links,
    HashSet<EdgeKey> openings,
    EdgeKey blockedEdge)
    {
        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == goal)
            {
                break;
            }

            foreach (NeighborLink link in links[current])
            {
                if (!openings.Contains(link.edge))
                {
                    continue;
                }

                if (link.edge.Equals(blockedEdge))
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

        if (!visited.Contains(goal))
        {
            return path;
        }

        int node = goal;
        path.Add(node);

        while (node != start)
        {
            node = cameFrom[node];
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    private void CreateOffsetPathPreview(
    string objectName,
    List<int> path,
    List<TriangleCell> cells,
    Transform parent,
    Color color,
    float lateralOffset)
    {
        if (path == null || path.Count <= 1)
        {
            return;
        }

        GameObject lineObject = new GameObject(objectName);
        lineObject.transform.SetParent(parent);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = path.Count;
        line.widthMultiplier = pathLineWidth;
        line.useWorldSpace = true;

        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        line.sharedMaterial = mat;

        Vector3[] offsetPositions = BuildOffsetPathPositions(path, cells, lateralOffset);

        for (int i = 0; i < offsetPositions.Length; i++)
        {
            line.SetPosition(i, offsetPositions[i]);
        }
    }

    private Vector3[] BuildOffsetPathPositions(
    List<int> path,
    List<TriangleCell> cells,
    float lateralOffset)
    {
        Vector3[] positions = new Vector3[path.Count];

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 current = cells[path[i]].center;

            Vector3 prev = i > 0 ? cells[path[i - 1]].center : current;
            Vector3 next = i < path.Count - 1 ? cells[path[i + 1]].center : current;

            Vector3 tangent;

            if (i == 0)
            {
                tangent = (next - current).normalized;
            }
            else if (i == path.Count - 1)
            {
                tangent = (current - prev).normalized;
            }
            else
            {
                tangent = ((next - current).normalized + (current - prev).normalized).normalized;
            }

            if (tangent.sqrMagnitude < 0.0001f)
            {
                tangent = Vector3.forward;
            }

            tangent.y = 0f;
            tangent.Normalize();

            Vector3 lateral = Vector3.Cross(Vector3.up, tangent).normalized;

            Vector3 offsetPosition = current + lateral * lateralOffset;
            offsetPosition.y = pathLineY;

            positions[i] = offsetPosition;
        }

        return positions;
    }

    private int[] ParseHeightProfile(string profile)
    {
        string[] parts = profile.Split(',', ';', ' ', '\n', '\t');
        List<int> values = new List<int>();

        foreach (string part in parts)
        {
            int value;

            if (int.TryParse(part, out value))
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
                nodes.Add(
                    new Vector2Int(col, row),
                    new Vector3(col * xSpacing, 0f, row * zSpacing + zOffset)
                );
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

    private int PickFarthestBoundaryCell(
        int start,
        List<TriangleCell> cells,
        HashSet<int> boundaryCells,
        List<NeighborLink>[] links,
        HashSet<EdgeKey> openings)
    {
        Dictionary<int, int> distance = GetDistances(start, links, openings, default(EdgeKey), false);

        int bestId = start;
        int bestDistance = -1;

        foreach (int id in boundaryCells)
        {
            if (distance.ContainsKey(id) && distance[id] > bestDistance)
            {
                bestDistance = distance[id];
                bestId = id;
            }
        }

        return bestId;
    }

    private HashSet<EdgeKey> CarveMazeByDFS(
        int cellCount,
        int start,
        List<NeighborLink>[] links,
        int seed)
    {
        HashSet<EdgeKey> openings = new HashSet<EdgeKey>();
        bool[] visited = new bool[cellCount];

        System.Random random = new System.Random(seed);
        Stack<int> stack = new Stack<int>();

        visited[start] = true;
        stack.Push(start);

        while (stack.Count > 0)
        {
            int current = stack.Peek();
            List<NeighborLink> candidates = new List<NeighborLink>();

            foreach (NeighborLink link in links[current])
            {
                if (!visited[link.cellId])
                {
                    candidates.Add(link);
                }
            }

            if (candidates.Count == 0)
            {
                stack.Pop();
                continue;
            }

            NeighborLink chosen = candidates[random.Next(candidates.Count)];

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

    private List<int> FindPath(
        int start,
        int goal,
        List<NeighborLink>[] links,
        HashSet<EdgeKey> openings)
    {
        Queue<int> queue = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();
        Dictionary<int, int> cameFrom = new Dictionary<int, int>();

        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            if (current == goal)
            {
                break;
            }

            foreach (NeighborLink link in links[current])
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

        if (!visited.Contains(goal))
        {
            return path;
        }

        int node = goal;
        path.Add(node);

        while (node != start)
        {
            node = cameFrom[node];
            path.Add(node);
        }

        path.Reverse();
        return path;
    }

    private bool TryGetSharedEdge(
        int cellA,
        int cellB,
        List<NeighborLink>[] links,
        out EdgeKey edge)
    {
        foreach (NeighborLink link in links[cellA])
        {
            if (link.cellId == cellB)
            {
                edge = link.edge;
                return true;
            }
        }

        edge = default(EdgeKey);
        return false;
    }

    private HashSet<int> GetReachableCells(
        int start,
        List<NeighborLink>[] links,
        HashSet<EdgeKey> openings,
        EdgeKey blockedEdge)
    {
        Dictionary<int, int> distances = GetDistances(start, links, openings, blockedEdge, true);
        return new HashSet<int>(distances.Keys);
    }

    private Dictionary<int, int> GetDistances(
        int start,
        List<NeighborLink>[] links,
        HashSet<EdgeKey> openings,
        EdgeKey blockedEdge,
        bool useBlockedEdge)
    {
        Dictionary<int, int> distance = new Dictionary<int, int>();
        Queue<int> queue = new Queue<int>();

        distance[start] = 0;
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();

            foreach (NeighborLink link in links[current])
            {
                if (!openings.Contains(link.edge))
                {
                    continue;
                }

                if (useBlockedEdge && link.edge.Equals(blockedEdge))
                {
                    continue;
                }

                if (distance.ContainsKey(link.cellId))
                {
                    continue;
                }

                distance[link.cellId] = distance[current] + 1;
                queue.Enqueue(link.cellId);
            }
        }

        return distance;
    }

    private void ForceKeyDoorAsCriticalEdge(
        HashSet<EdgeKey> openings,
        Dictionary<EdgeKey, EdgeSegment> edgeSegments,
        HashSet<int> startSideCells,
        EdgeKey keyDoorEdge)
    {
        List<EdgeKey> removeList = new List<EdgeKey>();

        foreach (EdgeKey edge in openings)
        {
            if (edge.Equals(keyDoorEdge))
            {
                continue;
            }

            if (!edgeSegments.ContainsKey(edge))
            {
                continue;
            }

            EdgeSegment segment = edgeSegments[edge];

            if (segment.cellIds.Count != 2)
            {
                continue;
            }

            bool aStartSide = startSideCells.Contains(segment.cellIds[0]);
            bool bStartSide = startSideCells.Contains(segment.cellIds[1]);

            if (aStartSide != bStartSide)
            {
                removeList.Add(edge);
            }
        }

        foreach (EdgeKey edge in removeList)
        {
            openings.Remove(edge);
        }

        openings.Add(keyDoorEdge);
    }

    private int PickKeyCell(
        int startCell,
        List<int> mainPath,
        int doorPathIndex,
        HashSet<int> startSideCells,
        List<NeighborLink>[] links,
        HashSet<EdgeKey> openings,
        EdgeKey keyDoorEdge)
    {
        Dictionary<int, int> distances = GetDistances(startCell, links, openings, keyDoorEdge, true);

        HashSet<int> beforeDoorPath = new HashSet<int>();

        for (int i = 0; i <= doorPathIndex; i++)
        {
            beforeDoorPath.Add(mainPath[i]);
        }

        int bestBranchCell = -1;
        int bestBranchDistance = -1;

        foreach (int cellId in startSideCells)
        {
            if (!distances.ContainsKey(cellId))
            {
                continue;
            }

            if (beforeDoorPath.Contains(cellId))
            {
                continue;
            }

            if (distances[cellId] > bestBranchDistance)
            {
                bestBranchDistance = distances[cellId];
                bestBranchCell = cellId;
            }
        }

        if (bestBranchCell != -1)
        {
            return bestBranchCell;
        }

        int bestCell = startCell;
        int bestDistance = -1;

        foreach (int cellId in startSideCells)
        {
            if (!distances.ContainsKey(cellId))
            {
                continue;
            }

            if (cellId == startCell)
            {
                continue;
            }

            if (distances[cellId] > bestDistance)
            {
                bestDistance = distances[cellId];
                bestCell = cellId;
            }
        }

        return bestCell;
    }

    private bool TryPickBoundaryEdgeForCell(
        int cellId,
        Dictionary<EdgeKey, EdgeSegment> edgeSegments,
        Dictionary<Vector2Int, Vector3> nodes,
        bool isEntrance,
        out EdgeKey result)
    {
        result = default(EdgeKey);

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

    private HashSet<EdgeKey> PickNormalDoorEdges(
        HashSet<EdgeKey> openings,
        Dictionary<EdgeKey, EdgeSegment> edgeSegments,
        EdgeKey keyDoorEdge,
        int count,
        int seed)
    {
        HashSet<EdgeKey> result = new HashSet<EdgeKey>();
        List<EdgeKey> candidates = new List<EdgeKey>();

        foreach (EdgeKey edge in openings)
        {
            if (edge.Equals(keyDoorEdge))
            {
                continue;
            }

            if (!edgeSegments.ContainsKey(edge))
            {
                continue;
            }

            if (edgeSegments[edge].cellIds.Count != 2)
            {
                continue;
            }

            candidates.Add(edge);
        }

        Shuffle(candidates, seed);

        int finalCount = Mathf.Min(count, candidates.Count);

        for (int i = 0; i < finalCount; i++)
        {
            result.Add(candidates[i]);
        }

        return result;
    }

    private void CreateWall(Vector3 start, Vector3 end, Transform parent, Material material)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        Vector3 center = (start + end) * 0.5f;
        center.y = wallHeight * 0.5f;

        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Maze_Wall";
        wall.transform.SetParent(parent);
        wall.transform.position = center;
        wall.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        wall.transform.localScale = new Vector3(wallThickness, wallHeight, length);

        if (material != null)
        {
            wall.GetComponent<Renderer>().sharedMaterial = material;
        }

        Collider col = wall.GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = false;
        }
    }

    private void CreateNormalDoor(Vector3 start, Vector3 end, Transform parent)
    {
        GameObject door = CreateDoorObject(start, end, parent, normalDoorPrefab, "Normal_Push_Door");

        MazePushDoor pushDoor = door.GetComponent<MazePushDoor>();

        if (pushDoor == null)
        {
            pushDoor = door.AddComponent<MazePushDoor>();
        }

        pushDoor.doorPart = door.transform;

        // 普通门点击按钮后向上升起
        pushDoor.openMode = MazePushDoor.DoorOpenMode.SlideUp;
        pushDoor.slideDistance = wallHeight;
        pushDoor.openSpeed = 3f;
        pushDoor.closeAgain = false;

        CreateDoorButtons(start, end, door.transform, pushDoor, null, "Normal_Door_Button");
    }

    private void CreateKeyDoor(Vector3 start, Vector3 end, Transform parent)
    {
        GameObject door = CreateDoorObject(start, end, parent, keyDoorPrefab, "Key_Locked_Door");

        MazeKeyDoor keyDoor = door.GetComponent<MazeKeyDoor>();

        if (keyDoor == null)
        {
            keyDoor = door.AddComponent<MazeKeyDoor>();
        }

        keyDoor.doorPart = door.transform;
        keyDoor.requiredKeyId = keyId;

        // 钥匙门点击按钮后向上升起
        keyDoor.openMode = MazeKeyDoor.KeyDoorOpenMode.SlideUp;
        keyDoor.openDistance = wallHeight;
        keyDoor.openSpeed = 3f;

        CreateDoorButtons(start, end, door.transform, null, keyDoor, "Key_Door_Button");
    }

    private void CreateDoorButtons(
    Vector3 start,
    Vector3 end,
    Transform doorTransform,
    MazePushDoor pushDoor,
    MazeKeyDoor keyDoor,
    string buttonName)
    {
        Vector3 direction = (end - start).normalized;
        Vector3 normal = Vector3.Cross(Vector3.up, direction).normalized;

        CreateSingleDoorButton(
            start,
            end,
            doorTransform,
            normal,
            pushDoor,
            keyDoor,
            buttonName + "_Front"
        );

        if (createButtonsOnBothSides)
        {
            CreateSingleDoorButton(
                start,
                end,
                doorTransform,
                -normal,
                pushDoor,
                keyDoor,
                buttonName + "_Back"
            );
        }
    }

    private void CreateSingleDoorButton(
        Vector3 start,
        Vector3 end,
        Transform doorTransform,
        Vector3 normal,
        MazePushDoor pushDoor,
        MazeKeyDoor keyDoor,
        string buttonName)
    {
        Vector3 center = (start + end) * 0.5f;
        center.y = doorButtonHeight;

        Vector3 position = center + normal * (wallThickness * 0.5f + doorButtonForwardOffset);

        GameObject buttonObject;

        if (doorButtonPrefab != null)
        {
            buttonObject = (GameObject)PrefabUtility.InstantiatePrefab(doorButtonPrefab);
            buttonObject.name = buttonName;
        }
        else
        {
            buttonObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            buttonObject.name = buttonName;
            buttonObject.transform.localScale = Vector3.one * doorButtonSize;
        }

        buttonObject.transform.position = position;
        buttonObject.transform.rotation = Quaternion.LookRotation(normal, Vector3.up);

        // 让按钮跟着门一起上升
        buttonObject.transform.SetParent(doorTransform, true);

        Collider col = buttonObject.GetComponentInChildren<Collider>();

        if (col == null)
        {
            BoxCollider box = buttonObject.AddComponent<BoxCollider>();
            box.isTrigger = false;
        }
        else
        {
            col.isTrigger = false;
        }

        MazeDoorButton button = buttonObject.GetComponent<MazeDoorButton>();

        if (button == null)
        {
            button = buttonObject.AddComponent<MazeDoorButton>();
        }

        button.pushDoor = pushDoor;
        button.keyDoor = keyDoor;
    }

    private GameObject CreateDoorObject(
    Vector3 start,
    Vector3 end,
    Transform parent,
    GameObject prefab,
    string objectName)
    {
        Vector3 direction = end - start;
        float length = direction.magnitude;

        Vector3 center = (start + end) * 0.5f;
        center.y = wallHeight * 0.5f;

        // 创建门的根物体，Root 永远保持 Scale 1,1,1
        GameObject doorRoot = new GameObject(objectName);
        doorRoot.transform.SetParent(parent);
        doorRoot.transform.position = center;
        doorRoot.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        doorRoot.transform.localScale = Vector3.one;

        // 生成门的视觉模型，Prefab 本身保持 Scale 1,1,1
        GameObject doorVisual;

        if (prefab != null)
        {
            doorVisual = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            doorVisual.name = objectName + "_Visual";
            doorVisual.transform.SetParent(doorRoot.transform);
            doorVisual.transform.localPosition = Vector3.zero;
            doorVisual.transform.localRotation = Quaternion.identity;
            doorVisual.transform.localScale = Vector3.one;
        }
        else
        {
            // 没有 prefab 时，自动生成一个简单门模型
            doorVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorVisual.name = objectName + "_Visual";
            doorVisual.transform.SetParent(doorRoot.transform);
            doorVisual.transform.localPosition = Vector3.zero;
            doorVisual.transform.localRotation = Quaternion.identity;
            doorVisual.transform.localScale = new Vector3(wallThickness * 1.3f, wallHeight, length);
        }

        // 创建一个隐形碰撞体，用来保证门能完整堵住通道
        GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blocker.name = objectName + "_Blocker";
        blocker.transform.SetParent(doorRoot.transform);
        blocker.transform.localPosition = Vector3.zero;
        blocker.transform.localRotation = Quaternion.identity;
        blocker.transform.localScale = new Vector3(wallThickness * 1.3f, wallHeight, length);

        MeshRenderer blockerRenderer = blocker.GetComponent<MeshRenderer>();

        if (blockerRenderer != null)
        {
            blockerRenderer.enabled = false;
        }

        BoxCollider blockerCollider = blocker.GetComponent<BoxCollider>();

        if (blockerCollider != null)
        {
            blockerCollider.isTrigger = false;
        }

        return doorRoot;
    }

    private void CreateKey(Vector3 position, Transform parent)
    {
        GameObject keyObject;

        if (keyPrefab != null)
        {
            keyObject = (GameObject)PrefabUtility.InstantiatePrefab(keyPrefab);
            keyObject.name = "Maze_Key_" + keyId;
        }
        else
        {
            keyObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            keyObject.name = "Maze_Key_" + keyId;
            keyObject.transform.localScale = Vector3.one * 0.35f;
        }

        keyObject.transform.SetParent(parent);
        keyObject.transform.position = position;

        MazeKeyPickup pickup = keyObject.GetComponent<MazeKeyPickup>();

        if (pickup == null)
        {
            pickup = keyObject.AddComponent<MazeKeyPickup>();
        }

        pickup.keyId = keyId;
        pickup.pickupOnTouch = false;

        Collider keyCollider = keyObject.GetComponentInChildren<Collider>();

        if (keyCollider == null)
        {
            SphereCollider sphere = keyObject.AddComponent<SphereCollider>();
            sphere.radius = 0.6f;
            sphere.isTrigger = false;
        }
        else
        {
            keyCollider.isTrigger = false;
        }
    }

    private void CreatePost(Vector3 position, Transform parent, Material material)
    {
        GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        post.name = "Maze_Post";
        post.transform.SetParent(parent);
        post.transform.position = new Vector3(position.x, wallHeight * 0.5f, position.z);
        post.transform.localScale = new Vector3(postRadius * 2f, wallHeight * 0.5f, postRadius * 2f);

        if (material != null)
        {
            post.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private void CreateFloor(Dictionary<Vector2Int, Vector3> nodes, Transform parent, Material material)
    {
        GetBounds(nodes, out float minX, out float maxX, out float minZ, out float maxZ);

        float padding = cellSize * 1.5f;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Maze_Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3((minX + maxX) * 0.5f, -0.05f, (minZ + maxZ) * 0.5f);
        floor.transform.localScale = new Vector3(maxX - minX + padding * 2f, 0.1f, maxZ - minZ + padding * 2f);

        if (material != null)
        {
            floor.GetComponent<Renderer>().sharedMaterial = material;
        }
    }

    private void CreateMarkers(Vector3 start, Vector3 exit, Transform parent)
    {
        CreateMarker("START_Position", start, parent, Color.green);
        CreateMarker("EXIT_Position", exit, parent, Color.red);
    }

    private void CreateMarker(string name, Vector3 position, Transform parent, Color color)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.SetParent(parent);
        marker.transform.position = new Vector3(position.x, 0.03f, position.z);
        marker.transform.localScale = new Vector3(0.6f, 0.03f, 0.6f);

        Collider col = marker.GetComponent<Collider>();

        if (col != null)
        {
            col.enabled = false;
        }

        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        marker.GetComponent<Renderer>().sharedMaterial = mat;
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

    private void Shuffle<T>(List<T> list, int seed)
    {
        System.Random random = new System.Random(seed);

        for (int i = 0; i < list.Count; i++)
        {
            int randomIndex = random.Next(i, list.Count);
            T temp = list[i];
            list[i] = list[randomIndex];
            list[randomIndex] = temp;
        }
    }

    private Material GetOrCreateWallMaterial()
    {
        EnsureGeneratedFolder();

        string path = GeneratedFolder + "/M_MazeWall_Auto.mat";
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
        mat.name = "M_MazeWall_Auto";
        SetMaterialColor(mat, new Color(0.28f, 0.28f, 0.3f, 1f));

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private Material GetOrCreatePostMaterial()
    {
        EnsureGeneratedFolder();

        string path = GeneratedFolder + "/M_MazePost_Auto.mat";
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
        mat.name = "M_MazePost_Auto";
        SetMaterialColor(mat, new Color(0.08f, 0.08f, 0.09f, 1f));

        AssetDatabase.CreateAsset(mat, path);
        AssetDatabase.SaveAssets();

        return mat;
    }

    private Material GetOrCreateFloorMaterial()
    {
        EnsureGeneratedFolder();

        string path = GeneratedFolder + "/M_MazeFloor_Auto.mat";
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
        mat.name = "M_MazeFloor_Auto";
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
            AssetDatabase.CreateFolder("Assets", "MazeGameplayGenerated");
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