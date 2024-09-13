using UnityEngine;

/// <summary>
/// Script d'une plateforme qui se prom�ne en suivant un chemin � une certaine vitesse
/// </summary>
public class Plateforme : MonoBehaviour
{

    [SerializeField] private Transform[] _waypoints;
    [SerializeField] private float _speed;

    private int _targetWaypointIndex;

    private Transform _previousWaypoint;
    private Transform _targetWaypoint;

    private float _timeToWaypoint;
    private float _elapsedTime;

    public void Initialize(float speed, Transform[] waypoints)
    {
        _waypoints = waypoints;
        _speed = speed;
    }

    public void SetSpeed(float speed)
    {
        _speed = speed;
    }

    private void Start()
    {
        if (_waypoints.Length == 0)
        {
            Debug.LogError("Pas de waypoints pour la plateforme " + gameObject.name);
            Destroy(this);
        }
        TargetNextWaypoint();
    }

    private void FixedUpdate()
    {
        _elapsedTime += Time.fixedDeltaTime;

        float elapsedPercentage = _elapsedTime / _timeToWaypoint;
        elapsedPercentage = Mathf.SmoothStep(0, 1, elapsedPercentage);
        transform.SetPositionAndRotation(Vector3.Lerp(_previousWaypoint.position, _targetWaypoint.position, elapsedPercentage), Quaternion.Lerp(_previousWaypoint.rotation, _targetWaypoint.rotation, elapsedPercentage));
        if (elapsedPercentage >= 1)
        {
            TargetNextWaypoint();
        }
    }


    private void TargetNextWaypoint()
    {
        _previousWaypoint = _waypoints[_targetWaypointIndex];
        _targetWaypointIndex = GetNextWaypointIndex();
        _targetWaypoint = _waypoints[_targetWaypointIndex];

        _elapsedTime = 0;

        float distanceToWaypoint = Vector3.Distance(_previousWaypoint.position, _targetWaypoint.position);
        _timeToWaypoint = distanceToWaypoint / _speed;

    }

    private int GetNextWaypointIndex()
    {
        return (_targetWaypointIndex + 1) % _waypoints.Length;
    }
}
