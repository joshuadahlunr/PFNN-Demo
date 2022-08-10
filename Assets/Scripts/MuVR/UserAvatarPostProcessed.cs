using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;

namespace MuVR {
	public abstract class UserAvatarPostProcessed : UserAvatar {
		public struct PostProcessData {
			public enum ProcessMode {
				Process,
				Copy,
				Ignore,
			}
			public ProcessMode processMode;
			public readonly PoseRef poseRef;

			public PostProcessData(ProcessMode processMode) {
				this.processMode = processMode;
				poseRef = new PoseRef();
			}
		}
		
		protected readonly Dictionary<string, PostProcessData> rawSlotData = new();
		
		public void Awake() {
			foreach (var slot in slots.Keys) 
				rawSlotData[slot] = new PostProcessData(PostProcessData.ProcessMode.Process);
		}
		
		public override PoseRef GetterPoseRef(string slot) => slots[slot];
		public override PoseRef SetterPoseRef(string slot) => rawSlotData[slot].poseRef;
		
		public virtual ref Pose GetProcessedPose(string slot) => ref GetterPoseRef(slot).pose;
		public virtual ref Pose GetRawPose(string slot) => ref SetterPoseRef(slot).pose;
		
		public virtual bool ShouldProcess(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Process;
		public virtual bool ShouldCopy(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Copy;
		public virtual bool ShouldIgnore(string slot) => rawSlotData[slot].processMode == PostProcessData.ProcessMode.Ignore;
		public void SetProcessMode(string slot, PostProcessData.ProcessMode value = PostProcessData.ProcessMode.Process) {
			var data = rawSlotData[slot];
			data.processMode = value;
			rawSlotData[slot] = data;
		}

		public void Update() {
			// Process all of the data that should be processed
			foreach (var slot in rawSlotData.Keys.Where(ShouldProcess)) 
				GetProcessedPose(slot) = OnPostProcess(slot, GetProcessedPose(slot), GetRawPose(slot));

			// Copy all of the data that should be copied
			foreach (var slot in rawSlotData.Keys.Where(ShouldCopy))
				GetProcessedPose(slot) = GetRawPose(slot);
		}

		// Function that can be overridden in derived classes to process the data in some way
		public abstract Pose OnPostProcess(string slot, Pose processed, Pose raw);
	}
}