using System.Collections;
using System.Collections.Generic;
using UnityEngine; // Unity core functionality
using Unity.MLAgents; // ML-Agents main namespace
using Unity.MLAgents.Actuators; // For handling actions
using Unity.MLAgents.Sensors; // For handling observations
using Unity.MLAgents.Integrations.Match3;
using Unity.Sentis;
using Random = UnityEngine.Random;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
using UnityEngine.TextCore.Text;
using JetBrains.Annotations;
using System;
using Unity.Burst.CompilerServices;

public enum Team
{
    Hider = 0,
    Seeker = 1
}

public class AgentController : Agent
{

    // Note that that the detectable tags are different for the blue and purple teams. The order is
    // * ball 
    // * own goal
    // * opposing goal
    // * wall
    // * own teammate
    // * opposing player

    public MySceneBehavior mySceneBehavior;
    public GridManager gridManager;


    // Agent Variables
    private float moveSpeed;
    private float rotateSpeed;
    //private Rigidbody rb;
    //[SerializeField] int teamID;
    public Team team;
    [SerializeField] public GameObject[] teamMates;

    public bool isGrounded;  // You need to determine if the character is grounded

    public Material AgentColor;

    public int width_length;
    public int height_length;

    public float currentYPosition;
    // gravity
    private bool groundedPlayer;
    private float gravityValue = -9.81f;
    private Vector3 playerVelocity;

    public bool iCanSeeAHider;

    private Rigidbody rb;
    BehaviorParameters m_BehaviorParameters;
    public LayerMask obstacleMask; 

    // eye sight for seeker
    [SerializeField] public Transform[] hiderTransforms;
    private float viewAngle = 90f; // Field of view angle
    private float sightDistance = 5f;


    // Object 
    [SerializeField] private GameObject[] interactableBlocks; // An array to hold references to the interactable objects.
    private Transform currentlyHoldingBlock = null;
    private bool lockingToggle = false;
    private Vector3 initialOffset;
    private Quaternion initialRotationDifference;
    Block blockScript;
    private Transform originalParent;

    public Collider characterCollider;
    //public Transform agent;

