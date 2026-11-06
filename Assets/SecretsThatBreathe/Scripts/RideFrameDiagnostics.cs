using System;
using System.Globalization;
using System.IO;
using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace SecretsThatBreathe
{
    // A bounded in-memory trace. No per-frame strings, logging or disk writes.
    // Saved on quit / scene unload so a real Player run can be inspected afterwards.
    [DefaultExecutionOrder(30000)]
    public sealed class RideFrameDiagnostics : MonoBehaviour
    {
        private struct Sample
        {
            public int frame, collections;
            public double seconds;
            public float frameMs, routeRemaining;
            public long allocatedBytes, mainNs, renderNs;
        }
        private const int Capacity = 16384;
        private readonly Sample[] samples = new Sample[Capacity];
        private int written, previousCollections;
        private PorschePassengerRide ride;
        private ProfilerRecorder allocations, mainThread, renderThread;
        private bool saved;
        private string sceneName;

        private void Awake()
        {
            ride = GetComponent<PorschePassengerRide>();
            sceneName = gameObject.scene.name;
            previousCollections = GC.CollectionCount(0);
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1);
            mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1);
            renderThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Render Thread", 1);
        }
        private void LateUpdate()
        {
            int collections = GC.CollectionCount(0);
            if (ride != null && ride.isActiveAndEnabled && ride.IsMoving
                && Time.timeScale > 0f && !EveningDriveIntro.IsInputBlocked(gameObject.scene))
            {
                samples[written % Capacity] = new Sample
                {
                    frame = Time.frameCount,
                    seconds = Time.realtimeSinceStartupAsDouble,
                    frameMs = Time.unscaledDeltaTime * 1000f,
                    routeRemaining = ride.RemainingDistance,
                    collections = collections - previousCollections,
                    allocatedBytes = Read(allocations), mainNs = Read(mainThread), renderNs = Read(renderThread)
                };
                written++;
            }
            previousCollections = collections;
        }
        private static long Read(ProfilerRecorder recorder)
            => recorder.Valid && recorder.Count > 0 ? recorder.LastValue : -1;
        private void OnApplicationQuit() => Save();
        private void OnDestroy()
        {
            Save();
            allocations.Dispose(); mainThread.Dispose(); renderThread.Dispose();
        }
        private void Save()
        {
            if (saved || written == 0) return;
            saved = true;
            try
            {
                var text = new StringBuilder(Math.Min(written, Capacity) * 90);
                text.AppendLine("# Ride frame trace; -1 = counter unavailable. Counter timings describe the preceding completed frame; they are clues, not a CPU/GPU diagnosis.");
                text.AppendLine("frame,realtime_seconds,frame_ms,route_remaining_m,gc_collections,gc_bytes,main_thread_ns,render_thread_ns");
                for (int i = Math.Max(0, written - Capacity); i < written; i++)
                {
                    Sample sample = samples[i % Capacity];
                    text.Append(sample.frame).Append(',')
                        .Append(sample.seconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                        .Append(sample.frameMs.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                        .Append(sample.routeRemaining.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                        .Append(sample.collections).Append(',').Append(sample.allocatedBytes).Append(',')
                        .Append(sample.mainNs).Append(',').Append(sample.renderNs).AppendLine();
                }
                string path = Path.Combine(Application.persistentDataPath, "RideFrameTrace-" + sceneName + ".csv");
                File.WriteAllText(path, text.ToString());
            }
            catch (Exception error) { Debug.LogWarning("Could not save ride frame trace: " + error.Message); }
        }
    }
}
