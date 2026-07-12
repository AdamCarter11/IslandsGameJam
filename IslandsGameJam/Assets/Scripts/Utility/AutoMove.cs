using UnityEngine;

public class AutoMove : MonoBehaviour
{
    [SerializeField]
    private float speed = 5f;

    private bool isMoving;
    private Vector2 startPosition;
    private Vector2 targetPosition;
    private float moveDuration;
    private float elapsedTime;

    public void Initialize(Vector2Int targetLocalPosition)
    {
        startPosition = transform.localPosition;
        targetPosition = targetLocalPosition;

        float distance = Vector2.Distance(startPosition, targetPosition);
        moveDuration = distance / Mathf.Max(speed, 0.01f);

        elapsedTime = 0f;
        isMoving = moveDuration > 0f;

        if (!isMoving)
            SetLocalPosition(targetPosition);
    }

    private void Update()
    {
        if (!isMoving)
            return;

        elapsedTime += Time.deltaTime;

        float t = Mathf.Clamp01(elapsedTime / moveDuration);

        // Ease-out cubic: starts fast and slows near the end.
        float easedT = 1f - Mathf.Pow(1f - t, 3f);

        Vector2 position = Vector2.LerpUnclamped(
            startPosition,
            targetPosition,
            easedT
        );

        SetLocalPosition(position);

        if (t >= 1f)
        {
            SetLocalPosition(targetPosition);
            isMoving = false;
        }
    }

    private void SetLocalPosition(Vector2 position)
    {
        Vector3 localPosition = transform.localPosition;
        localPosition.x = position.x;
        localPosition.y = position.y;
        transform.localPosition = localPosition;
    }
}
