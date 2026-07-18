using UnityEngine;
using System.Collections;

public class NPCSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public GameObject npcPrefab;
    public int maxNPCs = 3; 
    
    [Header("เวลาสุ่มเกิด (วินาที)")]
    public float minSpawnDelay = 10;  
    public float maxSpawnDelay = 80f; 

    [Header("NPC Path")]
    public Transform[] pathForNPCs; 

    void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            GameObject[] npcs = GameObject.FindGameObjectsWithTag("NPC");
            
            if (npcs.Length < maxNPCs)
            {
                SpawnNPC();
            }
            
           
            float randomDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
            yield return new WaitForSeconds(randomDelay);
        }
    }

    void SpawnNPC()
    {
        GameObject newNPC = Instantiate(npcPrefab, transform.position, transform.rotation);
        newNPC.tag = "NPC"; 
        
        NPCWalker walker = newNPC.GetComponent<NPCWalker>();
        if (walker != null)
        {
            walker.waypoints = pathForNPCs;
        }
    }
}