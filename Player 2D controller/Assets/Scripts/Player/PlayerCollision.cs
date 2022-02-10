using System;
using UnityEngine;

[Serializable]
public class RaycastInfo
{
    public Vector2 StartPoint;
    public float Length;
    public Vector2 RayDirection;
    public float Spacing;
    public Vector2 SpacingDirection;

    public RaycastInfo(Vector2 startPoint, float length, Vector2 rayDirection, float spacing, Vector2 spacingDirection)
    {
        StartPoint = startPoint;
        Length = length;
        RayDirection = rayDirection;
        Spacing = spacing;
        SpacingDirection = spacingDirection;
    }
}

[Serializable]
public class CollisionInfo
{
    public bool RayHit;
    public bool FirstHit;
    public bool LastHit;
    public int HitCount;
    public Vector2 Point;
    public float Distance;
    public bool Colliding;
    public RaycastInfo RaycastInfo;

    public CollisionInfo(bool rayHit, Vector2 point, float distance)
    {
        RayHit = rayHit;
        Point = point;
        Distance = distance;
    }

    public void RestartHits()
    {
        RayHit = false;
        LastHit = false;
        FirstHit = false;
        HitCount = 0;
    }
}

[RequireComponent(typeof(BoxCollider2D))]
public class PlayerCollision : MonoBehaviour
{
    [HideInInspector] public CollisionInfo DownCollision;
    [HideInInspector] public CollisionInfo UpCollision;
    [HideInInspector] public CollisionInfo RightCollision;
    [HideInInspector] public CollisionInfo LeftCollision;
    [HideInInspector] public CollisionInfo PlatformDownCollision;
    public event Action<GameObject> OnPlayerTriggerInteractables;

    [Range(0.1f, 0.5f)]
    [SerializeField] private float _skinWidth = 0.15f;
    [Range(2, 10)]
    [SerializeField] private int _rayCount = 3;
    [Range(0.2f, 5)]
    [SerializeField] private float _horizontalMinRayLength = 0.3f;
    [Range(0.2f, 5)]
    [SerializeField] private float _verticallMinRayLength = 0.3f;
    [Range(0.05f, 2)]
    [SerializeField] private float _rayLengthModifier = 0.05f;
    [SerializeField] private float _dashHitRadius = 0.25f;
    [SerializeField] private float _platformCollRadius = 0.8f;
    [SerializeField] private float _deffaultColRadius = 0.5f;
    [SerializeField] private LayerMask _collisionMask;
    [SerializeField] private LayerMask _platformMask;
    [SerializeField] private LayerMask _interactablesMask;

