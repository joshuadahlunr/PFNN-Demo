using UnityEngine;

namespace MuVR {
	
	public class WeightedUserAvatar : UserAvatarPostProcessed {
		public float positionAlpha = .9f;
		public float rotationAlpha = .9f;
		public float timeScale = 60;

		public override Pose OnPostProcess(string slot, Pose smoothed, Pose unsmoothed, float dt) {
			var modified = new Pose {
				position = positionAlpha * smoothed.position + (1 - positionAlpha) * unsmoothed.position,
				rotation = Quaternion.Slerp(smoothed.rotation, unsmoothed.rotation, 1 - rotationAlpha)
			};
			// Preform the blending with respect to time
			return Lerp(smoothed, modified, dt * timeScale);
		}
		
		public static Pose Lerp(Pose a, Pose b, float t) {
			Pose ret;
			ret.position = Vector3.Lerp(a.position, b.position, t);
			ret.rotation = Quaternion.Lerp(a.rotation, b.rotation, t);
			return ret;
		}
	}
}