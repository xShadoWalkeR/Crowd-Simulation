﻿using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using LibGameAI.DecisionTrees;

public class NPCBehaviour : MonoBehaviour
{
    // Arrays with the location of all areas of interest
    [SerializeField] private TablesManager[] eatingAreas;
    [SerializeField] private Transform[] restingAreas;
    [SerializeField] private Transform[] stages;

    // How fast the agent moves
    [SerializeField] private float speed = 10f;
    
    // Boredom behaviour variables
    private readonly float maximumExcitementLevel = 100f;   // How much excitement the agent can have
    private float excitementLevel;  // The current level of excitement
    private float excitementStep;   // How much the excitement decreases each second

    // Tiredness behaviour variables
    private readonly float maximumStaminaLevel = 100f;  // How much stamina the agent can have
    private float staminaLevel; // The current level of stamina 
    private float staminaStep;  // How much the stamina decreases each second
    private bool isResting; // If the agent is currently resting

    // Hungriness behaviour variables
    private readonly float maximumFullnessLevel = 100f; // How full the agent can be
    private float fullnessLevel;    // The current level of fullness
    private float fullnessSpet; // How much the fullness decreases each second
    private bool isEating;  // If the agent is currently eating

    // The multiplier for increasing the values when resting or eating
    private float multiplier;

    // The current stage the agent is watching
    private Transform currentStage;

    // If the agent has reach the stage he wants to go to
    public bool HasReachedStage { get; private set; }

    // How long will the agent try to push forwards
    private WaitForSeconds pushFor;

    // The current resting area the agent is going to
    private Transform currentRestingArea;

    // The curret eating area the agent is going to
    private Transform currentEatingArea;

    // The table chosen by the agent when he goes to eat
    private Table chosenTable;

    // Reference to our Nav Mesh Agent
    private NavMeshAgent agent;


    // Action and Decision Nodes \\

    // Decision node if the agent is hungry
    private IDecisionTreeNode isAgentHungry;

    // Action node in case the agent is hungry
    private IDecisionTreeNode agentIsHungry;

    // Decision node if the agent is tired
    private IDecisionTreeNode isAgentTired;

    // Action node in case the agent is tired
    private IDecisionTreeNode agentIsTired;

    // Decision node if the agent is excited
    private IDecisionTreeNode isAgentExcited;

    // Action node in case the agent is not excited
    private IDecisionTreeNode agentNotExcited;

    /// <summary>
    /// Awake is called when the script is loaded
    /// </summary>
    private void Awake()
    {
        // Create a new action node for the agent eating behaviour
        agentIsHungry = new ActionNode(AgentGoEat);

        // Create a new action node for the agent resting behaviour
        agentIsTired = new ActionNode(AgentGoRest);

        // Create a new action node for the agent change stage behaviour
        agentNotExcited = new ActionNode(AgentChangeStage);

        // Create a new decision node to know if the agent is excited
        isAgentExcited = new DecisionNode(IsAgentExcited, new ActionNode(() => { }), agentNotExcited);

        // Create a new decision node to know if the agent is tired
        isAgentTired = new DecisionNode(IsAgentTired, agentIsTired, isAgentExcited);

        // Create a new decision node to know if the agent is hungry
        isAgentHungry = new DecisionNode(IsAgentHungry, agentIsHungry, isAgentTired);
    }

