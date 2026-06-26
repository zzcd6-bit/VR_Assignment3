using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class VRCoasterRide : MonoBehaviour
{
    [Header("VR Seat Rig")]
    [Tooltip("这里拖 XR Origin，或者拖 XR Origin 外面的父物体。不要拖 Main Camera。")]
    public Transform rideTarget;

    [Header("VR Camera")]
    [Tooltip("这里拖 XR Origin 里面的 Main Camera。VR 模板一定要拖这个。")]
    public Transform xrCamera;

    [Header("Track Point Root")]
    public Transform trackPointRoot;
    public bool autoGetPointsFromRoot = true;
    public bool includeInactivePoints = false;

    [Header("Track Points")]
    public Transform[] trackPoints;

    [Header("Path Settings")]
    public int samplesPerSegment = 30;
    public bool loop = false;
    public bool startOnPlay = false;

    [Header("Speed Settings")]
    public float startSpeed = 4f;
    public float minSpeed = 2f;
    public float maxSpeed = 12f;
    public float acceleration = 8f;
    public float slopeInfluence = 10f;

    [Header("Start / End TrackPoint Speed")]
    public int startTrackPointCount = 3;
    public int endTrackPointCount = 3;
    public float startAreaSpeed = 3f;
    public float endAreaSpeed = 2f;

    [Header("Seat Offset")]
    public Vector3 seatOffset = new Vector3(0, 0.6f, 0);

    [Header("Rotation Settings")]
    public bool rotateSeatForward = true;
    public float rotateSmoothSpeed = 5f;

    [Tooltip("如果你的 TrackPoint 有旋转角度，打开这个可以让玩家跟着轨道倾斜。")]
    public bool useTrackPointUpForRoll = true;

    [Header("Start / Replay Objects")]
    public GameObject startRideObject;
    public GameObject replayObject;
    public GameObject cartModel;

    [Header("Score UI")]
    public int score = 0;

    [Header("Coins")]
    public VRCoinScore[] coins;

    private List<Vector3> ridePath = new List<Vector3>();
    private List<int> ridePathSegmentIndex = new List<int>();

    private int currentIndex = 0;
    private bool isRiding = false;
    private float currentSpeed;

    [Header("Panel")]
    public GameObject StartPanel;
    public GameObject EndPanel;

    [Header("End Panel Score Text")]
    public TMP_Text endScoreText;
    public string endScorePrefix = "Final Score: ";

    public bool IsRiding
    {
        get { return isRiding; }
    }

    void Start()
    {
        GetTrackPointsFromRoot();
        GenerateRidePath();

        if (rideTarget == null)
        {
            Debug.LogWarning("rideTarget 没有拖进去。VR 模板里请拖 XR Origin 或 XR Origin 的父物体。");
            return;
        }

        if (xrCamera == null)
        {
            Debug.LogWarning("xrCamera 没有拖进去。VR 模板里请拖 XR Origin 里面的 Main Camera。");
        }

        if (ridePath.Count > 0)
        {
            ResetRidePosition();
        }

        if (StartPanel != null)
        {
            StartPanel.SetActive(true);
        }

        if (EndPanel != null)
        {
            EndPanel.SetActive(false);
        }

        currentSpeed = startSpeed;
        isRiding = startOnPlay;

        if (coins == null || coins.Length == 0)
        {
            coins = FindObjectsOfType<VRCoinScore>(true);
        }

        score = 0;
        UpdateScoreUI();

        if (startRideObject != null)
        {
            startRideObject.SetActive(!startOnPlay);
        }

        if (replayObject != null)
        {
            replayObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!isRiding || ridePath.Count < 2 || rideTarget == null)
        {
            return;
        }

        MoveAlongPath();
    }

    public void StartRide()
    {
        if (StartPanel != null)
        {
            StartPanel.SetActive(false);
        }

        if (EndPanel != null)
        {
            EndPanel.SetActive(false);
        }

        if (ridePath.Count < 2 || rideTarget == null)
        {
            Debug.LogWarning("无法开始过山车，请检查 rideTarget 或 trackPoints。");
            return;
        }

        isRiding = true;

        if (startRideObject != null)
        {
            startRideObject.SetActive(false);
        }

        if (replayObject != null)
        {
            replayObject.SetActive(false);
        }

        SetCartModelActive(true);
        Debug.Log("Ride Start");
    }

    public void RestartRide()
    {
        ResetRidePosition();
        ResetScore();
        ResetCoins();
        SetCartModelActive(true);
        StartRide();
    }

    public void SetCartModelActive(bool active)
    {
        if (cartModel != null)
        {
            cartModel.SetActive(active);
        }
    }

    public void ResetRidePosition()
    {
        if (ridePath.Count == 0 || rideTarget == null)
        {
            return;
        }

        currentIndex = 0;
        currentSpeed = startSpeed;

        Vector3 startSeatPosition = ridePath[0] + seatOffset;

        if (rotateSeatForward && ridePath.Count > 1)
        {
            Vector3 direction = ridePath[1] - ridePath[0];

            if (direction.sqrMagnitude > 0.001f)
            {
                Vector3 up = GetPathUp();
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, up);

                rideTarget.rotation = targetRotation;
            }
        }

        MoveVRCameraToSeatPosition(startSeatPosition);
    }

    public void AddScore(int value)
    {
        score += value;
        UpdateScoreUI();
    }

    void ResetScore()
    {
        score = 0;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        Debug.Log("Current Score: " + score);

        if (endScoreText != null)
        {
            endScoreText.text = endScorePrefix + score.ToString();
        }
    }

    void ResetCoins()
    {
        if (coins == null)
        {
            return;
        }

        foreach (VRCoinScore coin in coins)
        {
            if (coin != null)
            {
                coin.ResetCoin();
            }
        }
    }

    void EndRide()
    {
        isRiding = false;

        if (endScoreText != null)
        {
            endScoreText.text = endScorePrefix + score.ToString();
        }

        if (EndPanel != null)
        {
            EndPanel.SetActive(true);
        }

        if (replayObject != null)
        {
            replayObject.SetActive(true);
        }

        Debug.Log("Ride End. Final Score: " + score);
    }

    void GetTrackPointsFromRoot()
    {
        if (!autoGetPointsFromRoot || trackPointRoot == null)
        {
            return;
        }

        List<Transform> points = new List<Transform>();

        for (int i = 0; i < trackPointRoot.childCount; i++)
        {
            Transform child = trackPointRoot.GetChild(i);

            if (!includeInactivePoints && !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            points.Add(child);
        }

        trackPoints = points.ToArray();
    }

    void GenerateRidePath()
    {
        ridePath.Clear();
        ridePathSegmentIndex.Clear();

        if (trackPoints == null || trackPoints.Length < 4)
        {
            Debug.LogWarning("TrackPoints 至少需要 4 个点。");
            return;
        }

        for (int i = 0; i < trackPoints.Length - 1; i++)
        {
            if (trackPoints[i] == null || trackPoints[i + 1] == null)
            {
                continue;
            }

            Vector3 p0 = trackPoints[Mathf.Max(i - 1, 0)].position;
            Vector3 p1 = trackPoints[i].position;
            Vector3 p2 = trackPoints[i + 1].position;
            Vector3 p3 = trackPoints[Mathf.Min(i + 2, trackPoints.Length - 1)].position;

            for (int j = 0; j < samplesPerSegment; j++)
            {
                float t = j / (float)samplesPerSegment;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);

                ridePath.Add(point);
                ridePathSegmentIndex.Add(i);
            }
        }

        ridePath.Add(trackPoints[trackPoints.Length - 1].position);
        ridePathSegmentIndex.Add(trackPoints.Length - 2);
    }

    void MoveAlongPath()
    {
        if (currentIndex >= ridePath.Count - 1)
        {
            if (loop)
            {
                ResetRidePosition();
                isRiding = true;
            }
            else
            {
                EndRide();
            }

            return;
        }

        Vector3 currentPoint = ridePath[currentIndex];
        Vector3 nextPoint = ridePath[currentIndex + 1];

        UpdateSpeed(currentPoint, nextPoint);

        Vector3 targetSeatPosition = nextPoint + seatOffset;

        Vector3 currentSeatPosition = GetCurrentSeatPosition();

        Vector3 newSeatPosition = Vector3.MoveTowards(
            currentSeatPosition,
            targetSeatPosition,
            currentSpeed * Time.deltaTime
        );

        if (rotateSeatForward)
        {
            Vector3 direction = nextPoint - currentPoint;

            if (direction.sqrMagnitude > 0.001f)
            {
                Vector3 up = GetPathUp();

                Quaternion targetRotation = Quaternion.LookRotation(
                    direction.normalized,
                    up
                );

                rideTarget.rotation = Quaternion.Slerp(
                    rideTarget.rotation,
                    targetRotation,
                    Time.deltaTime * rotateSmoothSpeed
                );
            }
        }

        MoveVRCameraToSeatPosition(newSeatPosition);

        if (Vector3.Distance(GetCurrentSeatPosition(), targetSeatPosition) < 0.05f)
        {
            currentIndex++;
        }
    }

    Vector3 GetCurrentSeatPosition()
    {
        if (xrCamera != null)
        {
            return xrCamera.position;
        }

        return rideTarget.position;
    }

    void MoveVRCameraToSeatPosition(Vector3 targetSeatPosition)
    {
        if (xrCamera != null)
        {
            Vector3 offset = targetSeatPosition - xrCamera.position;
            rideTarget.position += offset;
        }
        else
        {
            rideTarget.position = targetSeatPosition;
        }
    }

    Vector3 GetPathUp()
    {
        if (!useTrackPointUpForRoll)
        {
            return Vector3.up;
        }

        if (trackPoints == null || trackPoints.Length < 2)
        {
            return Vector3.up;
        }

        if (ridePathSegmentIndex == null || ridePathSegmentIndex.Count == 0)
        {
            return Vector3.up;
        }

        int segment = Mathf.Clamp(
            ridePathSegmentIndex[Mathf.Clamp(currentIndex, 0, ridePathSegmentIndex.Count - 1)],
            0,
            trackPoints.Length - 2
        );

        Transform pointA = trackPoints[segment];
        Transform pointB = trackPoints[Mathf.Min(segment + 1, trackPoints.Length - 1)];

        if (pointA == null || pointB == null)
        {
            return Vector3.up;
        }

        float t = 0f;

        if (samplesPerSegment > 0)
        {
            t = (currentIndex % samplesPerSegment) / (float)samplesPerSegment;
        }

        Vector3 up = Vector3.Slerp(pointA.up, pointB.up, t);

        if (up.sqrMagnitude < 0.001f)
        {
            return Vector3.up;
        }

        return up.normalized;
    }

    void UpdateSpeed(Vector3 currentPoint, Vector3 nextPoint)
    {
        float targetSpeed;

        int currentSegment = ridePathSegmentIndex[currentIndex];

        bool isStartArea = currentSegment < startTrackPointCount;

        int totalSegmentCount = trackPoints.Length - 1;
        bool isEndArea = currentSegment >= totalSegmentCount - endTrackPointCount;

        if (isStartArea)
        {
            targetSpeed = startAreaSpeed;
        }
        else if (isEndArea)
        {
            targetSpeed = endAreaSpeed;
        }
        else
        {
            float heightDifference = nextPoint.y - currentPoint.y;

            targetSpeed = currentSpeed - heightDifference * slopeInfluence;
            targetSpeed = Mathf.Clamp(targetSpeed, minSpeed, maxSpeed);
        }

        currentSpeed = Mathf.Lerp(
            currentSpeed,
            targetSpeed,
            Time.deltaTime * acceleration
        );
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
}