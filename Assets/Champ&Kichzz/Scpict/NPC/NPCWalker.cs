using UnityEngine;

// สร้างกล่องสำหรับเก็บ "ชุดบทสนทนา" 1 ชุด (ชื่อคู่กับบทพูด)
[System.Serializable]
public struct DialogSet
{
    public string[] speakerNames;
    [TextArea(2, 4)] public string[] dialogLines;
}

public class NPCWalker : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 2f;
    public Animator animator;
    
    [HideInInspector] public Transform[] waypoints;
    private int currentWaypointIndex = 0;
    private bool isTalking = false;

    [Header("ชุดบทสนทนา (เพิ่มได้หลายรูปแบบ)")]
    public DialogSet[] possibleDialogs; // กล่องเก็บชุดบทสนทนาทั้งหมด

    void Update()
    {
        if (isTalking || waypoints == null || waypoints.Length == 0) return;

        Transform target = waypoints[currentWaypointIndex];
        transform.position = Vector3.MoveTowards(transform.position, target.position, walkSpeed * Time.deltaTime);

        Vector3 dir = (target.position - transform.position).normalized;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            currentWaypointIndex++;
            if (currentWaypointIndex >= waypoints.Length)
            {
                Destroy(gameObject);
            }
        }

        if (animator) animator.SetBool("isWalking", true);
    }

    public void StartTalking()
    {
        if (isTalking) return; // กันกดปุ่มรัวๆ
        isTalking = true;
        
        if (animator) animator.SetBool("isWalking", false);

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Vector3 lookPos = player.transform.position;
            lookPos.y = transform.position.y;
            transform.LookAt(lookPos);
        }

        // --- ระบบสุ่มเลือกบทสนทนา ---
        if (DialogSystem.instance != null && possibleDialogs.Length > 0)
        {
            // สุ่มตัวเลขตั้งแต่ 0 ถึงจำนวนชุดข้อมูลที่มี
            int randomIndex = Random.Range(0, possibleDialogs.Length);
            DialogSet selectedDialog = possibleDialogs[randomIndex];

            // ส่งชุดที่สุ่มได้ไปให้ระบบ DialogSystem แสดงผล
            DialogSystem.instance.StartDialog(this, selectedDialog.speakerNames, selectedDialog.dialogLines);
        }
        else
        {
            // ถ้าลืมใส่บทสนทนาไว้ ให้ปล่อย NPC เดินต่อเลย จะได้ไม่ค้าง
            EndTalking();
        }
    }

    public void EndTalking()
    {
        isTalking = false;
    }
}