using UnityEngine;
using System.Collections;

public class Act1SequenceManager : MonoBehaviour
{
    [Header("เป้าหมายการสำรวจ")]
    public int dummyCluesRequired = 2; 
    private int currentCluesFound = 0;

    [Header("องค์ประกอบฉาก")]
    public GameObject ghostTriggerZone; 

    void Start()
    {
        if(ghostTriggerZone) ghostTriggerZone.SetActive(false);
    }

    public void AddDummyClueFound()
    {
        currentCluesFound++;
        if (currentCluesFound >= dummyCluesRequired)
        {
            StartCoroutine(TriggerFrustrationPhase());
        }
    }

    private IEnumerator TriggerFrustrationPhase()
    {
        yield return new WaitForSeconds(1.5f);
        Debug.Log("เข้ม: มันต้องมีสิ... ตำรวจมันชุ่ย... มันต้องทิ้งอะไรไว้บ้าง!!");
        yield return new WaitForSeconds(2.0f);
        if(ghostTriggerZone) ghostTriggerZone.SetActive(true);
    }
}