    /// <summary>
    /// Start is called before the first frame update
    /// </summary>
    void Start()
    {
        // Initialize `agent` by getting the NavMeshAgent component
        agent = this.GetComponent<NavMeshAgent>();

        // After colliding with someone the agent will keep pushing forward for 2 seconds
        pushFor = new WaitForSeconds(0.3f);

        // Initialize the chosen table as null
        chosenTable = null;

        // Initialize the `HasReachedStage` as false
        HasReachedStage = false;

        // Define the initial values for the excitement level
        excitementLevel = 0f;
        // Define the step amount for the excitement level
        excitementStep = Random.Range(1f, 3f);

        // Define the initial values for the stamina level
        staminaLevel = Random.Range(50f, 100f);
        // Define the step amount for the stamina level
        staminaStep = 0; // Random.Range(1f, 3f);

        // Define the initial values for the fullness level
        fullnessLevel = Random.Range(60f, 100f);
        // Define the step amount for the fullness level
        fullnessSpet = Random.Range(0.5f, 3f);

        // Define the multiplier for resting and eating
        multiplier = Random.Range(4f, 6f);
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void FixedUpdate()
    {
        // Call the root node of the decision tree
        (isAgentHungry.MakeDecision() as ActionNode).Execute();

        // Update the level of excitement the agent has
        UpdateExcitementLevel();
        
        // If the agent is not resting we update the stamina level
        if (!isResting)
            UpdateStaminaLevel();

        // If the agent is not eating we update the fullness level
        if (!isEating)
            UpdateFullness();
    }

    /// <summary>
    /// Checks if the agent is currently hungry
    /// </summary>
    /// <returns>True if the agent is hungry or eating, false otherwise</returns>
    private bool IsAgentHungry() => fullnessLevel == 0f || isEating;

    /// <summary>
    /// Makes the agent go eat till he's full again
    /// </summary>
    private void AgentGoEat() {
        // If the agent hasn't selected an eating area yet
        if (currentEatingArea == null)
        {
            // The agent is stoped while he chooses a table
            agent.isStopped = true;

            // Select a random eating area from the two available
            TablesManager eatingArea = 
                Random.Range(1f, 100f) > 50 ? eatingAreas[0] : eatingAreas[1];

            // Set the table to the first in the list
            chosenTable = eatingArea.Tables[0];

            // Logic to choose the table with less people on it
            foreach (Table t in eatingArea.Tables)
            {
                // If the number of people sitting at the current table is greater than at table `t`
                if (chosenTable.TakenSeats.Count > t.TakenSeats.Count)
                {
                    // Set the chosen table to be the table `t`
                    chosenTable = t;
                }
            }
            
            if (chosenTable.AvailableSeats.Count == 0)
            {
                // Return to choose another table
                return;
            }

            // Set the current eating area to be the seat on the table we want
            currentEatingArea = chosenTable.AvailableSeats[0];

            // Remove the chosen seat from the available list
            chosenTable.AvailableSeats.Remove(currentEatingArea);

            // Add it to the taken list
            chosenTable.TakenSeats.Add(currentEatingArea);

            // Move the agent to the seat
            agent.SetDestination(currentEatingArea.position);

            // We're not close to the stage
            HasReachedStage = false;

            // The agent will move after choosing a table
            agent.isStopped = false;

        } // If the agent has't reached the objective yet
        else if (Vector3.Distance(transform.position, currentEatingArea.position) > 2f)
        {
            // Move the agent to the seat
            agent.SetDestination(currentEatingArea.position);

        } // If we've reached the destination and our `fullnessLevel` is less than the maximum
        else if (Vector3.Distance(transform.position, currentEatingArea.position) < 2f && 
            fullnessLevel < maximumFullnessLevel)
        {
            // We're currently eating
            isEating = true;

            // Increase the `fullnessLevel` each second based on the `(fullnessStep * multiplier)`
            fullnessLevel = Mathf.Min(
                fullnessLevel + ((fullnessSpet * multiplier) * Time.deltaTime),
                maximumFullnessLevel);

        }   // If the `fullnessLevel` is at maximum
        else if (fullnessLevel >= maximumFullnessLevel)
        {
            // We finished eating so the seat will now be available
            // Add it to the available seats list
            chosenTable.AvailableSeats.Add(currentEatingArea);
            // Remove it from the taken seats list
            chosenTable.TakenSeats.Remove(currentEatingArea);

            // Set the current eating area to null
            currentEatingArea = null;

            // We're no longer eating
            isEating = false;

            // Start moving the agent to the stage
            excitementLevel = 0;
        }
    }

    /// <summary>
    /// Checks if the agent is currently tired
    /// </summary>
    /// <returns>True if the agent is tired or resting, false otherwise</returns>
    private bool IsAgentTired() => staminaLevel == 0f || isResting;

    /// <summary>
    /// Makes the agent go rest till he's got stamina again
    /// </summary>
    private void AgentGoRest()
    {
        // If we don't currently have a resting area assigned
        if (currentRestingArea == null)
        {
            // Choses randomly between the two available resting areas
            currentRestingArea = Random.Range(1, 100) > 50 ? restingAreas[0] : restingAreas[1];

            // When the agent starts going to the stage he want's he's no longer stopped
            // agent.isStopped = false;

            // Sets the agent destination to be that resting area
            agent.destination = currentRestingArea.position;
        }

        // If we've reached the destination and our `staminaLevel` is less than the maximum
        if (Vector3.Distance(transform.position, currentRestingArea.position) < 1 && 
            staminaLevel < maximumStaminaLevel)
        {
            // We're currently resting
            isResting = true;

            // Increase the `staminaLevel` each second based on the `(staminaStep * multiplier)`
            staminaLevel = Mathf.Min(
                staminaLevel + ((staminaStep * multiplier) * Time.deltaTime),
                maximumStaminaLevel);

        }   // If the `staminaLevel` is at maximum
        else if (staminaLevel >= maximumStaminaLevel)
        {
            // Set the `currentRestingArea` to null
            currentRestingArea = null;

            // The agent is no longer resting
            isResting = false;

            // The agent has not yet reached the stage
            HasReachedStage = false;

            // Set the excitement level to 0
            excitementLevel = 0;
        }
    }

    /// <summary>
    /// Checks if the agent is currently excited
    /// </summary>
    /// <returns>True if the agent is excited, false otherwise</returns>
    private bool IsAgentExcited() => excitementLevel > 0f;

    /// <summary>
    /// Makes the agent go to another stage
    /// </summary>
    private void AgentChangeStage()
    {
        // Get a new random float between 1 and 100
        float stageSelect = Random.Range(1, 100);

        // If the current stage is null
        if (currentStage == null)
        {
            // Select one of the three stages, with the bigger ones having a higher chance
            currentStage = stageSelect > 60 ? stages[0] : stageSelect > 28 ? stages[1] : stages[2];

            // Move the agent to the current stage
            agent.SetDestination(currentStage.position);

        } // If the agent has't reached the objective yet
        else if (Vector3.Distance(transform.position, currentStage.position) > 2.5f)
        {
            // Move the agent to the current stage
            agent.SetDestination(currentStage.position);

        } // If the distance from the agent to the target position is less than 3
        else if (Vector3.Distance(transform.position, currentStage.position) < 2.5f)
        {
            // The agent has reached the stage
            HasReachedStage = true;

            // Increasse the excitement level when we switch stage
            excitementLevel = Random.Range(90f, 100f);

            // Set the current stage to null
            currentStage = null;
        }
        else if (HasReachedStage)
        {
            // Increasse the excitement level when we switch stage
            excitementLevel = Random.Range(90f, 100f);

            // Set the current stage to null
            currentStage = null;
        }
    }

    /// <summary>
    /// Decrease the current excitement level by `excitementStep` each second
    /// </summary>
    private void UpdateExcitementLevel()
    {
        // Clamp the excitement level between 0 and `maximumExcitementLevel`
        excitementLevel = Mathf.Clamp(
            excitementLevel - (excitementStep * Time.deltaTime),
            0f, maximumExcitementLevel);

        if (HasReachedStage)
        {
            // If the agent has reached the stage he will stop moving
            agent.isStopped = true;
        } else
        {
            // If the agent has not reached the stage he will move
            agent.isStopped = false;
        }
    }

    /// <summary>
    /// Decrease the current stamina level by `staminaStep` each second
    /// </summary>
    private void UpdateStaminaLevel()
    {
        // Clamp the stamina level between 0 and `maximumStaminaLevel`
        staminaLevel = Mathf.Clamp(
            staminaLevel - (staminaStep * Time.deltaTime),
            0f, maximumStaminaLevel);
    }

    /// <summary>
    /// Decrease the current fullness level by `fullnessStep` each second
    /// </summary>
    private void UpdateFullness()
    {
        // Clamp the fullness level between 0 and `maximumFullnessLevel`
        fullnessLevel = Mathf.Clamp(
            fullnessLevel - (fullnessSpet * Time.deltaTime),
            0f, maximumFullnessLevel);
    }

    /// <summary>
    /// Called when the agent collides with another agent
    /// </summary>
    /// <param name="other">The collider of the other agent</param>
    private void OnCollisionEnter(Collision other)
    {
        // If the agent collided with another npc
        if (other.transform.CompareTag("NPC"))
        {
            // And that npc is on a stage
            if (other.transform.GetComponent<NPCBehaviour>().HasReachedStage)
            {
                // If the agent is not yet on a stage
                if (!HasReachedStage)
                {
                    // Start a corroutine
                    StartCoroutine(StopPushing());
                }
            }
        }
    }

    /// <summary>
    /// Makes the agent stop pushing forawrd after some time
    /// </summary>
    /// <returns>Waits for some seconds</returns>
    private IEnumerator StopPushing()
    {
        // Waits for some seconds
        yield return pushFor;

        // If the agent is currently moving towards a stage
        if (currentStage != null && agent.destination == currentStage.position)
        {
            // The agent has reached the stage
            HasReachedStage = true;
        }
    }
}
