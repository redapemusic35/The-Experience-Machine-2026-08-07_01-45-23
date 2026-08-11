using UnityEngine;
using Yarn.Unity;

public class PhaedrusWalk : MonoBehaviour
{
    enum Phase { Waiting, Joining, Walking, Arrived, Dismissed }

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [SerializeField] private string joinNode = "Socrates_Join";
    [SerializeField] private string[] walkingNodes;      // fired en route, in order
    [SerializeField] private string arrivalNode = "PlaneTree";

    [Header("Trigger")]
    [SerializeField] private float triggerRadius = 6f;

    [Header("Destination")]
    [SerializeField] private Transform destination;
    [SerializeField] private float arriveDistance = 3f;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 2f;
    [SerializeField] private float stopDistance = 2.5f;
    [SerializeField] private float turnSpeed = 5f;

    private Transform player;
    private Phase phase = Phase.Waiting;
    private int nextWalkingLine = 0;
    private float startDistanceToDest;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        else Debug.LogError("No object tagged 'Player' found.");

        if (dialogueRunner != null)
            dialogueRunner.onDialogueComplete.AddListener(OnDialogueComplete);
    }

    void Update()
    {
        if (player == null || destination == null) return;

        switch (phase)
        {
            case Phase.Waiting:
                if (Vector3.Distance(transform.position, player.position) <= triggerRadius)
                {
                    startDistanceToDest = Vector3.Distance(player.position, destination.position);
                    Speak(joinNode);
                    phase = Phase.Joining;
                }
                break;

            case Phase.Joining:
                Follow();   // he starts walking as soon as he's spoken
                break;

            case Phase.Walking:
                Follow();
                CheckWalkingLines();
                if (Vector3.Distance(player.position, destination.position) <= arriveDistance)
                {
                    phase = Phase.Arrived;
                    Speak(arrivalNode);
                }
                break;

            case Phase.Arrived:
                FaceTarget(player.position);   // stands and faces you
                break;

            case Phase.Dismissed:
                break;
        }
    }

    void CheckWalkingLines()
    {
        if (walkingNodes == null || nextWalkingLine >= walkingNodes.Length) return;
        if (dialogueRunner.IsDialogueRunning) return;

        // space the lines evenly across the journey
        float remaining = Vector3.Distance(player.position, destination.position);
        float progress = 1f - (remaining / Mathf.Max(startDistanceToDest, 0.01f));
        float threshold = (nextWalkingLine + 1f) / (walkingNodes.Length + 1f);

        if (progress >= threshold)
        {
            Speak(walkingNodes[nextWalkingLine]);
            nextWalkingLine++;
        }
    }

    void Speak(string node)
    {
        if (dialogueRunner != null && !dialogueRunner.IsDialogueRunning && !string.IsNullOrEmpty(node))
            dialogueRunner.StartDialogue(node);
    }

    void OnDialogueComplete()
    {
        if (phase == Phase.Joining) phase = Phase.Walking;
        else if (phase == Phase.Arrived) phase = Phase.Dismissed;
    }

    void Follow()
    {
        float distance = Vector3.Distance(transform.position, player.position);
        FaceTarget(player.position);
        if (distance <= stopDistance) return;

        Vector3 flat = new Vector3(player.position.x, transform.position.y, player.position.z);
        Vector3 dir = (flat - transform.position).normalized;
        Vector3 next = transform.position + dir * followSpeed * Time.deltaTime;
        next.y = GroundHeight(next);
        transform.position = next;
    }

    void FaceTarget(Vector3 target)
    {
        Vector3 flat = new Vector3(target.x, transform.position.y, target.z);
        Vector3 dir = (flat - transform.position).normalized;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.LookRotation(dir), turnSpeed * Time.deltaTime);
    }

    float GroundHeight(Vector3 pos)
    {
        if (Terrain.activeTerrain != null)
            return Terrain.activeTerrain.SampleHeight(pos) + Terrain.activeTerrain.transform.position.y;
        return pos.y;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);
        if (destination != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(destination.position, arriveDistance);
            Gizmos.DrawLine(transform.position, destination.position);
        }
    }
}