using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(ConstantForce)), RequireComponent(typeof(Rigidbody)), RequireComponent(typeof(SphereCollider))]
public class StickyMovableController : MonoBehaviour
{
    #region Local Components
    private Rigidbody _rigidBody;
    private ConstantForce _constantForce;
    [SerializeField] private SphereCollider _collider;
    #endregion

    #region Public properties
    public Vector3 SurfaceNormal { get; private set; }
    public bool IsGrounded { get; private set; }
    public Vector3 MyNormal { get; private set; }
    #endregion

    #region Private fields
    private float _gravity = -Physics.gravity.magnitude;
    private Dictionary<Collider, List<Vector3>> _collisionNormals = new Dictionary<Collider, List<Vector3>>();
    public bool IsFrozen = false;
    #endregion

    #region Stickyness settings
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField, Range(0f, 5f), Tooltip("How far beyond the collider to check for ground")]
    private float _groundCheckRange = 0.2f;
    [SerializeField, Range(1f, 20f), Tooltip("How fast the snail rotates to align with a surface (in degrees/second)")]
    private float _lerpSpeed = 10f;
    [SerializeField, Range(1f, 10f)] private float _gravityMultiplier = 2f;
    #endregion

    private void Awake()
    {
        _constantForce = GetComponent<ConstantForce>();
        _rigidBody = GetComponent<Rigidbody>();
        _collider = GetComponent<SphereCollider>();
        MyNormal = transform.up;
        SurfaceNormal = MyNormal;
    }

    private void OnEnable()
    {
        _constantForce.enabled = true;
    }

    private void OnDisable()
    {
        _constantForce.enabled = false;
    }

    private void Update()
    {
        LerpRotationToSurfaceNormal();
    }

    private void FixedUpdate()
    {
        _constantForce.force = _gravity * _gravityMultiplier * _rigidBody.mass * SurfaceNormal;
#if DEBUG
        Vector3 origin = transform.position + transform.up * _collider.radius * 3f;
        DebugExtension.DebugArrow(origin, _constantForce.force.normalized, Color.red);
        DebugExtension.DebugArrow(origin + transform.up * 0.1f, -SurfaceNormal, Color.blue);
        DebugExtension.DebugArrow(origin + transform.up * 0.2f, -MyNormal, Color.green);
#endif
    }

    private void LateUpdate()
    {
        AverageContactPoints();
    }

    private void OnDestroy()
    {
        if (_constantForce != null) Destroy(_constantForce);
        if (_rigidBody != null) _rigidBody.isKinematic = true;
    }

    public void Enable()
    {
        SurfaceNormal = MyNormal = Vector3.up;
        _rigidBody.velocity = Vector3.zero;
        _constantForce.enabled = true;
        _constantForce.force = _gravity * _gravityMultiplier * _rigidBody.mass * SurfaceNormal;
    }

    public void Disable()
    {
        _collisionNormals.Clear();
        _constantForce.enabled = false;
    }

    /// <summary>
    /// Calculates a desired normal for the snail that averages all the surface normals beneath and contacting it
    /// </summary>
    private void AverageContactPoints()
    {
#if DEBUG
        Vector3 origin = transform.position + transform.up * _collider.radius * 2.5f;
#endif
        Vector3 average = Vector3.zero;
        if (_collisionNormals.Keys.Count > 0)
        {
            IsGrounded = true;
            foreach (var normals in _collisionNormals.Values)
            {
                foreach (var normal in normals)
                {
                    average += normal;
#if DEBUG
                    DebugExtension.DebugArrow(origin, -normal, Color.cyan);
#endif
                }
            }
#if DEBUG
            DebugExtension.DebugArrow(origin, -average.normalized, Color.blue);
#endif
        }
        else
        {
            IsGrounded = CheckDown(ref average);
        }

        if (average != Vector3.zero)
        {
            SurfaceNormal = average.normalized;
        }
        else
        {
            SurfaceNormal = Vector3.up;
        }
    }

    /// <summary>
    /// Calculates an average normal for the surface beneath the snail, allows for rotating around lips
    /// </summary>
    /// <param name="average">The calculated average surface normal</param>
    /// <returns>True if the down checks hit something</returns>
    private bool CheckDown(ref Vector3 average)
    {
#if DEBUG
        Vector3 origin = transform.position + transform.up * _collider.radius * 2.5f;
#endif
        Vector3 center = _collider.bounds.center;
        Vector3 avg = Vector3.zero;
        bool anyHit;

		var frontHits = GetDownHits(center + transform.forward * _groundCheckRange, -transform.forward);
		var backHits = GetDownHits(center - transform.forward * _groundCheckRange, transform.forward);

		if (frontHits.Length > 0)
		{
			foreach (var hit in frontHits)
			{
				avg += hit.normal;
#if DEBUG
				DebugExtension.DebugArrow(origin, -hit.normal, Color.magenta);
#endif
			}
		}

		if (backHits.Length > 0)
		{
			foreach (var hit in backHits)
			{
				avg += hit.normal;
#if DEBUG
				DebugExtension.DebugArrow(origin, -hit.normal, Color.magenta);
#endif
			}
		}

		average = avg.normalized;
#if DEBUG
        DebugExtension.DebugArrow(origin, -average, Color.red);
#endif
        anyHit = frontHits.Length > 0 || backHits.Length > 0;
        return anyHit;
    }

    private Vector3 GetNormal(Collider coll, Vector3 center, float radius)
    {
        Vector3 closestPoint;

        if (coll is MeshCollider && !((MeshCollider)coll).convex)
        {
            closestPoint = GetClosestPoint((MeshCollider)coll, center, radius);
        }
        else
        {
            closestPoint = coll.ClosestPoint(center);
        }

        Vector3 avg = Vector3.zero;
        Vector3 direction = (closestPoint - center).normalized;
        Ray ray = new Ray(center, direction);
        float distance = Vector3.Distance(closestPoint, center);
#if DEBUG
        DebugExtension.DebugCapsule(center, closestPoint, Color.green, _groundCheckRange);
#endif
        RaycastHit hit;
        if(Physics.SphereCast(ray, _groundCheckRange, out hit, distance, _groundLayer))
        {
            avg = hit.normal;
#if DEBUG
            DebugExtension.DebugArrow(hit.point, hit.normal * 3f, Color.white);
#endif
        }

        Vector3 normal = avg.normalized;
#if DEBUG
        DebugExtension.DebugArrow(closestPoint, normal * 3f, Color.gray);
#endif
        return normal;
    }

    private Vector3 GetClosestPoint(MeshCollider coll, Vector3 center, float radius)
    {
        BSPTree bspTree = coll.GetComponent<BSPTree>();
        if (bspTree == null)
        {
            Debug.LogErrorFormat("{0} has a concave mesh collider, but no BSPTree component! This is a bad thing.", coll.name);
            return Vector3.zero;
        }
        return bspTree.ClosestPoint(center, radius);
    }

    private RaycastHit[] GetDownHits(Vector3 origin, Vector3 backupDirection)
    {
        float radius = _collider.radius;
        Ray ray = new Ray(origin, -transform.up);
        var hits = Physics.SphereCastAll(ray, _groundCheckRange, radius, _groundLayer);
        if (hits.Length <= 0)
        {
            Ray backupRay = new Ray(origin - transform.up * radius, backupDirection);
            DebugExtension.DebugWireSphere(backupRay.origin + backupDirection * _groundCheckRange, Color.blue, _groundCheckRange);
            hits = Physics.SphereCastAll(backupRay, _groundCheckRange, radius, _groundLayer);
        }
        else
        {
            DebugExtension.DebugWireSphere(origin - transform.up * radius, Color.blue, _groundCheckRange);
        }
        return hits;
    }

    /// <summary>
    /// Smoothly transitions snail between the current normal (MyNormal) and the desired normal (SurfaceNormal)
    /// </summary>
    private void LerpRotationToSurfaceNormal()
    {
        MyNormal = Vector3.Lerp(MyNormal, SurfaceNormal, _lerpSpeed * Time.deltaTime);
        Vector3 myForward = Vector3.Cross(transform.right, MyNormal);
        Quaternion targetRotation = Quaternion.LookRotation(myForward, MyNormal);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, _lerpSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Unity Message: Collects collision information for consumption in AverageContactPoints
    /// </summary>
    /// <param name="collision">The collision that happened</param>
    private void OnCollisionEnter(Collision collision)
    {
        if (!_groundLayer.ContainsLayer(collision.collider.gameObject.layer)) return;

        List<Vector3> normals;
        if (_collisionNormals.TryGetValue(collision.collider, out normals))
        {
            normals.Clear();
        }
        else
        {
            normals = new List<Vector3>();
            _collisionNormals.Add(collision.collider, normals);
        }
        foreach (var contact in collision.contacts)
        {
            normals.Add(contact.normal);
        }
    }

    /// <summary>
    /// Unity Message: Collects collision information for consumption in AverageContactPoints
    /// </summary>
    /// <param name="collision">The collision that is still happening</param>
    private void OnCollisionStay(Collision collision)
    {
        if (!_groundLayer.ContainsLayer(collision.collider.gameObject.layer)) return;

        List<Vector3> normals;
        if (_collisionNormals.TryGetValue(collision.collider, out normals))
        {
            normals.Clear();
        }
        else
        {
            normals = new List<Vector3>();
            _collisionNormals.Add(collision.collider, normals);
        }
        foreach (var contact in collision.contacts)
        {
            normals.Add(contact.normal);
        }
    }

    /// <summary>
    /// Unity Message: Cleans up collision information when the collision stops
    /// </summary>
    /// <param name="collision"> The collision that stopped happening</param>
    private void OnCollisionExit(Collision collision)
    {
        if (!_groundLayer.ContainsLayer(collision.collider.gameObject.layer)) return;

        _collisionNormals.Remove(collision.collider);
    }
}
