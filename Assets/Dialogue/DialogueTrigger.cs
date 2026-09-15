using UnityEngine;
using Yarn.Unity;

/// <summary>
/// Socrates NPC controller for the Phaedrus opening walk.
///
/// Flow: seated in the grass -> player approaches -> stands and faces the player ->
/// walks WITH the player toward the plane tree -> whenever the player stops or falls
/// behind, steps into their line of sight and poses a question -> on arrival, sits
/// back down and the plane-tree conversation begins.
///
/// Dialogue-end is detected by polling DialogueRunner.IsDialogueRunning rather than
/// subscribing to events, because the event names differ across Yarn Spinner versions
/// and polling compiles against all of them.
/// </summary>
public class DialogueTrigger : MonoBehaviour
{
    private enum SocState { Seated, Rising, Greeting, Walking, Checkpoint, SittingDown, Arrived }

    [Header("Dialogue")]
    [SerializeField] private DialogueRunner dialogueRunner;
    [Tooltip("Optional. Played once when the player first comes within callOutRadius. Leave blank to disable.")]
    [SerializeField] private string callOutNode = "";
    [SerializeField] private float callOutRadius = 14f;
    [Tooltip("The node that runs when the player reaches triggerRadius and Socrates stands.")]
    [SerializeField] private string joinNode = "Part_Intro_Technophilosophy";
    [Tooltip("Question nodes, played one per stop, in order.")]
    [SerializeField] private string[] walkingNodes = new string[]
    {
        "Walk_Checkpoint_1",
        "Walk_Checkpoint_2",
        "Walk_Checkpoint_3",
        "Walk_Checkpoint_4"
    };
    [Tooltip("The node that runs once Socrates has sat down at the plane tree.")]
    [SerializeField] private string arrivalNode = "PlaneTree";

    [Header("Player")]
    [Tooltip("Drag the object that actually MOVES when you walk - the one carrying your camera and movement script. Leave empty to fall back to tag lookup.")]
    [SerializeField] private Transform playerOverride;

    [Header("Trigger & destination")]
    [SerializeField] private float triggerRadius = 3f;
    [SerializeField] private Transform destination;
    [SerializeField] private float arriveDistance = 3f;
    [Tooltip("Optional. Where Socrates sits at the plane tree. Falls back to the destination transform.")]
    [SerializeField] private Transform sitSpot;

    [Header("Movement")]
    [SerializeField] private Animator dialogueAnimator;
    [SerializeField] private CharacterController characterController;
    [Tooltip("Maximum walking speed. Socrates eases off as he closes on his slot beside the player.")]
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private float stopDistance = 0.35f;
    [SerializeField] private float turnSpeed = 8f;
    [Tooltip("Negative puts Socrates on the player's LEFT, so the player walks to his right.")]
    [SerializeField] private float sideOffset = -1f;
    [SerializeField] private float forwardOffset = 0.2f;
    [SerializeField] private string walkBoolParam = "IsWalking";

    [Header("Checkpoints")]
    [SerializeField] private float playerStoppedSpeed = 0.4f;
    [SerializeField] private float stoppedDwellTime = 0.9f;
    [SerializeField] private float fallBehindDistance = 6f;
    [SerializeField] private float checkpointCooldown = 6f;
    [Tooltip("How far in front of the player Socrates plants himself when he stops them.")]
    [SerializeField] private float lineOfSightDistance = 2.2f;

    [Header("Seated animation (enable once the Mixamo sit clips are imported)")]
    [SerializeField] private bool useSeatedAnimations = false;
    [SerializeField] private string seatedBoolParam = "IsSeated";
    [SerializeField] private string standUpTriggerParam = "StandUp";
    [SerializeField] private string sitDownTriggerParam = "SitDown";
    [SerializeField] private float standUpDuration = 1.4f;
    [SerializeField] private float sitDownDuration = 1.4f;

    [Header("Debug")]
    [Tooltip("Draws a live state readout in the top-left during play. Turn off when the walk behaves.")]
    [SerializeField] private bool showDebug = true;

