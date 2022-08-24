using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Rigidbody))]
public class JointToTarget : MonoBehaviour {
    public Transform target;
    
    [FormerlySerializedAs("K")]
    public float springConstant = 1;
    public float maxForce = 2000;
    
    private Rigidbody rb;

    private void Awake() => rb = GetComponent<Rigidbody>();

    private void FixedUpdate() {
        var force = (target.position - transform.position) * springConstant;
        force = force.normalized * Mathf.Min(force.magnitude, maxForce);
        rb.AddForce(force);

#if UNITY_EDITOR
        Debug.DrawRay(transform.position, force, Color.red);
#endif
    }
}
