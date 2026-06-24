using UnityEngine;

public class MovingTarget : MonoBehaviour
{
    public float moveDistance = 1.5f;
    public float moveSpeed = 1f;

    private Vector3 startLocalPosition;
    private Vector3 leftPoint;
    private Vector3 rightPoint;
    private bool movingRight = true;

    private void Start()
    {
        startLocalPosition = transform.localPosition;

        leftPoint = startLocalPosition + Vector3.back * (moveDistance * 0.5f);
        rightPoint = startLocalPosition + Vector3.forward * (moveDistance * 0.5f);
    }

    private void Update()
    {
        Vector3 target = movingRight ? rightPoint : leftPoint;

        transform.localPosition = Vector3.MoveTowards(
            transform.localPosition,
            target,
            moveSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.localPosition, target) < 0.01f)
        {
            movingRight = !movingRight;
        }
    }
}