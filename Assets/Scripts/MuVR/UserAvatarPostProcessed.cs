using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MuVR {
	public abstract class UserAvatarPostProcessed : UserAvatar {
		#region Static Reference Management

		protected static UserAvatarPostProcessed[] inScene;
		protected uint indexInScene;

		protected void OnEnable() {
			if (inScene is null) {
				inScene = new[] { this };
				indexInScene = 0;
				return;
			}
			
			inScene = new List<UserAvatarPostProcessed>(inScene) { this }.ToArray();
			indexInScene = (uint)(inScene.Length - 1);
		}
		protected void OnDisable() {
			var list = new List<UserAvatarPostProcessed>(inScene);
			list.Remove(this);
			inScene = list.Count > 0 ? list.ToArray() : null;
		}

		#endregion
		
		#region Types

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

		protected struct PostProcessJob : IJobParallelFor {
			public uint ownerID; // The ID of the UserAvatar in the static references
			public NativeArray<byte>.ReadOnly keyData;
			public NativeArray<long>.ReadOnly keyStarts;

			public void Execute(int index) {
				var start = (int)keyStarts[index];
				var end = (int)(index < keyStarts.Length - 1 ? keyStarts[index + 1] : keyData.Length);
				
				var slot = Encoding.UTF8.GetString(keyData.Skip(start).Take(end - start).ToArray());
				var owner = UserAvatarPostProcessed.inScene[ownerID];
				
				if(owner.ShouldProcess(slot))
					owner.GetProcessedPose(slot) = owner.OnPostProcess(slot, owner.GetProcessedPose(slot), owner.GetRawPose(slot));
				else if(owner.ShouldCopy(slot))
					owner.GetProcessedPose(slot) = owner.GetRawPose(slot);
			}
		} 

		#endregion

		// Weather or not post processing should be performed using jobs or sequentially 
		public bool useJobs = true;
		
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

		// Native arrays 
		protected NativeArray<byte> keysNativeArray;
		protected NativeArray<long> startsNativeArray;
		protected JobHandle postProcessJob;
		public void OnDestroy() {
			if (keysNativeArray.IsCreated) keysNativeArray.Dispose();
			if (startsNativeArray.IsCreated) startsNativeArray.Dispose();
		}

		public void Update() {
			#region Jobs

			if (useJobs) {
				// If the old arrays are dirty
				if (!keysNativeArray.IsCreated || !startsNativeArray.IsCreated || startsNativeArray.Length != rawSlotData.Count) {
					// Dispose of the old arrays (if they exists)
					if (keysNativeArray.IsCreated)
						keysNativeArray.Dispose();
					if (startsNativeArray.IsCreated)
						startsNativeArray.Dispose();

					// Allocate a new arrays
					rawSlotData.Keys.Select(s => Encoding.UTF8.GetBytes(s)).Concatentate(out var data, out var starts);
					keysNativeArray = new NativeArray<byte>(data, Allocator.Persistent);
					startsNativeArray = new NativeArray<long>(starts, Allocator.Persistent);
				}

				postProcessJob = new PostProcessJob {
					ownerID = indexInScene,
					keyData = keysNativeArray.AsReadOnly(),
					keyStarts = startsNativeArray.AsReadOnly()
				}.Schedule(rawSlotData.Count, 1); //.Complete();
			}

			#endregion

			else
				
			#region Serial

			{
				// Process all of the data that should be processed
				foreach (var slot in rawSlotData.Keys.Where(ShouldProcess))
					GetProcessedPose(slot) = OnPostProcess(slot, GetProcessedPose(slot), GetRawPose(slot));

				// Copy all of the data that should be copied
				foreach (var slot in rawSlotData.Keys.Where(ShouldCopy))
					GetProcessedPose(slot) = GetRawPose(slot);
			}

			#endregion
		}

		// After everything else has updated... make sure that our job is finished
		public void LateUpdate() => postProcessJob.Complete();

		// Function that can be overridden in derived classes to process the data in some way
		public abstract Pose OnPostProcess(string slot, Pose processed, Pose raw);
	}
}