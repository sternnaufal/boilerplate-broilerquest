using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public enum MovementMode
    {
        SinglePoint,
        Patrol,
        Wander,
        Follow
    }

    [Header("Mode")]
    [SerializeField] private MovementMode mode = MovementMode.Wander;

    [Header("Movement")]
    [SerializeField] private float speed = 150f;
    [SerializeField] private Vector2 fixingPoint;
    [SerializeField] private Vector2[] setAllPointstoFix;
    [SerializeField] private bool snapOnArrival = true;
    [SerializeField] private float arrivalThreshold = 10f;
    [SerializeField] private bool autoStart = true;

    [Header("Patrol")]
    [SerializeField] private bool pingPong = true;

    [Header("Wander")]
    [SerializeField] private Vector2 wanderRadius = new Vector2(100f, 80f);
    [SerializeField] private float wanderPauseMin = 1f;
    [SerializeField] private float wanderPauseMax = 3f;

    [Header("Follow")]
    [SerializeField] private Transform followTarget;
    [SerializeField] private float followStopDistance = 50f;

    [Header("Collision Avoidance")]
    [SerializeField] private bool avoidCollision = true;
    [SerializeField] private float collisionMargin = 10f;
    [SerializeField] private string obstacleTag = "KandangSlot";

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkAnimParam = "isWalking";

    [Header("Facing")]
    [SerializeField] private bool flipOnDirection = true;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private bool isMoving;
    private int currentPointIndex;
    private int direction = 1;
    private float wanderPauseTimer;
    private bool isPausing;

    private float patrolPauseTimer;
    private bool isPatrolPausing;

    private float previousY;
    private string upAnimParam = "isGoingUp";
    private string downAnimParam = "isGoingDown";

    public Vector2 FixingPoint
    {
        get => fixingPoint;
        set
        {
            fixingPoint = value;
            mode = MovementMode.SinglePoint;
            isMoving = true;
        }
    }

    public bool IsMoving => isMoving;
    public MovementMode Mode => mode;
    public Vector2[] SetAllPointstoFix
    {
        get => setAllPointstoFix;
        set
        {
            setAllPointstoFix = value;
            if (setAllPointstoFix != null && setAllPointstoFix.Length > 0)
            {
                mode = MovementMode.Patrol;
                currentPointIndex = 0;
                direction = 1;
                fixingPoint = setAllPointstoFix[0];
                isMoving = true;
            }
        }
    }

    private RectTransform[] obstacles;
    private Vector2 playerHalfSize;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        playerHalfSize = rectTransform.rect.size * 0.5f;
        
        if (animator == null)
            animator = GetComponentInChildren<Animator>(); // ← tambah InChildren
        
        if (animator == null)
            animator = GetComponentInParent<Animator>(); // ← fallback ke parent
    }

    private void LateUpdate()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>(); // coba re-fetch
            if (animator == null) return; // masih null, skip
        }
        
        if (animator.runtimeAnimatorController == null) return;
        
        if (!isPatrolPausing)
        animator.SetBool(walkAnimParam, isMoving);
    }

    private void Start()
    {
        startPosition = rectTransform.anchoredPosition;
        previousY = startPosition.y;

        if (avoidCollision)
        {
            GameObject[] found = GameObject.FindGameObjectsWithTag(obstacleTag);
            obstacles = new RectTransform[found.Length];
            for (int i = 0; i < found.Length; i++)
                obstacles[i] = found[i].GetComponent<RectTransform>();
        }

        switch (mode)
        {
            case MovementMode.Patrol:
                if (setAllPointstoFix != null && setAllPointstoFix.Length > 0)
                {
                    currentPointIndex = 0;
                    direction = 1;
                    fixingPoint = setAllPointstoFix[0];
                    isMoving = autoStart;
                }
                break;

            case MovementMode.SinglePoint:
                if (fixingPoint != startPosition)
                    isMoving = autoStart;
                break;

            case MovementMode.Wander:
                isMoving = autoStart;
                break;
        }
    }

    private void Update()
    {
        if (rectTransform == null) return;

        // Wander tetap perlu update saat pausing (untuk hitung timer)
        if (mode == MovementMode.Wander)
        {
            UpdateWander();
            return;
        }

        else if (mode == MovementMode.Follow)
        {
            UpdateFollow();
            return;
        }

        UpdateMoveToPoint();
    }

    private void UpdateFollow()
    {
        if (followTarget == null) return;

        Vector2 targetPos = followTarget.position; // atau anchoredPosition jika target UI
        Vector2 currentPos = rectTransform.anchoredPosition;
        float distance = Vector2.Distance(currentPos, targetPos);

        if (distance <= followStopDistance)
        {
            // Berhenti dekat target
            if (animator != null) animator.SetBool(walkAnimParam, false);
            return;
        }

        // Bergerak menuju target
        Vector2 oldPos = currentPos;
        Vector2 newPos = Vector2.MoveTowards(currentPos, targetPos, speed * Time.deltaTime);
        rectTransform.anchoredPosition = newPos;
        if (flipOnDirection) UpdateFacing(oldPos, newPos);
        if (animator != null) animator.SetBool(walkAnimParam, true);
    }

    public void SetFollowTarget(Transform target)
    {
        followTarget = target;
        mode = MovementMode.Follow;
        isMoving = true;
    }

    private void UpdateWander()
    {
        Vector2 currentPos = rectTransform.anchoredPosition;

        if (isPausing)
        {
            wanderPauseTimer -= Time.deltaTime;
            if (wanderPauseTimer <= 0f)
            {
                isPausing = false;
                isMoving = true;
                fixingPoint = new Vector2(
                    currentPos.x + Random.Range(-wanderRadius.x, wanderRadius.x),
                    currentPos.y + Random.Range(-wanderRadius.y, wanderRadius.y)
                );
            }
            return;
        }

        float distance = Vector2.Distance(currentPos, fixingPoint);

        if (distance <= arrivalThreshold)
        {
            if (snapOnArrival)
                rectTransform.anchoredPosition = fixingPoint;

            wanderPauseTimer = Random.Range(wanderPauseMin, wanderPauseMax);
            isPausing = true;
            isMoving = false;
            previousY = rectTransform.anchoredPosition.y; // update previousY
            return;
        }

        Vector2 oldPos = currentPos;
        Vector2 newPos = Vector2.MoveTowards(currentPos, fixingPoint, speed * Time.deltaTime);

        if (!avoidCollision || !WouldOverlap(newPos))
        {
            rectTransform.anchoredPosition = newPos;
            if (flipOnDirection)
                UpdateFacing(oldPos, newPos);
            // Update animasi vertikal setelah posisi berubah
            UpdateVerticalAnimation(newPos.y);
        }
        else
        {
            if (animator != null && !string.IsNullOrEmpty(walkAnimParam))
                animator.SetBool(walkAnimParam, false);
        }
    }

    private void UpdateMoveToPoint()
    {
        if (isPatrolPausing)
        {
            patrolPauseTimer -= Time.deltaTime;
            if (patrolPauseTimer <= 0f)
            {
                isPatrolPausing = false;
                isMoving = true;
                MoveToNextPoint();
            }
            return;
        }

        if (!isMoving) return;

        Vector2 currentPos = rectTransform.anchoredPosition;
        float distance = Vector2.Distance(currentPos, fixingPoint);

        if (distance <= arrivalThreshold)
        {
            if (snapOnArrival)
                rectTransform.anchoredPosition = fixingPoint;

            // ← Reset animasi vertikal saat arrival
            if (animator != null)
            {
                animator.SetBool(upAnimParam, false);
                animator.SetBool(downAnimParam, false);
                animator.SetBool(walkAnimParam, false);
            }

            previousY = rectTransform.anchoredPosition.y; // ← update di sini
            patrolPauseTimer = Random.Range(wanderPauseMin, wanderPauseMax);
            isPatrolPausing = true;
            isMoving = false;
            return;
        }

        Vector2 oldPos = currentPos;
        Vector2 newPos = Vector2.MoveTowards(currentPos, fixingPoint, speed * Time.deltaTime);

        if (!avoidCollision || !WouldOverlap(newPos))
        {
            rectTransform.anchoredPosition = newPos;
            if (flipOnDirection)
                UpdateFacing(oldPos, newPos);
            UpdateVerticalAnimation(newPos.y);
        }
    }

    private void MoveToNextPoint()
    {
        Debug.Log("MoveToNextPoint called");
        if (setAllPointstoFix == null || setAllPointstoFix.Length == 0)
        {
            isMoving = false;
            return;
        }

        if (pingPong)
        {
            currentPointIndex += direction;
            if (currentPointIndex >= setAllPointstoFix.Length)
            {
                currentPointIndex = setAllPointstoFix.Length - 2;
                direction = -1;
            }
            else if (currentPointIndex < 0)
            {
                currentPointIndex = 1;
                direction = 1;
            }
        }
        else
        {
            currentPointIndex = (currentPointIndex + 1) % setAllPointstoFix.Length;
        }

        fixingPoint = setAllPointstoFix[currentPointIndex];
        previousY = rectTransform.anchoredPosition.y;
        isMoving = true;
        Debug.Log($"New target: {fixingPoint}, isMoving={isMoving}");
    }

    public void SetFixingPoint(Vector2 point)
    {
        FixingPoint = point;
    }

    public void SetRandomFixingPoint(Vector2 center, Vector2 radius)
    {
        Vector2 randomPoint = new Vector2(
            center.x + Random.Range(-radius.x, radius.x),
            center.y + Random.Range(-radius.y, radius.y)
        );
        FixingPoint = randomPoint;
    }

    public void GoToPoint(int index)
    {
        if (setAllPointstoFix == null || index < 0 || index >= setAllPointstoFix.Length)
            return;

        currentPointIndex = index;
        mode = MovementMode.SinglePoint;
        fixingPoint = setAllPointstoFix[index];
        isMoving = true;
    }

    public void StartPatrol()
    {
        if (setAllPointstoFix != null && setAllPointstoFix.Length > 0)
        {
            mode = MovementMode.Patrol;
            currentPointIndex = 0;
            direction = 1;
            fixingPoint = setAllPointstoFix[0];
            isMoving = true;
        }
    }

    public void StartWander()
    {
        mode = MovementMode.Wander;
        isPausing = false;
        isMoving = true;
    }

    public void StopMoving()
    {
        isMoving = false;
    }

    private bool WouldOverlap(Vector2 position)
    {
        if (obstacles == null) return false;

        Rect playerRect = new Rect(
            position - playerHalfSize + new Vector2(collisionMargin, collisionMargin) * 0.5f,
            rectTransform.rect.size - new Vector2(collisionMargin, collisionMargin)
        );

        foreach (RectTransform obstacle in obstacles)
        {
            if (obstacle == null || !obstacle.gameObject.activeInHierarchy) continue;

            Rect obstacleRect = new Rect(
                (Vector2)obstacle.anchoredPosition - obstacle.rect.size * 0.5f,
                obstacle.rect.size
            );

            if (playerRect.Overlaps(obstacleRect))
                return true;
        }

        return false;
    }

    private void UpdateFacing(Vector2 from, Vector2 to)
    {
        float dirX = to.x - from.x;
        if (Mathf.Abs(dirX) < 0.01f) return;

        Vector3 scale = rectTransform.localScale;
        if (dirX > 0 && scale.x < 0)
            scale.x = Mathf.Abs(scale.x);
        else if (dirX < 0 && scale.x > 0)
            scale.x = -Mathf.Abs(scale.x);

        rectTransform.localScale = scale;
    }

    private void UpdateVerticalAnimation(float currentY)
    {
        if (animator == null) return;

        float deltaY = currentY - previousY;
        bool goingUp = deltaY > 0.1f;
        bool goingDown = deltaY < -0.1f;
        bool isMovingNow = isMoving && !isPausing && !isPatrolPausing;

        if (!isMovingNow)
        {
            animator.SetBool(upAnimParam, false);
            animator.SetBool(downAnimParam, false);
            previousY = currentY;
            return;
        }

        animator.SetBool(upAnimParam, goingUp);
        animator.SetBool(downAnimParam, goingDown);
        previousY = currentY;
    }
}
