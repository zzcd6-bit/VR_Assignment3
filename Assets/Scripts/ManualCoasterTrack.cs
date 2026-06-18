using UnityEngine;
using System.Collections.Generic;

public class ManualCoasterTrack : MonoBehaviour
{
    [Header("Track Points")]
    public Transform trackPointRoot;
    public bool autoGetPointsFromRoot = true;
    public bool includeInactivePoints = false;
    public Transform[] trackPoints;

    [Header("Generated Object")]
    public string generatedRootName = "GeneratedTrack";
    public bool clearOldGeneratedObjects = true;

    [Header("Track Settings")]
    public float pointSpacing = 0.15f;
    public float trackWidth = 1.4f;
    public float railThickness = 0.08f;

    [Header("Materials")]
    public Material railMaterial;
    public Material woodMaterial;

    [Header("Sleepers")]
    public float sleeperSpacing = 0.4f;
    public Vector3 sleeperScale = new Vector3(1.8f, 0.12f, 0.22f);

    [Header("Gizmos")]
    public bool showGizmos = true;
    public float gizmoPointSize = 0.35f;
    public float gizmoCurveStep = 0.05f;

    private List<Vector3> centerPoints = new List<Vector3>();
    private List<Vector3> rightDirs = new List<Vector3>();
    private Transform generatedRoot;

    void Start()
    {
        RegenerateTrack();
    }

    private void OnValidate()
    {
        GetTrackPointsFromRoot();

        if (pointSpacing < 0.01f)
            pointSpacing = 0.01f;

        if (sleeperSpacing < 0.01f)
            sleeperSpacing = 0.01f;
    }

    [ContextMenu("Regenerate Track")]
    public void RegenerateTrack()
    {
        GetTrackPointsFromRoot();

        if (trackPoints == null || trackPoints.Length < 4)
        {
            Debug.LogError("Need at least 4 track points.");
            return;
        }

        BuildCenterLineWithFixedSpacing();
        BuildRightDirections();
        PrepareGeneratedRoot();

        CreateRails();
        CreateSleepers();
    }

    [ContextMenu("Rename Points By Hierarchy Order")]
    public void RenamePointsByHierarchyOrder()
    {
        if (trackPointRoot == null)
        {
            Debug.LogError("Track Point Root is null.");
            return;
        }

        for (int i = 0; i < trackPointRoot.childCount; i++)
        {
            Transform child = trackPointRoot.GetChild(i);
            child.name = "TrackPoint_" + (i + 1).ToString("00");
        }

        GetTrackPointsFromRoot();

        Debug.Log("Renamed " + trackPointRoot.childCount + " track points by hierarchy order.");
    }

    void GetTrackPointsFromRoot()
    {
        if (!autoGetPointsFromRoot || trackPointRoot == null)
            return;

        List<Transform> points = new List<Transform>();

        for (int i = 0; i < trackPointRoot.childCount; i++)
        {
            Transform child = trackPointRoot.GetChild(i);

            if (!includeInactivePoints && !child.gameObject.activeInHierarchy)
                continue;

            points.Add(child);
        }

        trackPoints = points.ToArray();
    }