    [Header("Grounding")]
    [Tooltip("Keeps Socrates on the terrain by probing downward each frame instead of accumulating gravity.")]
    [SerializeField] private bool snapToGround = true;
    [SerializeField] private LayerMask groundMask = ~0;
    [Tooltip("How far above his feet the ground probe starts.")]
    [SerializeField] private float groundProbeUp = 1.5f;
    [Tooltip("How far below his feet the ground probe reaches.")]
    [SerializeField] private float groundProbeDown = 80f;
    [Tooltip("If no ground is found below him, a second probe drops from this height to recover a character buried under raised terrain.")]
    [SerializeField] private float buriedRecoveryHeight = 300f;
    [SerializeField] private float groundOffset = 0f;
    [Tooltip("How fast he settles onto the ground height. Higher is snappier.")]
    [SerializeField] private float groundStick = 20f;
    [Tooltip("Only used when the probe finds nothing underneath him at all.")]
    [SerializeField] private float gravityStrength = 20f;
    [SerializeField] private float maxFallSpeed = 30f;

    private Transform player;
    private SocState state = SocState.Seated;

    private Vector3 lastPlayerPos;
    private float playerSpeed;
    private float stoppedTimer;
    private float cooldownTimer;
    private float stateTimer;
    private float verticalVelocity;

    private int checkpointIndex;
    private bool waitingForDialogue;
    private float dialogueGrace;
    private bool hasCalledOut;

    private Vector3 desiredMove;

    // ---------------------------------------------------------------- lifecycle

    private void Start()
    {
        player = FindPlayerTransform();
        if (player != null)
        {
            lastPlayerPos = player.position;
            Debug.Log($"[DialogueTrigger] Following '{player.name}'. If that object does not move when you walk, Socrates will treat you as standing still.", this);
        }

        if (dialogueRunner == null)
        {
            Debug.LogError("[DialogueTrigger] DialogueRunner is not assigned.", this);
        }

        if (destination == null)
        {
            Debug.LogWarning("[DialogueTrigger] No destination assigned - Socrates will greet but never walk.", this);
        }

        SetBool(walkBoolParam, false);
        if (useSeatedAnimations)
        {
            SetBool(seatedBoolParam, true);
        }

        // Put him on the ground once at spawn rather than letting him fall to it.
        if (snapToGround && TryGetGroundHeight(transform.position, out float y0))
        {
            Vector3 p = transform.position;
            p.y = y0 + groundOffset;
            transform.position = p;
        }
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        stateTimer += dt;
        if (cooldownTimer > 0f) cooldownTimer -= dt;
        if (dialogueGrace > 0f) dialogueGrace -= dt;

        TrackPlayer(dt);

        if (waitingForDialogue && dialogueGrace <= 0f)
        {
            if (dialogueRunner == null || !dialogueRunner.IsDialogueRunning)
            {
                waitingForDialogue = false;
                OnDialogueFinished();
            }
        }

        desiredMove = Vector3.zero;

        switch (state)
        {
            case SocState.Seated:      TickSeated();        break;
            case SocState.Rising:      TickRising();        break;
            case SocState.Greeting:    FaceThePlayer(dt);   break;
            case SocState.Walking:     TickWalking(dt);     break;
            case SocState.Checkpoint:  TickCheckpoint(dt);  break;
            case SocState.SittingDown: TickSittingDown(dt); break;
            case SocState.Arrived:     FaceThePlayer(dt);   break;
        }

        ApplyMotion(dt);
    }

    // ---------------------------------------------------------------- states

    private void TickSeated()
    {
        if (player == null || waitingForDialogue) return;

        float d = FlatDistance(transform.position, player.position);

        if (!hasCalledOut && !string.IsNullOrEmpty(callOutNode) && d <= callOutRadius)
        {
            hasCalledOut = true;
            RunNode(callOutNode);
            return;
        }

        if (d <= triggerRadius)
        {
            EnterState(SocState.Rising);
            if (useSeatedAnimations)
            {
                SetBool(seatedBoolParam, false);
                SetTrigger(standUpTriggerParam);
            }
        }
    }