    public override void Initialize()
    {
        //rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
        //envMaterial = env.GetComponent<Renderer>().material;
        rb = GetComponent<Rigidbody>(); // Get the Rigidbody component
                                        //rb.centerOfMass = rb.transform.InverseTransformPoint(rb.worldCenterOfMass);
        characterCollider = GetComponent<Collider>();

        // ensure team assignment
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (m_BehaviorParameters.TeamId == (int)Team.Seeker)
        {
            team = Team.Seeker;
        }
        else
        {
            team = Team.Hider;
        }

    }
    public void placeMe(float x, float y, float Rotation)
    {
        transform.localPosition = new Vector3(x, 0f, y);
        //Debug.Log(x+ y+ Rotation);
        transform.localRotation = Quaternion.Euler(0f, Rotation, 0f);
    }
    public override void OnEpisodeBegin()
    {
        if (currentlyHoldingBlock != null)
        {
            currentlyHoldingBlock.transform.SetParent(originalParent);
            blockScript.AddRigidbody();
            currentlyHoldingBlock = null;
        }
        iCanSeeAHider = false;
        //rb.centerOfMass = new Vector3(0, 0.75f, 0);

        // Reset the agent's position randomly within a range
        //transform.localPosition = new Vector3(Random.Range(-8f, 8f), 0f, Random.Range(-8f, 8f)); 
        // make sure no overlap
        //      - make a grid assigning for a battleship like placement to prevent overlap, giving number of grid points that a block would take up 

        currentlyHoldingBlock = null;
        lockingToggle = false;

        int randomYMultiplier = Random.Range(0, 4); // 4 is exclusive, so it will never actually be picked
        int yRotation = randomYMultiplier * 90; // Random Y rotation at 90 degree increments
        (float x, float y, int rotation) = gridManager.PlaceItem(width_length, height_length, yRotation);
        //Debug.Log("Agent placed");


        placeMe(x, y, rotation);




        //Debug.Log("Environment Reset");
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(transform.localRotation);

        // team mates position
        foreach (var teamMate in teamMates)
        {
            sensor.AddObservation(teamMate.transform.localPosition);
            sensor.AddObservation(teamMate.transform.localRotation);

        }

        // blocks
        foreach (var blockObject in interactableBlocks)
        {
            Block block = blockObject.GetComponent<Block>();
            if (block != null)
            {
                sensor.AddObservation(block.transform.localPosition);
                sensor.AddObservation(block.transform.localRotation);
                sensor.AddObservation(block.IsHeldOrLocked);
            }
            

        }



    }
    public void Update()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (mySceneBehavior.seekerStarted == true && m_BehaviorParameters.TeamId == (int)Team.Seeker)
        { // if agent is a seeker, check line of sight and update rewards accordingly
            //CheckLineOfSight();
        }
    }
    public override void OnActionReceived(ActionBuffers actions)
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if ((mySceneBehavior.seekerStarted == true && m_BehaviorParameters.TeamId == (int)Team.Seeker) || (m_BehaviorParameters.TeamId == (int)Team.Hider))
        {
            // Get the continuous actions for rotation and forward movement
            float moveRotate = actions.ContinuousActions[0]; // Rotation around the Y-axis
            float moveForward = actions.ContinuousActions[1]; ; // Move forward in the facing direction
            float toggleHoldingBlock = actions.DiscreteActions[0];
            float toggleLockingBlock = actions.DiscreteActions[1];

            //Debug.Log(" Holding: " + toggleHoldingBlock + " Locking: " + toggleLockingBlock);   
            //CheckGroundStatus();
            if (toggleLockingBlock == 0f && toggleHoldingBlock == 1f && currentlyHoldingBlock == null)
            { // pick up new object
                CheckForBlockToPickUp();
                if (currentlyHoldingBlock != null)
                {
                    initialOffset = currentlyHoldingBlock.position - transform.position;
                    originalParent = currentlyHoldingBlock.transform.parent;

                    //blockScript.kinematicTrue();
                    initialRotationDifference = Quaternion.Inverse(transform.rotation) * currentlyHoldingBlock.rotation;
                    rb.centerOfMass = new Vector3(0, 0.75f, 0);


                    currentlyHoldingBlock.transform.SetParent(transform);
                    blockScript.RemoveRigidbody();


                }
            }
            if ((toggleLockingBlock == 0f && toggleHoldingBlock == 0f && currentlyHoldingBlock != null) )//|| (!isGrounded && currentlyHoldingBlock != null))
            { // drop current object
                currentlyHoldingBlock.GetComponent<Block>().blockRenderer.material = mySceneBehavior.unlockedColor;
                currentlyHoldingBlock.GetComponent<Block>().currentlyHeldByPlayer = null;
                blockScript.AddRigidbody();
  




                currentlyHoldingBlock.transform.SetParent(originalParent);
                currentlyHoldingBlock = null;

                // Set the parent back to the original parent
                

                blockScript = null;

                initialOffset = Vector3.zero;
                rb.centerOfMass = new Vector3(0, 0.75f, 0);


            }
            if (toggleLockingBlock == 1f && toggleHoldingBlock == 0f && currentlyHoldingBlock == null && lockingToggle == false)
            { // lock object
                lockingToggle = true;
                CheckForBlockToLock();
            }
            if (toggleLockingBlock == 0f)
            { // Only lock block once for every key press
                lockingToggle = false;
            }


            
            if (currentlyHoldingBlock != null)
            {

                moveSpeed = 75f;
                rotateSpeed = 5000f;


            } else
            {
                moveSpeed = 90f;
                rotateSpeed = 300f;
            }





            CheckGroundStatus();

            if (isGrounded || currentlyHoldingBlock != null)
            {
                rb.drag = 5f; // Higher drag on the ground
                //Debug.Log("Drag increased");
                rb.angularDrag = 2f;


                float turn = moveRotate * rotateSpeed * Time.fixedDeltaTime;
                rb.AddTorque(Vector3.up * turn, ForceMode.Force);

                Vector3 force = transform.forward * moveForward * moveSpeed;
                rb.AddForce(force, ForceMode.Force);

                //Debug.Log("is grounded");
            }
            else if (!isGrounded)//&& currentlyHoldingBlock == null)
            {
                rb.drag = 0.01f; // Lower drag in the air
                //Debug.Log("Drag decreased");
                rb.angularDrag = 0.1f;
                Vector3 forceY = -(transform.up * moveSpeed);
                rb.AddForce(forceY, ForceMode.Force);
                Vector3 force = transform.forward * 20;
                rb.AddForce(force, ForceMode.Force);
            }

            Vector3 rotation = transform.eulerAngles;
            rotation.x = 0;
            rotation.z = 0;
            transform.eulerAngles = rotation;

            // Apply the calculated velocity to the Rigidbody
            //rb.velocity = new Vector3(rb.velocity.x, playerVelocity.y, rb.velocity.z);


            if (mySceneBehavior.seekerStarted == true && m_BehaviorParameters.TeamId == (int)Team.Seeker)
            { // if agent is a seeker, check line of sight and update rewards accordingly
                CheckLineOfSight();
            }
            //HeadColour();
        }
    }




    void CheckForBlockToPickUp()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, mySceneBehavior.maxObjectInteractDistance);


        float closestDistance = Mathf.Infinity;
        Transform closestBlock = null;
        blockScript = null;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Block"))
            {
                Block tempblockScript = hitCollider.GetComponent<Block>();
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (tempblockScript != null && distance < closestDistance)
                {
                    closestDistance = distance;

                    closestBlock = hitCollider.transform;
                    blockScript = tempblockScript;
                }
            }
        }


        if (closestBlock != null && blockScript.currentlyHeldByPlayer == null && blockScript.currentlyLockedByTeam == null)
        {  // pick up block if no one currently has it, and the block is unlocked 
            closestBlock.GetComponent<Block>().currentlyHeldByPlayer = this.GetInstanceID(); // Lock this block to the current team
            currentlyHoldingBlock = closestBlock;
            blockScript.TagTheBlock();
            if (team == Team.Hider)
            {
                closestBlock.GetComponent<Block>().blockRenderer.material = mySceneBehavior.hiderPickedUpColor;
            }
            else
            {
                closestBlock.GetComponent<Block>().blockRenderer.material = mySceneBehavior.seekerPickedUpColor;
            }
        } // else block is not picked up

    }

    void CheckForBlockToLock()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, mySceneBehavior.maxObjectInteractDistance);

        float closestDistance = Mathf.Infinity;
        Transform closestBlock = null;
        Block blockScript = null;

        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Block"))
            {
                Block tempblockScript = hitCollider.GetComponent<Block>();
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                if (tempblockScript != null  && distance < closestDistance )
                {
                    closestDistance = distance;
                    closestBlock = hitCollider.transform;
                    blockScript = tempblockScript;
                }
            }
        }

        if (closestBlock != null && blockScript.currentlyHeldByPlayer == null && blockScript.currentlyLockedByTeam == null)
        {
            // Lock this block to the current team
            closestBlock.GetComponent<Block>().currentlyLockedByTeam = team;
            //closestBlock.GetComponent<Rigidbody>().isKinematic = true;
            //Debug.Log("Closest block locked!");
            blockScript.RemoveRigidbody();

            if (team == Team.Hider)
            {
                closestBlock.GetComponent<Block>().blockRenderer.material = mySceneBehavior.hiderLockedColor;
            } else
            {
                closestBlock.GetComponent<Block>().blockRenderer.material = mySceneBehavior.seekerLockedColor;
            }
            

        }
        else if (closestBlock != null && blockScript.currentlyHeldByPlayer == null && blockScript.currentlyLockedByTeam == team) 
        {
            // unlock block if no one is holding and was locked by a fellow team member
            //Debug.Log("Closest block unlocked!");
            closestBlock.GetComponent<Block>().currentlyLockedByTeam = null;
            //closestBlock.GetComponent<Rigidbody>().isKinematic = false;
            closestBlock.GetComponent<Block>().blockRenderer.material = mySceneBehavior.unlockedColor;
            blockScript.AddRigidbody();
        }
        closestBlock = null;
        blockScript = null;
    }

    void CheckGroundStatus()
    {
        // Settings
        RaycastHit hit;
        float distanceToGround = 0.3f; // Adjust based on character's height from ground
        Vector3 start = transform.position + (Vector3.up * 0.1f); // Start the ray a little above the pivot

        // Visualize the Raycast (make sure to turn on Gizmos in your Game view)
        Debug.DrawRay(start, -Vector3.up * distanceToGround, Color.red);

        // Layer Mask (replace "Ground" with your actual layer name for the ground)
        int layerMask = LayerMask.GetMask("Wall");

        // Perform Raycast
        if (Physics.Raycast(start, -Vector3.up, out hit, distanceToGround, layerMask))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
    private void CheckLineOfSight()
    {
        int hiderSeen = 0;
        float episodeRewardmultiplier = mySceneBehavior.m_ResetTimer / mySceneBehavior.EpisodeSteps;
        foreach (Transform hiderTransform in hiderTransforms)
        {
            hiderSeen += TryToSpotHider(hiderTransform);
        }
        if (hiderSeen == 0)
        { // can be seen
            mySceneBehavior.HiderGroup.AddGroupReward(5f * episodeRewardmultiplier * (mySceneBehavior.totalNumberOfHiders ^ 2));
            mySceneBehavior.SeekerGroup.AddGroupReward(-5f * episodeRewardmultiplier * (mySceneBehavior.totalNumberOfHiders ^ 2));
            mySceneBehavior.HiderRewards += 5f * episodeRewardmultiplier * (mySceneBehavior.totalNumberOfHiders ^ 2);
            mySceneBehavior.SeekerRewards -= 5f * episodeRewardmultiplier * (mySceneBehavior.totalNumberOfHiders ^ 2);
        }
        else if (hiderSeen > 1)
        { // cannot be seen
            mySceneBehavior.HiderGroup.AddGroupReward(-5f * episodeRewardmultiplier * (hiderSeen ^ 2));
            mySceneBehavior.SeekerGroup.AddGroupReward(5f * episodeRewardmultiplier * (hiderSeen ^ 2));
            mySceneBehavior.HiderRewards -= 5f * episodeRewardmultiplier * (hiderSeen ^ 2);
            mySceneBehavior.SeekerRewards += 5f * episodeRewardmultiplier * (hiderSeen ^ 2);
        }
    }

    int TryToSpotHider(Transform hider)
    {
        AgentController hiderAgent = hider.GetComponent<AgentController>();
        Vector3 directionToHider = (hider.position - transform.position).normalized;
        float distanceToHider = Vector3.Distance(transform.position, hider.position);
        iCanSeeAHider = false; 
        float episodeRewardmultiplier = mySceneBehavior.m_ResetTimer / mySceneBehavior.EpisodeSteps;
        if (distanceToHider < sightDistance)
        {
            float angleToHider = Vector3.Angle(transform.forward, directionToHider);

            if (angleToHider < viewAngle / 2f)
            {
                if (!Physics.Raycast(transform.position + Vector3.up * 1.0f, directionToHider, distanceToHider, obstacleMask))
                {
                    
                    Debug.DrawLine(transform.position+ Vector3.up * 1.0f, transform.position + directionToHider * distanceToHider + Vector3.up * 1.0f, Color.red);
                    iCanSeeAHider = true;

                }
            }
        }
        if (iCanSeeAHider)
        {
            //Debug.Log("true");
            // this hider can be seen
            hiderAgent.AddReward(-7f * episodeRewardmultiplier);
            AddReward(7f * episodeRewardmultiplier);
            mySceneBehavior.HiderRewards -= 7f * episodeRewardmultiplier;
            mySceneBehavior.SeekerRewards += 7f * episodeRewardmultiplier;
            return 1;
        } else
        {
            //Debug.Log("false");
            // this hider cannot be seen
            hiderAgent.AddReward(4f * episodeRewardmultiplier);
            //AddReward(-7f * episodeRewardmultiplier);
            mySceneBehavior.HiderRewards += 4f * episodeRewardmultiplier;
            //mySceneBehavior.SeekerRewards -= 7f * episodeRewardmultiplier;
            return 0;
        }
        
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Set continuous actions based on keyboard input
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetAxis("Horizontal"); // Rotation
        continuousActions[1] = Input.GetAxis("Vertical"); // Forward/backward movement
        //continuousActions[2] = Input.GetKey(KeyCode.C) ? 1.0f : 0.0f; // Hold down the 'C' key to hold onto an object
        //continuousActions[3] = Input.GetKey(KeyCode.X) ? 1.0f : 0.0f; // Hold down the 'C' key to hold onto an object

        actionsOut.DiscreteActions.Array[0] = Input.GetKey(KeyCode.C) ? 1 : 0;
        actionsOut.DiscreteActions.Array[1] = Input.GetKey(KeyCode.X) ? 1 : 0;

    }



}
