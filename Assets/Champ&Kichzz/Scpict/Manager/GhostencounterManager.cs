using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
public class GhostEncounterManager : MonoBehaviour
{
    [Header("Atmosphere")]
    public AudioSource rainAudio;
    public Volume glitchVolume;

    [Header("Ghost")]
    public GameObject ghostEntity;
    public Vector3 ghostFaceOffset = new Vector3(0f, 1.5f, 0f);

    [Header("The Inevitable Car")]
    public GameObject fakeCar;
    public Transform carSpawnPoint;
    public Vector3 carFaceOffset = new Vector3(0f, 1f, 0f);
    public float carTurnDuration = 0.5f;
    public float carSpeed = 25f;
    public AudioSource carRushAudio;
    public AudioSource jumpscareAudio;

    [Header("Payoff")]
    public GameObject finalEvidence;
    public string evidenceClueId = "bumper_fragment_red";

    [Header("Subtitles (กด + เพื่อเพิ่มบรรทัด)")]
    [TextArea(2, 3)] public string[] flashlightGlitchTexts;
    [TextArea(2, 3)] public string[] ghostAppearTexts;
    [TextArea(2, 3)] public string[] aftermathTexts;
    public float subtitleDuration = 3f;

    private bool _triggered;

    private void Start()
    {
        if (ghostEntity) ghostEntity.SetActive(false);
        if (fakeCar) fakeCar.SetActive(false);
        if (glitchVolume) glitchVolume.weight = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_triggered || !other.CompareTag("Player")) return;
        _triggered = true;
        StartCoroutine(Sequence());
    }

    private IEnumerator Sequence()
    {
        var pm = PlayerManager.Instance;
        GameManager.Instance.SetState(GameState.Cutscene);
        GameEvents.CutsceneStart();

        float startRainVol = rainAudio ? rainAudio.volume : 1f;
        
        if (rainAudio) rainAudio.volume = 0f;
        if (glitchVolume) glitchVolume.weight = 1f;
        if (pm != null && pm.flashlight) pm.flashlight.StartGlitchFlicker(3.5f);
        SubtitleManager.Instance.Show(flashlightGlitchTexts, subtitleDuration);
        yield return new WaitForSeconds(1.0f);
        
        if (ghostEntity) ghostEntity.SetActive(true);
        SubtitleManager.Instance.Show(ghostAppearTexts, subtitleDuration);
        if (ghostEntity && pm != null)
            yield return CinematicCamera.Instance.TurnBodyAndLookAt(
                pm.playerBody, ghostEntity.transform, 0.8f, ghostFaceOffset);

        yield return new WaitForSeconds(1.5f);
        
        if (fakeCar && carSpawnPoint && pm != null)
        {
            fakeCar.transform.position = carSpawnPoint.position;
            fakeCar.SetActive(true);
            if (carRushAudio) carRushAudio.Play();

            // หันตัว+กล้องไปทางรถที่กำลังพุ่งเข้ามา
            yield return CinematicCamera.Instance.TurnBodyAndLookAt(
                pm.playerBody, fakeCar.transform, carTurnDuration, carFaceOffset);

            while (Vector3.Distance(fakeCar.transform.position, pm.playerBody.position) > 2.5f)
            {
                Vector3 dir = (pm.playerBody.position - fakeCar.transform.position).normalized;
                fakeCar.transform.position += dir * carSpeed * Time.deltaTime;
                fakeCar.transform.LookAt(pm.playerBody);
                yield return null;
            }
        }
        
        if (jumpscareAudio) jumpscareAudio.Play();
        if (glitchVolume) glitchVolume.weight = 1f;
        if (fakeCar) fakeCar.SetActive(false);
        if (ghostEntity) ghostEntity.SetActive(false);
        yield return new WaitForSeconds(0.8f);

        if (glitchVolume) glitchVolume.weight = 0f;
        if (rainAudio) rainAudio.volume = startRainVol;
        
        CinematicCamera.Instance.EndOverride();
        GameManager.Instance.SetState(GameState.Exploration);
        GameEvents.CutsceneEnd();

        SubtitleManager.Instance.Show(aftermathTexts, subtitleDuration);
        if (finalEvidence) finalEvidence.SetActive(true);
        GameEvents.ClueFound(evidenceClueId);
    }
}