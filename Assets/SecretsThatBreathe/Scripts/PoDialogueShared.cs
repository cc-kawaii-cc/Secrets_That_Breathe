using System.Collections;
using TMPro;
using UnityEngine;

namespace SecretsThatBreathe
{
    // Shared by the Po cell-block scripts (PoGlassDoorVoice, PoSleepingPoliceInteraction): only one of them is speaking at a time,
    // and both type their lines into the project's authored FlowingDialogue Canvases.
    internal static class PoDialogueGate
    {
        public static bool Busy { get; private set; }
        public static bool TryEnter()
        {
            if (Busy) return false;
            Busy = true;
            return true;
        }
        public static void Leave() => Busy = false;

        // With "Enter Play Mode Options" the static would survive between runs.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay() => Busy = false;
    }

    internal sealed class PoDialoguePlayer
    {
        private FlowingDialogue flowing;
        private DialogueCanvasView shown;

        // Types every saved label into the Canvas, one after another (same behaviour as the duty officer's SayAll).
        public IEnumerator Say(DialogueCanvasView view, FlowingDialogueSettings presentation, TMP_Text[] lines)
        {
            if (lines == null) yield break;
            foreach (var line in lines)
            {
                if (line == null) continue;
                Stop();
                ApplySentenceStyle(view, line);
                flowing = new FlowingDialogue(view, presentation);
                shown = view;
                yield return flowing.Play(line.text);
            }
        }

        public void Stop()
        {
            if (flowing != null && Alive(shown)) flowing.Hide();
            flowing = null;
        }

        // When Play Mode stops, Unity destroys the Canvas objects before OnDisable runs.
        public static bool Alive(DialogueCanvasView view) => view != null && view.IsValid && view.sentence != null;

        private static void ApplySentenceStyle(DialogueCanvasView canvas, TMP_Text line)
        {
            if (line == null || canvas.sentence == null) return;
            var target = canvas.sentence;
            target.font = line.font; target.fontSharedMaterial = line.fontSharedMaterial;
            target.fontSize = line.fontSize; target.fontStyle = line.fontStyle; target.fontWeight = line.fontWeight;
            target.color = line.color; target.characterSpacing = line.characterSpacing;
            target.wordSpacing = line.wordSpacing; target.lineSpacing = line.lineSpacing; target.paragraphSpacing = line.paragraphSpacing;
            target.enableAutoSizing = line.enableAutoSizing; target.fontSizeMin = line.fontSizeMin; target.fontSizeMax = line.fontSizeMax;
        }
    }
}
