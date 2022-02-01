using System;
using UnityEngine;

[Serializable]
public class RaycastInfo
{
    public Vector2 startPoint;
    public float length;
    public Vector2 rayDirection;
    public float spacing;
    public Vector2 spacingDirection;

    public RaycastInfo(Vector2 startPoint, float length, Vector2 rayDirection, float spacing, Vector2 spacingDirection)
    {
        this.startPoint = startPoint;
        this.length = length;
        this.rayDirection = rayDirection;
        this.spacing = spacing;
        this.spacingDirection = spacingDirection;
    }
}

[Serializable]
public class CollisionInfo
{
    public bool rayHit;
    public bool firstHit;
    public bool lastHit;
    public int hitCount;
    public Vector2 point;
    public float distance;
    public bool colliding;
    public RaycastInfo raycastInfo;

    public CollisionInfo(bool rayHit, Vector2 point, float distance)
    {
        this.rayHit = rayHit;
        this.point = point;
        this.distance = distance;
    }

    public void RestartHits()
    {
        rayHit = false;
        lastHit = false;
        firstHit = false;
        hitCount = 0;
    }
}

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCollision : MonoBehaviour
{
    [HideInInspector] public CollisionInfo downCollision;
    [HideInInspector] public CollisionInfo upCollision;
    [HideInInspector] public CollisionInfo rightCollision;
    [HideInInspector] public CollisionInfo leftCollision;
    [HideInInspector] public CollisionInfo platformDownCollision;
    public event Action<GameObject> onPlayerTriggerInteractables;

    [Range(0.1f, 0.5f)]
    [SerializeField] private float skinWidth = 0.15f;
    [Range(2, 10)]
    [SerializeField] private int rayCount = 2;
    [Range(0.2f, 5)]
    [SerializeField] private float horizontalMinRayLength = 0.3f;
    [Range(0.2f, 5)]
    [SerializeField] private float verticallMinRayLength = 1f;
    [Range(0.05f, 2)]
    [SerializeField] private float rayLengthModifier = 0.05f;
    [SerializeField] private float dashHitRadius = 0.5f;
    [SerializeField] private float platformCollRadius = 1f;
    [SerializeField] private float deffaultColRadius = 0.5f;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private LayerMask platformMask;
    [SerializeField] private LayerMask interactablesMask;

    private Transform _transform;
    private Bounds bounds;
    private BoxCollider2D _collider;

    private void Awake()
    {
        _transform = transform;
        _collider = GetComponent<BoxCollider2D>();
    }

    private void Start()
    {
        UpdateBounds();
        SetUpRaycastInfo();
        UpdateRaycastStartPoint();
    }

    private void FixedUpdate()
    {
        HandleInteractableCollisions();
    }

    private void HandleInteractableCollisions()
    {
        if (OverlapWithInteractables(out GameObject obj))
        {
            onPlayerTriggerInteractables?.Invoke(obj);
        }
    }

    private void SetUpRaycastInfo()
    {
        var spacing = GetRaySpacings();

        platformDownCollision.raycastInfo = new RaycastInfo(Vector2.zero, verticallMinRayLength, Vector2.down, spacing.y, Vector2.right);
        downCollision.raycastInfo = new RaycastInfo(Vector2.zero, verticallMinRayLength, Vector2.down, spacing.y, Vector2.right);
        upCollision.raycastInfo = new RaycastInfo(Vector2.zero, verticallMinRayLength, Vector2.up, spacing.y, Vector2.right);
        rightCollision.raycastInfo = new RaycastInfo(Vector2.zero, horizontalMinRayLength, Vector2.right, spacing.x, Vector2.up);
        leftCollision.raycastInfo = new RaycastInfo(Vector2.zero, horizontalMinRayLength, Vector2.left, spacing.x, Vector2.up);
    }

    public bool IsVerticalliColliding()
    {
        return downCollision.colliding || upCollision.colliding;
    }

    public bool OverlapPlatform(out GameObject gameObject)
    {
        var hit = Physics2D.OverlapCircle(_transform.position, platformCollRadius, platformMask);
        gameObject = hit ? hit.gameObject : null;

        return hit;
    }

    public bool OverlapWithInteractables(out GameObject obj)
    {
        var hit = Physics2D.OverlapCircle(_transform.position, deffaultColRadius, interactablesMask);
        obj = hit ? hit.gameObject : null;

        return hit;
    }

    public bool IsOverlapBox(Vector2 point, Vector2 size)
    {
        return Physics2D.OverlapBox(point, size, 0, collisionMask);
    }

    public bool IsHorizontallyColliding()
    {
        return rightCollision.colliding || leftCollision.colliding;
    }

    private float GetVerticalReposition(CollisionInfo hit)
    {
        var dir = -hit.raycastInfo.rayDirection.y;
        return hit.point.y + (bounds.size.y / 2 + skinWidth) * dir;
    }

    private float GetHorizontalReposition(CollisionInfo hit)
    {
        var dir = -hit.raycastInfo.rayDirection.x;
        return hit.point.x + (bounds.size.x / 2 + skinWidth) * dir;
    }

    //Probablly will delete this function
    public void ForceHorizontalReposition(CollisionInfo collisionInfo)
    {
        var pos = _transform.position;
        var floatX = GetHorizontalReposition(collisionInfo);
        pos.x = floatX;
        _transform.position = pos;
    }

    public void ForceVerticalReposition(CollisionInfo collisionInfo)
    {
        var pos = _transform.position;
        var floatY = GetVerticalReposition(collisionInfo);
        pos.y = floatY;
        _transform.position = pos;
    }

    public Vector2 GetDashHitPos(Vector2 dir, Vector2 furthestPoint)
    {
        return GetCircleCastAllHit(dir, furthestPoint, dashHitRadius);
    }

    public Vector2 GetCircleCastAllHit(Vector2 dir, Vector2 furthestPoint, float radius)
    {
        var dis = Vector2.Distance(_transform.position, furthestPoint);
        var hits = Physics2D.CircleCastAll(_transform.position, radius,  dir, dis, collisionMask);

        return hits.Length == 0 ? furthestPoint : hits[0].point;
    }

    public CollisionInfo GetClosestHorizontal()
    {
        if (!rightCollision.rayHit && !leftCollision.rayHit)
            return null;

        if (rightCollision.rayHit && !leftCollision.rayHit)
            return rightCollision;

        if (leftCollision.rayHit && !rightCollision.rayHit)
            return leftCollision;

        return rightCollision.distance < leftCollision.distance ? rightCollision : leftCollision;
    }

    public void HandleCollisions(Vector2 furthestPoint, ref Vector2 move, Vector2 rawMovement)
    {
        Vector2 moveDir = furthestPoint - (Vector2)_transform.position;
        moveDir.x = moveDir.x < 0 ? -1 : (moveDir.x > 0 ? 1 : 0);
        moveDir.y = moveDir.y < 0 ? -1 : (moveDir.y > 0 ? 1 : 0);

        CollisionDetection(ref rightCollision, rawMovement, false, collisionMask);
        CollisionDetection(ref leftCollision, rawMovement, false, collisionMask);
        CollisionDetection(ref upCollision, rawMovement, false, collisionMask);
        CollisionDetection(ref downCollision, rawMovement, true, collisionMask);
        CollisionDetection(ref platformDownCollision, rawMovement, true, platformMask);

        HandleHorizontalCollision(rightCollision, moveDir.x, furthestPoint, ref move);
        HandleHorizontalCollision(leftCollision, moveDir.x, furthestPoint, ref move);
        HandleVerticalCollision(upCollision, moveDir.y, furthestPoint, ref move);
        HandleVerticalCollision(downCollision, moveDir.y, furthestPoint, ref move);

        Debug.DrawLine(_transform.position, furthestPoint, Color.magenta);
    }

    private void HandleVerticalCollision(CollisionInfo collisionInfo, float moveDir, Vector2 furthestPoint, ref Vector2 move)
    {
        var checkPos = new Vector2(_transform.position.x, furthestPoint.y);
        collisionInfo.colliding = false;

        if (collisionInfo.rayHit)
        {
            Vector2 size = new Vector2(bounds.size.x, _collider.bounds.size.y);
            Vector2 dir = furthestPoint - (Vector2)_transform.position;
            dir.y = dir.y < 0 ? -1 : (dir.y > 0 ? 1 : -1);
            bool isDirMatching = dir.y == collisionInfo.raycastInfo.rayDirection.y;

            if (IsOverlapBox(checkPos, size) && isDirMatching)
            {
                var reposition = GetVerticalReposition(collisionInfo);
                var gap = reposition - furthestPoint.y;

                move.y += gap;
                collisionInfo.colliding = true;
            }
        }
    }

    private void HandleHorizontalCollision(CollisionInfo collisionInfo, float moveDir, Vector2 furthestPoint, ref Vector2 move)
    {
        var checkPos = new Vector2(furthestPoint.x, _transform.position.y);
        collisionInfo.colliding = false;

        if (collisionInfo.rayHit)
        {
            Vector2 size = new Vector2(_collider.bounds.size.x, bounds.size.y);
            bool isDirMatching = moveDir == collisionInfo.raycastInfo.rayDirection.x;

            if (IsOverlapBox(checkPos, size) && isDirMatching)
            {
                var reposition = GetHorizontalReposition(collisionInfo);
                var gap = reposition - furthestPoint.x;

                move.x += gap;
                collisionInfo.colliding = true;
            }
        }
    }

    private void CollisionDetection(ref CollisionInfo collisionInfo, Vector2 rawMovement, bool modifyLength, LayerMask layerMask)
    {
        UpdateRaycastStartPoint();
        collisionInfo.RestartHits();

        var info = collisionInfo.raycastInfo;
        var speed = (rawMovement * collisionInfo.raycastInfo.rayDirection).magnitude;
        var length = speed * rayLengthModifier + info.length;
        RaycastHit2D lastHit = new RaycastHit2D();

        for (int i = 0; i < rayCount; i++)
        {
            var origin = info.startPoint + info.spacingDirection * info.spacing * i;
            RaycastHit2D hit = Physics2D.Raycast(origin, info.rayDirection, length, layerMask);

            if (!hit)
                continue;

            if (i == 0)
                collisionInfo.firstHit = true;

            if (i == rayCount - 1)
                collisionInfo.lastHit = true;

            if (modifyLength)
                length = hit.distance;

            collisionInfo.hitCount++;
            lastHit = hit;
        }

        collisionInfo.rayHit = lastHit;
        collisionInfo.point = lastHit.point;
        collisionInfo.distance = lastHit.distance;

        for (int i = 0; i < rayCount; i++)
        {
            var origin = info.startPoint + info.spacingDirection * info.spacing * i;
            Debug.DrawRay(origin, info.rayDirection * length, Color.red);
        }
    }

    private void UpdateRaycastStartPoint()
    {
        UpdateBounds();

        platformDownCollision.raycastInfo.startPoint = new Vector2(bounds.min.x, bounds.min.y);
        downCollision.raycastInfo.startPoint = new Vector2(bounds.min.x, bounds.min.y);
        upCollision.raycastInfo.startPoint = new Vector2(bounds.min.x, bounds.max.y);
        rightCollision.raycastInfo.startPoint = new Vector2(bounds.max.x, bounds.min.y);
        leftCollision.raycastInfo.startPoint = new Vector2(bounds.min.x, bounds.min.y);
    }

    private Vector2 GetRaySpacings()
    {
        var horizontal = bounds.size.y / (rayCount - 1);
        var vertical = bounds.size.x / (rayCount - 1);

        return new Vector2(horizontal, vertical);
    }

    private float Sign(float number)
    {
        return number < 0 ? -1 : (number > 0 ? 1 : -1);
    }

    private void UpdateBounds()
    {
        bounds = _collider.bounds;
        bounds.Expand(skinWidth * -2);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, bounds.size);
        Gizmos.DrawWireSphere(transform.position, platformCollRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, deffaultColRadius);
    }
}