    private void TickRising()
    {
        FaceThePlayer(Time.deltaTime);

        float wait = useSeatedAnimations ? standUpDuration : 0f;
        if (stateTimer >= wait)
        {
            EnterState(SocState.Greeting);
            RunNode(joinNode);
        }
    }

    private void TickWalking(float dt)
    {
        if (waitingForDialogue) return;

        if (player == null || destination == null)
        {
            SetBool(walkBoolParam, false);
            return;
        }

        // Arrived?
        if (FlatDistance(player.position, destination.position) <= arriveDistance)
        {
            EnterState(SocState.SittingDown);
            SetBool(walkBoolParam, false);
            if (useSeatedAnimations)
            {
                SetTrigger(sitDownTriggerParam);
                SetBool(seatedBoolParam, true);
            }
            return;
        }

        // Should we stop and pose a question?
        bool playerStopped = playerSpeed < playerStoppedSpeed;
        stoppedTimer = playerStopped ? stoppedTimer + dt : 0f;

        bool fellBehind = FlatDistance(transform.position, player.position) > fallBehindDistance;

        if (cooldownTimer <= 0f && HasCheckpointLeft() && (stoppedTimer >= stoppedDwellTime || fellBehind))
        {
            EnterState(SocState.Checkpoint);
            SetBool(walkBoolParam, false);
            stoppedTimer = 0f;
            return;
        }

        // Walk alongside: hold a slot at the player's side.
        Vector3 slot = SlotBesidePlayer();
        Vector3 toSlot = slot - transform.position;
        toSlot.y = 0f;

        float dist = toSlot.magnitude;
        if (dist > stopDistance)
        {
            // Ease speed by distance so he matches the player's pace instead of jittering.
            float speed = Mathf.Min(followSpeed, followSpeed * (dist / 2f) + 0.4f);
            desiredMove = toSlot.normalized * speed;
            SetBool(walkBoolParam, true);
            FaceDirection(PlayerFlatForward(), dt);
        }
        else
        {
            SetBool(walkBoolParam, false);
            FaceDirection(PlayerFlatForward(), dt);
        }
    }

    private void TickCheckpoint(float dt)
    {
        if (player == null)
        {
            EnterState(SocState.Walking);
            return;
        }

        // Step into the player's line of sight, then face them and ask.
        Vector3 spot = player.position + PlayerFlatForward() * lineOfSightDistance;
        Vector3 toSpot = spot - transform.position;
        toSpot.y = 0f;

        if (!waitingForDialogue && toSpot.magnitude > stopDistance)
        {
            desiredMove = toSpot.normalized * followSpeed;
            SetBool(walkBoolParam, true);
            FaceDirection(toSpot, dt);
            return;
        }

        SetBool(walkBoolParam, false);
        FaceThePlayer(dt);

        if (!waitingForDialogue)
        {
            string node = NextCheckpointNode();
            if (string.IsNullOrEmpty(node))
            {
                // Nothing left to ask - don't strand him standing in the road.
                EnterState(SocState.Walking);
                cooldownTimer = checkpointCooldown;
                return;
            }
            RunNode(node);
        }
    }

    private void TickSittingDown(float dt)
    {
        Transform target = sitSpot != null ? sitSpot : destination;

        if (target != null)
        {
            Vector3 toSeat = target.position - transform.position;
            toSeat.y = 0f;

            if (toSeat.magnitude > stopDistance)
            {
                desiredMove = toSeat.normalized * followSpeed;
                SetBool(walkBoolParam, true);
                FaceDirection(toSeat, dt);
                return;
            }
        }

        SetBool(walkBoolParam, false);
        FaceThePlayer(dt);

        float wait = useSeatedAnimations ? sitDownDuration : 0f;
        if (stateTimer >= wait && !waitingForDialogue)
        {
            EnterState(SocState.Arrived);
            RunNode(arrivalNode);
        }
    }

    // ---------------------------------------------------------------- dialogue

