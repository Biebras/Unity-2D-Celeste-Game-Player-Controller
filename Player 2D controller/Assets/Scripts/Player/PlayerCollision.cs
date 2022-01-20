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
}

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCollision : MonoBehaviour
{
    [HideInInspector] public CollisionInfo downCollision;
    [HideInInspector] public CollisionInfo upCollision;
    [HideInInspector] public CollisionInfo rightCollision;
    [HideInInspector] public CollisionInfo leftCollision;

    [Range(0.1f, 0.5f)]
    [SerializeField] private float skinWidth = 0.015f;
    [Range(2, 10)]
    [SerializeField] private int rayCount = 3;
    [Range(0.2f, 5)]
    [SerializeField] private float minRayLength = 0.5f;
    [Range(0.05f, 2)]
    [SerializeField] private float rayLengthModifier = 0.05f;
    [SerializeField] private LayerMask collisionMask;

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

    private void SetUpRaycastInfo()
    {
        var spacing = GetRaySpacings();

        downCollision.raycastInfo = new RaycastInfo(Vector2.zero, minRayLength, Vector2.down, spacing.y, Vector2.right);
        upCollision.raycastInfo = new RaycastInfo(Vector2.zero, minRayLength, Vector2.up, spacing.y, Vector2.right);
        rightCollision.raycastInfo = new RaycastInfo(Vector2.zero, minRayLength, Vector2.right, spacing.x, Vector2.up);
        leftCollision.raycastInfo = new RaycastInfo(Vector2.zero, minRayLength, Vector2.left, spacing.x, Vector2.up);
    }

    public bool IsVerticalliColliding()
    {
        return downCollision.colliding || upCollision.colliding;
    }

    public bool IsOverlapBox(Vector2 point, Vector2 size)
    {
        return Physics2D.OverlapBox(point, size, 0, collisionMask);
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

    public void HandleCollisions(Vector2 furthestPoint, ref Vector2 move, Vector2 rawMovement)
    {
        Vector2 moveDir = furthestPoint - (Vector2)_transform.position;
        moveDir.x = moveDir.x < 0 ? -1 : (moveDir.x > 0 ? 1 : 0);
        moveDir.y = moveDir.y < 0 ? -1 : (moveDir.y > 0 ? 1 : 0);

        CollisionDetection(ref rightCollision, rawMovement);
        CollisionDetection(ref leftCollision, rawMovement);
        CollisionDetection(ref upCollision, rawMovement);
        CollisionDetection(ref downCollision, rawMovement);

        HandleHorizontalCollision(rightCollision, moveDir.x, furthestPoint, ref move, true);
        HandleHorizontalCollision(leftCollision, moveDir.x, furthestPoint, ref move, true);
        HandleVerticalCollision(upCollision, moveDir.y, furthestPoint, ref move, true);
        HandleVerticalCollision(downCollision, moveDir.y, furthestPoint, ref move, true);

        Debug.DrawLine(_transform.position, furthestPoint, Color.magenta);
    }

    private void HandleVerticalCollision(CollisionInfo collisionInfo, float moveDir, Vector2 furthestPoint, ref Vector2 move, bool recalculate)
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

                if(recalculate)
                    move.y += gap;

                collisionInfo.colliding = true;
            }
        }
    }

    private void HandleHorizontalCollision(CollisionInfo collisionInfo, float moveDir, Vector2 furthestPoint, ref Vector2 move, bool recalculate)
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

                if (recalculate)
                    move.x += gap;

                collisionInfo.colliding = true;
            }
        }
    }

    private void CollisionDetection(ref CollisionInfo collisionInfo, Vector2 rawMovement)
    {
        UpdateRaycastStartPoint();

        var info = collisionInfo.raycastInfo;
        var speed = (rawMovement * collisionInfo.raycastInfo.rayDirection).magnitude;
        var length = speed * rayLengthModifier + info.length;
        RaycastHit2D lastHit = new RaycastHit2D();

        for (int i = 0; i < rayCount; i++)
        {
            var origin = info.startPoint + info.spacingDirection * info.spacing * i;
            RaycastHit2D hit = Physics2D.Raycast(origin, info.rayDirection, length, collisionMask);

            if (!hit)
                continue;

            lastHit = hit;
            length = lastHit.distance;
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

    public void UpdateRaycastStartPoint()
    {
        UpdateBounds();

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
    }
}
