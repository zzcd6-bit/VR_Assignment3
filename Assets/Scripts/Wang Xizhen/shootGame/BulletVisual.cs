using UnityEngine;

public class BulletVisual : MonoBehaviour
{
    public float speed = 25f;

    private Vector3 targetPosition;
    private bool hasTarget;

    public void Init(Vector3 startPosition, Vector3 endPosition, float moveSpeed)
    {
        transform.position = startPosition;
        targetPosition = endPosition;
        speed = moveSpeed;
        hasTarget = true;

        Vector3 direction = targetPosition - startPosition;

        if (direction.sqrMagnitude > 0.001f)
        {
            transform.forward = direction.normalized;
        }
    }

    private void Update()
    {
        if (!hasTarget)
        {
            return;
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            Destroy(gameObject);
        }
    }
}