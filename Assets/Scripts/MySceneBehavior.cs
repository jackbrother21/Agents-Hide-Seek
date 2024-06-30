using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class MySceneBehavior : MonoBehaviour
{
    [System.Serializable]

    public class PlayerInfo
    {
        public AgentController Agent;
        [HideInInspector]
        public Vector3 StartingPos;
        [HideInInspector]
        public Quaternion StartingRot;
        [HideInInspector]
        public Rigidbody Rb;
    }
    public GridManager gridManager;
    public GameObject[] interactableBlocks; // Array of all interactable blocks
    public bool hiderCanBeSeen;

    // game wide variables
    [HideInInspector] public int seekerStartDelaySteps;
    [HideInInspector] public float maxObjectInteractDistance;
    [HideInInspector] public bool seekerStarted;

    [HideInInspector] public float HiderRewards;
    [HideInInspector] public float SeekerRewards;

    public Material hiderLockedColor;
    public Material hiderPickedUpColor;
    public Material seekerLockedColor;
    public Material seekerPickedUpColor;
    public Material unlockedColor;

    public Material SeekersWin;
    public Material HidersWin;
    public Material Tie;
    public GameObject plane;


    //List of Agents On Platform
    public List<PlayerInfo> AgentsList = new List<PlayerInfo>();

    public SimpleMultiAgentGroup HiderGroup;
    public SimpleMultiAgentGroup SeekerGroup;

    [HideInInspector] public float EpisodeSteps;
    public float m_ResetTimer;

    [HideInInspector] public int totalNumberOfHiders;

    void Start() // initialize
    {
        // Initial setup if needed
        Application.runInBackground = true;
        Academy.Instance.OnEnvironmentReset += EnvironmentReset;

        HiderGroup = new SimpleMultiAgentGroup();
        SeekerGroup = new SimpleMultiAgentGroup();

        maxObjectInteractDistance = 1f;
        //seekerStartDelaySteps = 1000;
        EpisodeSteps = 2000f;

        // initialize the objects
        foreach (GameObject interactableBlock in interactableBlocks)
        {
            Block blockScript = interactableBlock.GetComponent<Block>(); // 
            if (blockScript != null)
            {
                blockScript.BlockInitialize();
            }
        }

        foreach (var item in AgentsList)
        {
            item.StartingPos = item.Agent.transform.position;
            item.StartingRot = item.Agent.transform.rotation;
            item.Rb = item.Agent.GetComponent<Rigidbody>();
            if (item.Agent.team == Team.Hider)
            {
                HiderGroup.RegisterAgent(item.Agent);
                totalNumberOfHiders += 1;
            }
            else if (item.Agent.team == Team.Seeker)
            {
                SeekerGroup.RegisterAgent(item.Agent);
            } else
            {
                Debug.Log("Team assignment error");
            }
        }
        EnvironmentReset();
    }




    // Call this method at the beginning of each episode
    void EnvironmentReset()
    {
        seekerStarted = false;
        m_ResetTimer = 0f;
        HiderRewards = 0f;
        SeekerRewards = 0f;

        hiderCanBeSeen = false;
        int seekerRandomStarter = Random.Range(200, 1500);
        seekerStartDelaySteps = seekerRandomStarter;//seekerRandomStarter;

        gridManager.ResetGrid();

        foreach (GameObject block in interactableBlocks)
        {
            Block blockScript = block.GetComponent<Block>();
            if (blockScript != null)
            {
                blockScript.BlockOnEpisodeBegin();
            }
        }
    }

    void FixedUpdate()
    {
        m_ResetTimer += 1f;
        if (m_ResetTimer >= EpisodeSteps && EpisodeSteps > 0)
        {
            Renderer planeRenderer = plane.GetComponent<Renderer>();
            if (HiderRewards > SeekerRewards)
            {
                Debug.Log("Hiders win");
                planeRenderer.material = HidersWin;
            } else if (HiderRewards < SeekerRewards)
            {
                Debug.Log("Seekers win");
                planeRenderer.material = SeekersWin;
            }
            else
            {
                Debug.Log("Tie!");
                planeRenderer.material = Tie;


            }


            HiderGroup.GroupEpisodeInterrupted();
            SeekerGroup.GroupEpisodeInterrupted();
            HiderGroup.EndGroupEpisode();
            SeekerGroup.EndGroupEpisode();
            EnvironmentReset();
            //Debug.Log("Environment reset");
            

            // need to set floor colour based on who scored the most rewards for the last episode

        }
        if (m_ResetTimer >= seekerStartDelaySteps)
        {
            seekerStarted = true;
            //Debug.Log("Seeker started");
        }

        // Existential punishment 
        SeekerGroup.AddGroupReward(-0.1f);
        HiderGroup.AddGroupReward(-0.1f);
    }




}
