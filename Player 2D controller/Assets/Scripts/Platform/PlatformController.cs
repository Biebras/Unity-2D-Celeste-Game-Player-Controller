using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//I will change this later, created to fast test platforms... This code was made at 1am. Me brain booga booga.
public class PlatformController : MonoBehaviour
{
    [SerializeField] private float xSpeed;
    [SerializeField] private float ySpeed;
    [SerializeField] private Vector2 pointA;
    [SerializeField] private Vector2 pointB;

    private Vector2 rawMovement;
    private float horizontalSpeed;
    private float verticalSpeed;
    private Vector2 currentWaypoint;

    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
    }

    private void Start()
    {
        _transform.position = pointA;

        horizontalSpeed = xSpeed;
        verticalSpeed = ySpeed;
        currentWaypoint = pointB;
    }

    private void Update()
    {
        MovePlatform();
        Move();
    }

    private void MovePlatform()
    {
        var distance = Vector2.Distance(_transform.position, currentWaypoint);

        if(distance < 0.1f)
        {
            if (currentWaypoint == pointB)
                currentWaypoint = pointA;
            else
                currentWaypoint = pointB;

            horizontalSpeed = -horizontalSpeed;
            verticalSpeed = -verticalSpeed;
        }
    }

    public Vector2 GetRawVelocity()
    {
        return new Vector2(horizontalSpeed, verticalSpeed);
    }

    private void Move()
    {
        var pos = _transform.position;
        rawMovement = new Vector2(horizontalSpeed, verticalSpeed);
        var move = rawMovement * Time.deltaTime;

        _transform.position += (Vector3)move;
    }
}