    private Transform _transform;
    private Bounds _bounds;
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
            OnPlayerTriggerInteractables?.Invoke(obj);
        }
    }

    private void SetUpRaycastInfo()
    {
        var spacing = GetRaySpacings();

        PlatformDownCollision.RaycastInfo = new RaycastInfo(Vector2.zero, _verticallMinRayLength, Vector2.down, spacing.y, Vector2.right);
        DownCollision.RaycastInfo = new RaycastInfo(Vector2.zero, _verticallMinRayLength, Vector2.down, spacing.y, Vector2.right);
        UpCollision.RaycastInfo = new RaycastInfo(Vector2.zero, _verticallMinRayLength, Vector2.up, spacing.y, Vector2.right);
        RightCollision.RaycastInfo = new RaycastInfo(Vector2.zero, _horizontalMinRayLength, Vector2.right, spacing.x, Vector2.up);
        LeftCollision.RaycastInfo = new RaycastInfo(Vector2.zero, _horizontalMinRayLength, Vector2.left, spacing.x, Vector2.up);
    }

    public bool IsVerticalliColliding()
    {
        return DownCollision.Colliding || UpCollision.Colliding;
    }

    public bool OverlapPlatform(out GameObject gameObject)
    {
        var hit = Physics2D.OverlapCircle(_transform.position, _platformCollRadius, _platformMask);
        gameObject = hit ? hit.gameObject : null;

        return hit;
    }

    public bool OverlapWithInteractables(out GameObject obj)
    {
        var hit = Physics2D.OverlapCircle(_transform.position, _deffaultColRadius, _interactablesMask);
        obj = hit ? hit.gameObject : null;

        return hit;
    }

    public bool IsOverlapBox(Vector2 point, Vector2 size)
    {
        return Physics2D.OverlapBox(point, size, 0, _collisionMask);
    }

    public bool IsHorizontallyColliding()
    {
        return RightCollision.Colliding || LeftCollision.Colliding;
    }

    private float GetVerticalReposition(CollisionInfo hit)
    {
        var dir = -hit.RaycastInfo.RayDirection.y;
        return hit.Point.y + (_bounds.size.y / 2 + _skinWidth) * dir;
    }

    private float GetHorizontalReposition(CollisionInfo hit)
    {
        var dir = -hit.RaycastInfo.RayDirection.x;
        return hit.Point.x + (_bounds.size.x / 2 + _skinWidth) * dir;
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
        return GetCircleCastAllHit(dir, furthestPoint, _dashHitRadius);
    }

    public Vector2 GetCircleCastAllHit(Vector2 dir, Vector2 furthestPoint, float radius)
    {
        var dis = Vector2.Distance(_transform.position, furthestPoint);
        var hits = Physics2D.CircleCastAll(_transform.position, radius,  dir, dis, _collisionMask);

        return hits.Length == 0 ? furthestPoint : hits[0].point;
    }

    public CollisionInfo GetClosestHorizontal()
    {
        if (!RightCollision.RayHit && !LeftCollision.RayHit)
            return null;

        if (RightCollision.RayHit && !LeftCollision.RayHit)
            return RightCollision;

        if (LeftCollision.RayHit && !RightCollision.RayHit)
            return LeftCollision;

        return RightCollision.Distance < LeftCollision.Distance ? RightCollision : LeftCollision;
    }

    public void HandleCollisions(Vector2 furthestPoint, ref Vector2 move, Vector2 rawMovement)
    {
        Vector2 moveDir = furthestPoint - (Vector2)_transform.position;
        moveDir.x = moveDir.x < 0 ? -1 : (moveDir.x > 0 ? 1 : 0);
        moveDir.y = moveDir.y < 0 ? -1 : (moveDir.y > 0 ? 1 : 0);

        CollisionDetection(ref RightCollision, rawMovement, false, _collisionMask);
        CollisionDetection(ref LeftCollision, rawMovement, false, _collisionMask);
        CollisionDetection(ref UpCollision, rawMovement, false, _collisionMask);
        CollisionDetection(ref DownCollision, rawMovement, true, _collisionMask);
        CollisionDetection(ref PlatformDownCollision, rawMovement, true, _platformMask);

        HandleHorizontalCollision(RightCollision, moveDir.x, furthestPoint, ref move);
        HandleHorizontalCollision(LeftCollision, moveDir.x, furthestPoint, ref move);
        HandleVerticalCollision(UpCollision, moveDir.y, furthestPoint, ref move);
        HandleVerticalCollision(DownCollision, moveDir.y, furthestPoint, ref move);

        Debug.DrawLine(_transform.position, furthestPoint, Color.magenta);
    }

    private void HandleVerticalCollision(CollisionInfo collisionInfo, float moveDir, Vector2 furthestPoint, ref Vector2 move)
    {
        var checkPos = new Vector2(_transform.position.x, furthestPoint.y);
        collisionInfo.Colliding = false;

        if (collisionInfo.RayHit)
        {
            Vector2 size = new Vector2(_bounds.size.x, _collider.bounds.size.y);
            Vector2 dir = furthestPoint - (Vector2)_transform.position;
            dir.y = dir.y < 0 ? -1 : (dir.y > 0 ? 1 : -1);
            bool isDirMatching = dir.y == collisionInfo.RaycastInfo.RayDirection.y;

            if (IsOverlapBox(checkPos, size) && isDirMatching)
            {
                var reposition = GetVerticalReposition(collisionInfo);
                var gap = reposition - furthestPoint.y;

                move.y += gap;
                collisionInfo.Colliding = true;
            }
        }
    }

    private void HandleHorizontalCollision(CollisionInfo collisionInfo, float moveDir, Vector2 furthestPoint, ref Vector2 move)
    {
        var checkPos = new Vector2(furthestPoint.x, _transform.position.y);
        collisionInfo.Colliding = false;

        if (collisionInfo.RayHit)
        {
            Vector2 size = new Vector2(_collider.bounds.size.x, _bounds.size.y);
            bool isDirMatching = moveDir == collisionInfo.RaycastInfo.RayDirection.x;

            if (IsOverlapBox(checkPos, size) && isDirMatching)
            {
                var reposition = GetHorizontalReposition(collisionInfo);
                var gap = reposition - furthestPoint.x;

                move.x += gap;
                collisionInfo.Colliding = true;
            }
        }
    }

    private void CollisionDetection(ref CollisionInfo collisionInfo, Vector2 rawMovement, bool modifyLength, LayerMask layerMask)
    {
        UpdateRaycastStartPoint();
        collisionInfo.RestartHits();

        var info = collisionInfo.RaycastInfo;
        var speed = (rawMovement * collisionInfo.RaycastInfo.RayDirection).magnitude;
        var length = speed * _rayLengthModifier + info.Length;
        RaycastHit2D lastHit = new RaycastHit2D();

        for (int i = 0; i < _rayCount; i++)
        {
            var origin = info.StartPoint + info.SpacingDirection * info.Spacing * i;
            RaycastHit2D hit = Physics2D.Raycast(origin, info.RayDirection, length, layerMask);

            if (!hit)
                continue;

            if (i == 0)
                collisionInfo.FirstHit = true;

            if (i == _rayCount - 1)
                collisionInfo.LastHit = true;

            if (modifyLength)
                length = hit.distance;

            collisionInfo.HitCount++;
            lastHit = hit;
        }

        collisionInfo.RayHit = lastHit;
        collisionInfo.Point = lastHit.point;
        collisionInfo.Distance = lastHit.distance;

        for (int i = 0; i < _rayCount; i++)
        {
            var origin = info.StartPoint + info.SpacingDirection * info.Spacing * i;
            Debug.DrawRay(origin, info.RayDirection * length, Color.red);
        }
    }

    private void UpdateRaycastStartPoint()
    {
        UpdateBounds();

        PlatformDownCollision.RaycastInfo.StartPoint = new Vector2(_bounds.min.x, _bounds.min.y);
        DownCollision.RaycastInfo.StartPoint = new Vector2(_bounds.min.x, _bounds.min.y);
        UpCollision.RaycastInfo.StartPoint = new Vector2(_bounds.min.x, _bounds.max.y);
        RightCollision.RaycastInfo.StartPoint = new Vector2(_bounds.max.x, _bounds.min.y);
        LeftCollision.RaycastInfo.StartPoint = new Vector2(_bounds.min.x, _bounds.min.y);
    }

    private Vector2 GetRaySpacings()
    {
        var horizontal = _bounds.size.y / (_rayCount - 1);
        var vertical = _bounds.size.x / (_rayCount - 1);

        return new Vector2(horizontal, vertical);
    }

    private float Sign(float number)
    {
        return number < 0 ? -1 : (number > 0 ? 1 : -1);
    }

    private void UpdateBounds()
    {
        _bounds = _collider.bounds;
        _bounds.Expand(_skinWidth * -2);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, _bounds.size);
        Gizmos.DrawWireSphere(transform.position, _platformCollRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _deffaultColRadius);
    }
}