    private void OnDialogueFinished()
    {
        switch (state)
        {
            case SocState.Seated:
                // The call-out finished; stay seated and wait for the player to close in.
                break;

            case SocState.Greeting:
                EnterState(SocState.Walking);
                cooldownTimer = checkpointCooldown;
                break;

            case SocState.Checkpoint:
                EnterState(SocState.Walking);
                cooldownTimer = checkpointCooldown;
                stoppedTimer = 0f;
                break;

            case SocState.Arrived:
                // Plane-tree conversation is done. Yarn sends the player on to Chalmers.
                break;
        }
    }

    private void RunNode(string node)
    {
        if (string.IsNullOrEmpty(node) || dialogueRunner == null) return;

        if (dialogueRunner.IsDialogueRunning)
        {
            // Something else is already talking - most often the DialogueRunner's own
            // autoStart node. Adopt that conversation and wait for it to end rather than
            // returning without arming the wait, which dead-ends the state machine here
            // permanently: he would stand facing the player and never walk.
            waitingForDialogue = true;
            dialogueGrace = 0.3f;
            return;
        }

        dialogueRunner.StartDialogue(node);
        waitingForDialogue = true;
        dialogueGrace = 0.3f;
    }

    private bool HasCheckpointLeft()
    {
        return walkingNodes != null && checkpointIndex < walkingNodes.Length;
    }

    private string NextCheckpointNode()
    {
        if (!HasCheckpointLeft()) return null;
        string node = walkingNodes[checkpointIndex];
        checkpointIndex++;
        return node;
    }

    // ---------------------------------------------------------------- helpers

    private void EnterState(SocState next)
    {
        state = next;
        stateTimer = 0f;
    }

    private void TrackPlayer(float dt)
    {
        if (player == null || dt <= 0f) return;

        Vector3 now = player.position;
        Vector3 delta = now - lastPlayerPos;
        delta.y = 0f;

        playerSpeed = Mathf.Lerp(playerSpeed, delta.magnitude / dt, 0.35f);
        lastPlayerPos = now;
    }

    private Vector3 SlotBesidePlayer()
    {
        Vector3 fwd = PlayerFlatForward();
        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized;
        return player.position + right * sideOffset + fwd * forwardOffset;
    }

    private Vector3 PlayerFlatForward()
    {
        if (player == null) return transform.forward;

        Vector3 f = player.forward;
        f.y = 0f;

        if (f.sqrMagnitude < 0.0001f) return transform.forward;
        return f.normalized;
    }

