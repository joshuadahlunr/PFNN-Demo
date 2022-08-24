using UnityEngine;

[RequireComponent(typeof(ConfigurableJoint))]
public class JointMatchRotation : MonoBehaviour {
	public Transform target;
	private Quaternion targetInitialRotation;

	// [FormerlySerializedAs("K")]
	// public float springConstant = 1;
	// public float maxForce = 2000;
	public float rotationStrength = 30;
	public float rotationThreshold = 5f;

	private ConfigurableJoint joint;
	private Rigidbody rb;

	private void Awake() {
		joint = GetComponent<ConfigurableJoint>();
		rb = joint.GetComponent<Rigidbody>();
		targetInitialRotation = target.localRotation;
	}

	private void FixedUpdate() {
		// From: https://amebouslabs.com/developing-physics-based-vr-hands-in-unity/
		var angleDistance = Quaternion.Angle(transform.rotation, target.rotation);
		if (angleDistance < rotationThreshold)
			rb.MoveRotation(target.rotation);
		else {
			var kp = 6f * rotationStrength * (6f * rotationStrength) * 0.25f;
			var kd = 4.5f * rotationStrength;
			Vector3 x;
			float xMag;
			var q = target.rotation * Quaternion.Inverse(transform.rotation);
			q.ToAngleAxis(out xMag, out x);
			x.Normalize();
			x *= Mathf.Deg2Rad;
			var pidv = x * (kp * xMag) - kd * rb.angularVelocity;
			var rotInertia2World = rb.inertiaTensorRotation * transform.rotation;
			pidv = Quaternion.Inverse(rotInertia2World) * pidv;
			pidv.Scale(rb.inertiaTensor);
			pidv = rotInertia2World * pidv;
			rb.AddTorque(pidv);
		}
	}
}