    void BuildCenterLineWithFixedSpacing()
    {
        centerPoints.Clear();

        List<Vector3> densePoints = new List<Vector3>();

        for (int i = 0; i < trackPoints.Length - 1; i++)
        {
            if (trackPoints[i] == null || trackPoints[i + 1] == null)
                continue;

            Vector3 p0 = trackPoints[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = trackPoints[i].position;
            Vector3 p2 = trackPoints[i + 1].position;
            Vector3 p3 = trackPoints[Mathf.Min(i + 2, trackPoints.Length - 1)].position;

            int previewSteps = 30;

            for (int j = 0; j < previewSteps; j++)
            {
                float t = j / (float)previewSteps;
                densePoints.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }

        densePoints.Add(trackPoints[trackPoints.Length - 1].position);

        if (densePoints.Count < 2)
            return;

        centerPoints.Add(densePoints[0]);

        float distanceLeft = pointSpacing;

        for (int i = 1; i < densePoints.Count; i++)
        {
            Vector3 previous = densePoints[i - 1];
            Vector3 current = densePoints[i];

            float segmentLength = Vector3.Distance(previous, current);

            if (segmentLength < 0.0001f)
                continue;

            while (segmentLength >= distanceLeft)
            {
                float t = distanceLeft / segmentLength;
                Vector3 newPoint = Vector3.Lerp(previous, current, t);

                centerPoints.Add(newPoint);

                previous = newPoint;
                segmentLength = Vector3.Distance(previous, current);
                distanceLeft = pointSpacing;
            }

            distanceLeft -= segmentLength;
        }

        Vector3 lastPoint = densePoints[densePoints.Count - 1];

        if (Vector3.Distance(centerPoints[centerPoints.Count - 1], lastPoint) > 0.01f)
        {
            centerPoints.Add(lastPoint);
        }
    }

    void BuildRightDirections()
    {
        rightDirs.Clear();

        for (int i = 0; i < centerPoints.Count; i++)
        {
            Vector3 forward = GetForward(i);
            Vector3 flatForward = Vector3.ProjectOnPlane(forward, Vector3.up);

            Vector3 right;

            if (flatForward.sqrMagnitude > 0.001f)
                right = Vector3.Cross(Vector3.up, flatForward.normalized).normalized;
            else
                right = Vector3.right;

            rightDirs.Add(right);
        }
    }

    void CreateRails()
    {
        Vector3[] leftRail = new Vector3[centerPoints.Count];
        Vector3[] rightRail = new Vector3[centerPoints.Count];

        for (int i = 0; i < centerPoints.Count; i++)
        {
            leftRail[i] = centerPoints[i] - rightDirs[i] * trackWidth * 0.5f;
            rightRail[i] = centerPoints[i] + rightDirs[i] * trackWidth * 0.5f;
        }

        CreateLineRenderer("Left Rail", leftRail);
        CreateLineRenderer("Right Rail", rightRail);
    }

    void CreateLineRenderer(string name, Vector3[] points)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(generatedRoot, false);

        LineRenderer line = obj.AddComponent<LineRenderer>();
        line.positionCount = points.Length;
        line.SetPositions(points);

        line.startWidth = railThickness;
        line.endWidth = railThickness;
        line.useWorldSpace = true;

        if (railMaterial != null)
            line.material = railMaterial;
    }

    void CreateSleepers()
    {
        if (centerPoints.Count < 2)
            return;

        float nextSleeperDistance = 0f;
        float travelledDistance = 0f;

        for (int i = 0; i < centerPoints.Count; i++)
        {
            if (i > 0)
            {
                travelledDistance += Vector3.Distance(centerPoints[i - 1], centerPoints[i]);
            }

            if (travelledDistance >= nextSleeperDistance)
            {
                GameObject sleeper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sleeper.name = "Sleeper";
                sleeper.transform.SetParent(generatedRoot, false);

                Vector3 forward = GetForward(i);

                sleeper.transform.position = centerPoints[i];
                sleeper.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
                sleeper.transform.localScale = sleeperScale;

                if (woodMaterial != null)
                    sleeper.GetComponent<Renderer>().material = woodMaterial;

                nextSleeperDistance += sleeperSpacing;
            }
        }
    }

    Vector3 GetForward(int index)
    {
        if (centerPoints.Count < 2)
            return Vector3.forward;

        if (index == 0)
            return (centerPoints[1] - centerPoints[0]).normalized;

        if (index == centerPoints.Count - 1)
            return (centerPoints[index] - centerPoints[index - 1]).normalized;

        Vector3 before = centerPoints[index] - centerPoints[index - 1];
        Vector3 after = centerPoints[index + 1] - centerPoints[index];

        Vector3 forward = before + after;

        if (forward.sqrMagnitude < 0.001f)
            return after.normalized;

        return forward.normalized;
    }

    Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t
        );
    }

    void PrepareGeneratedRoot()
    {
        generatedRoot = transform.Find(generatedRootName);

        if (generatedRoot == null)
        {
            GameObject rootObj = new GameObject(generatedRootName);
            rootObj.transform.SetParent(transform, false);
            generatedRoot = rootObj.transform;
        }

        if (clearOldGeneratedObjects)
            ClearChildren(generatedRoot);
    }

    void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            GameObject child = root.GetChild(i).gameObject;

            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        GetTrackPointsFromRoot();

        if (trackPoints == null || trackPoints.Length < 2)
            return;

        Gizmos.color = Color.yellow;

        foreach (Transform point in trackPoints)
        {
            if (point != null)
                Gizmos.DrawSphere(point.position, gizmoPointSize);
        }

        Gizmos.color = Color.gray;

        for (int i = 0; i < trackPoints.Length - 1; i++)
        {
            if (trackPoints[i] != null && trackPoints[i + 1] != null)
                Gizmos.DrawLine(trackPoints[i].position, trackPoints[i + 1].position);
        }

        if (trackPoints.Length < 4)
            return;

        Gizmos.color = Color.cyan;

        Vector3 previousPoint = trackPoints[0].position;

        for (int i = 0; i < trackPoints.Length - 1; i++)
        {
            if (trackPoints[i] == null || trackPoints[i + 1] == null)
                continue;

            Vector3 p0 = trackPoints[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = trackPoints[i].position;
            Vector3 p2 = trackPoints[i + 1].position;
            Vector3 p3 = trackPoints[Mathf.Min(i + 2, trackPoints.Length - 1)].position;

            for (float t = gizmoCurveStep; t <= 1f; t += gizmoCurveStep)
            {
                Vector3 currentPoint = CatmullRom(p0, p1, p2, p3, t);
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }
        }
    }
}