using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Recorded key attacks are shared by both Intro scenes. Audio playback position
    // is the clock, so frame rate, pauses and source pitch cannot desynchronise text.
    internal static class AudioSynchronizedTypewriter
    {
        [Serializable]
        private sealed class Timing
        {
            public string clipName;
            public float clipLength;
            public float[] keyTimes;
        }

        public static IEnumerator Play(TMP_Text label, AudioSource audio, TextAsset timingAsset, float fallbackInterval)
        {
            label.maxVisibleCharacters = 0;
            label.ForceMeshUpdate();
            int count = label.textInfo.characterCount;
            if (count == 0) yield break;

            Timing timing = timingAsset != null ? JsonUtility.FromJson<Timing>(timingAsset.text) : null;
            AudioClip clip = audio != null ? audio.clip : null;
            bool synchronised = clip != null && audio.isActiveAndEnabled && timing != null
                && timing.keyTimes != null && timing.keyTimes.Length > 0
                && timing.clipName == clip.name && Mathf.Abs(timing.clipLength - clip.length) < .1f;

            if (!synchronised)
            {
                if (audio != null && audio.isActiveAndEnabled && clip != null) audio.Play();
                yield return RevealUniform(label, count, fallbackInterval);
                if (audio != null) audio.Stop();
                yield break;
            }

            List<int> ends = RevealEnds(label);
            bool previousLoop = audio.loop;
            audio.Stop();
            audio.loop = true;
            audio.timeSamples = 0;
            audio.Play();
            int previousSample = 0, revealed = 0;
            double loopOffset = 0d;
            float stoppedSeconds = 0f;
            while (revealed < ends.Count)
            {
                if (audio == null || !audio.isActiveAndEnabled || audio.clip != clip
                    || clip.loadState == AudioDataLoadState.Failed) break;
                int sample = audio.timeSamples;
                if (sample < previousSample) loopOffset += clip.length;
                previousSample = sample;
                double playbackTime = loopOffset + (double)sample / clip.frequency;
                while (revealed < ends.Count)
                {
                    double cue = (double)(revealed / timing.keyTimes.Length) * clip.length
                        + timing.keyTimes[revealed % timing.keyTimes.Length];
                    if (playbackTime < cue) break;
                    label.maxVisibleCharacters = ends[revealed++];
                }
                // If an unavailable source cannot start, continue reading rather than
                // trapping the player in Intro. Losing focus simply pauses this check.
                stoppedSeconds = !audio.isPlaying && Application.isFocused
                    ? stoppedSeconds + Time.unscaledDeltaTime : 0f;
                if (stoppedSeconds >= 1f) break;
                yield return null;
            }

            if (revealed < ends.Count) yield return RevealUniform(label, count, fallbackInterval);
            else yield return new WaitForSecondsRealtime(.06f); // Let the last key's short tail sound.
            if (audio != null)
            {
                audio.Stop();
                audio.loop = previousLoop;
            }
        }

        private static List<int> RevealEnds(TMP_Text label)
        {
            var ends = new List<int>();
            for (int i = 0; i < label.textInfo.characterCount; i++)
            {
                char character = label.textInfo.characterInfo[i].character;
                UnicodeCategory category = char.GetUnicodeCategory(character);
                bool attached = char.IsWhiteSpace(character) || category == UnicodeCategory.NonSpacingMark
                    || category == UnicodeCategory.SpacingCombiningMark || category == UnicodeCategory.EnclosingMark
                    || category == UnicodeCategory.Format;
                if (attached && ends.Count > 0) ends[ends.Count - 1] = i + 1;
                else if (!attached) ends.Add(i + 1);
            }
            // Whitespace-only authored text still completes without a typing loop.
            if (ends.Count == 0 && label.textInfo.characterCount > 0) ends.Add(label.textInfo.characterCount);
            return ends;
        }

        private static IEnumerator RevealUniform(TMP_Text label, int count, float interval)
        {
            int initial = label.maxVisibleCharacters;
            float elapsed = 0f;
            while (label.maxVisibleCharacters < count)
            {
                elapsed += Time.unscaledDeltaTime;
                label.maxVisibleCharacters = Mathf.Min(count, initial + Mathf.FloorToInt(elapsed / Mathf.Max(.001f, interval)));
                yield return null;
            }
        }
    }
}