    private void FaceThePlayer(float dt)
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        FaceDirection(toPlayer, dt);
    }

    private void FaceDirection(Vector3 dir, float dt)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion want = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, want, dt * turnSpeed);
    }

    private void ApplyMotion(float dt)
    {
        // Horizontal first. Route it through the CharacterController when there is one
        // so he doesn't walk through trees.
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(desiredMove * dt);
        }
        else
        {
            transform.position += desiredMove * dt;
        }

        if (!snapToGround)
        {
            return;
        }

        // Deliberately NOT accumulating gravity against CharacterController.isGrounded.
        // An NPC that mis-grounds for even a few frames builds enough downward speed to
        // punch through a terrain collider and is then gone for good. Probing for the
        // ground every frame cannot do that.
        if (TryGetGroundHeight(transform.position, out float groundY))
        {
            Vector3 p = transform.position;
            float targetY = groundY + groundOffset;

            // Big discrepancy (spawn, teleport, a fall already in progress) - snap.
            // Small one - ease, so he doesn't jitter over bumpy terrain.
            p.y = Mathf.Abs(p.y - targetY) > 2f
                ? targetY
                : Mathf.Lerp(p.y, targetY, 1f - Mathf.Exp(-groundStick * dt));

            transform.position = p;
            verticalVelocity = 0f;
            return;
        }

        // Nothing under him at all. Fall, but at a bounded speed.
        verticalVelocity = Mathf.Max(verticalVelocity - gravityStrength * dt, -maxFallSpeed);
        Vector3 q = transform.position;
        q.y += verticalVelocity * dt;
        transform.position = q;
    }

    private bool TryGetGroundHeight(Vector3 from, out float groundY)
    {
        Vector3 origin = from + Vector3.up * groundProbeUp;
        RaycastHit hit;

        if (Physics.Raycast(origin, Vector3.down, out hit, groundProbeUp + groundProbeDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            return true;
        }

        // Nothing underneath. He may be UNDER the surface rather than above it -
        // a terrain collider is a one-sided heightfield, so a ray starting beneath it
        // and pointing down passes straight through and reports nothing. Drop a second
        // probe from well overhead to find the surface he is buried below.
        origin = from + Vector3.up * buriedRecoveryHeight;

        if (Physics.Raycast(origin, Vector3.down, out hit, buriedRecoveryHeight + groundProbeDown, groundMask, QueryTriggerInteraction.Ignore))
        {
            groundY = hit.point.y;
            return true;
        }

        groundY = from.y;
        return false;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void SetBool(string param, bool value)
    {
        if (dialogueAnimator == null || string.IsNullOrEmpty(param)) return;
        dialogueAnimator.SetBool(param, value);
    }

    private void SetTrigger(string param)
    {
        if (dialogueAnimator == null || string.IsNullOrEmpty(param)) return;
        dialogueAnimator.SetTrigger(param);
    }

    private Transform FindPlayerTransform()
    {
        // An explicit reference beats tag archaeology. Tags are easy to put on the
        // wrong object - an empty parent wrapper looks identical in the Hierarchy to
        // the thing that actually moves, and the symptom (NPC treats you as stationary)
        // gives no hint about the cause.
        if (playerOverride != null)
        {
            return playerOverride;
        }

        GameObject tagged = GameObject.FindWithTag("Player");
        if (tagged != null) return tagged.transform;

        GameObject xr = GameObject.Find("XR Origin");
        if (xr != null) return xr.transform;

        GameObject cam = GameObject.FindWithTag("MainCamera");
        if (cam != null)
        {
            Debug.LogWarning("[DialogueTrigger] Falling back to the camera as the player. Tag the player root 'Player' for steadier following.", this);
            return cam.transform;
        }

        Debug.LogWarning("[DialogueTrigger] Could not find a player transform.", this);
        return null;
    }

    private void OnGUI()
    {
        if (!showDebug || !Application.isPlaying) return;

        string playerName = player != null ? player.name : "<none>";
        float toDest = (player != null && destination != null)
            ? FlatDistance(player.position, destination.position) : -1f;
        float toPlayer = player != null ? FlatDistance(transform.position, player.position) : -1f;
        bool talking = dialogueRunner != null && dialogueRunner.IsDialogueRunning;

        string text =
            $"state        {state}\n" +
            $"player       {playerName}\n" +
            $"playerSpeed  {playerSpeed:F2}   (stopped below {playerStoppedSpeed})\n" +
            $"stoppedFor   {stoppedTimer:F2}s  (fires at {stoppedDwellTime})\n" +
            $"cooldown     {Mathf.Max(0f, cooldownTimer):F1}s\n" +
            $"waitingDlg   {waitingForDialogue}   runnerBusy {talking}\n" +
            $"desiredMove  {desiredMove.magnitude:F2}\n" +
            $"dist->player {toPlayer:F1}   dist(player->dest) {toDest:F1}\n" +
            $"checkpoint   {checkpointIndex}/{(walkingNodes != null ? walkingNodes.Length : 0)}";

        GUI.color = Color.black;
        GUI.Box(new Rect(8, 8, 380, 156), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(16, 14, 372, 148), text);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerRadius);

        if (!string.IsNullOrEmpty(callOutNode))
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.35f);
            Gizmos.DrawWireSphere(transform.position, callOutRadius);
        }

        if (destination != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(destination.position, arriveDistance);
            Gizmos.DrawLine(transform.position, destination.position);
        }

        if (sitSpot != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(sitSpot.position, Vector3.one * 0.4f);
        }
    }
}
