﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplosionBehaviour : MonoBehaviour {

    // The Explosions speeds
    private float explosionSpeed;
    private float fireSpeed;

    // All Explosions radiuses
    private float killRadius;  // The radius at which NPC's get killed
    private float stunRadius;  // The radius at which NPC's get stunned
    private float panicRadius; // The radius at which NPC's start panicking
    private float maxRadius;   // The radius at which this explosion gets destroyed

    // The scale to be applied
    private Vector3 desiredScale;

    // The current explosion radius
    private float currentRadius;

    // The script that spawned this explosion
    private ExplosionManager explosionManager;

    /// <summary>
    /// Awake is called before the game starts
    /// </summary>
    private void Awake() {

        // Reposition the explosion prefab accordingly
        transform.position = new Vector3(transform.position.x,
                                         transform.localScale.y, 
                                         transform.position.z);
    }

    /// <summary>
    /// Update is called once per frame
    /// </summary>
    void Update() {

        // Spread the fire / Change scale
        SpreadFire();
    }

    /// <summary>
    /// Assign Explosion Manager variables to an Explosion
    /// </summary>
    /// <param name="explosionSpeed">The speed of the explosion</param>
    /// <param name="fireSpeed">The speed of the fire spread</param>
    /// <param name="killRadius">The radius to kill NPCs</param>
    /// <param name="stunRadius">The radius to stun NPCs</param>
    /// <param name="panicRadius">The radius for the NPCs to panic</param>
    public void AssignVariables(float explosionSpeed, float fireSpeed, 
        float killRadius, float stunRadius, float panicRadius, float maxRadius,
        ExplosionManager explosionManager) {

        this.explosionSpeed = explosionSpeed;
        this.fireSpeed = fireSpeed;
        this.killRadius = killRadius;
        this.stunRadius = stunRadius;
        this.panicRadius = panicRadius;
        this.maxRadius = maxRadius;

        this.explosionManager = explosionManager;
    }

    /// <summary>
    /// Start the explosion "animation"
    /// </summary>
    /// <param name = "explosion" > The explosion object</param>
    /// <returns></returns>
    private void SpreadFire() {

        // Save this explosion radius
        currentRadius = desiredScale.x;

        // If our current radius is less than the one to kill NPCs...
        if (currentRadius < killRadius) {

            // ...Increase the radius with the appropriate speed
            desiredScale += Vector3.one * (Time.deltaTime * explosionSpeed);

        // If the current radius is bigger or equals the radius to kill NPCs
        // but it's smaller than the maximum one...
        } else if (currentRadius >= killRadius && currentRadius < maxRadius) {

            // ...Increase the radius with the appropriate speed
            desiredScale += Vector3.one * (Time.deltaTime * fireSpeed);

        // If the current radius is bigger or equals the maximum radius...
        } else if (currentRadius >= maxRadius) {

            // ...Destroy this explosion GameObject
            Destroy(gameObject);
        }

        // Apply the correct scale on the 'y' axis (by resetting it)
        desiredScale.y = transform.localScale.y;

        // Apply the new scale to this explosion GameObject
        transform.localScale = desiredScale;
    }

    /// <summary>
    /// Creates an OverlapSphere at the center of the Instantiated Explosion
    /// </summary>
    public void Explode() {

        // Gets all colliders around this GameObject
        Collider[] npcs = Physics.OverlapSphere(transform.position, maxRadius);

        // Iterates through all Colliders found...
        foreach(Collider c in npcs) {

            // ...finds the ones with the 'NPC' tag...
            if (c.CompareTag("NPC")) {

                // ...fetch its NPC Behaviour script...
                NPCBehaviour npc = c.GetComponent<NPCBehaviour>();

                // ...if the distance is smaller than the kill radius on the moment of the explosion...
                if (Vector3.Distance(transform.position, c.transform.position) <= killRadius) {

                    // ...'kill' the npc...
                    npc.IsDead = true;

                    // ...and update the UI display
                    explosionManager.UpdateKillCount();

                // ...if the distance is smaller than the stun radius on the moment of the explosion...
                } else if (Vector3.Distance(transform.position, npc.transform.position) <= stunRadius) {

                    // ...set its stun time...
                    npc.StunTime = explosionManager.StunTime;

                    // ...set the npc as Stunned...
                    npc.IsStunned = true;

                    // ...and also as Panicking (although he won't be able to run)
                    npc.IsPanicking = true;

                // ...if the distance is smaller than the panic radius on the moment of the explosion...
                } else if (Vector3.Distance(transform.position, npc.transform.position) <= panicRadius) {

                    // ...set the npc as Panicking (will be able to run)
                    npc.IsPanicking = true;
                }
            }
        }
    }

    /// <summary>
    /// Gets called when the Collider 'other' enters the trigger
    /// </summary>
    /// <param name="other">The Collider we hit</param>
    private void OnTriggerEnter(Collider other) {

        // If the 'other' Collider has an 'NPC' tag...
        if (other.CompareTag("NPC")) {

            // ...fetch its NPC Behaviour script...
            NPCBehaviour npc = other.GetComponent<NPCBehaviour>();

            // If the distance between the Explosion and the NPC is smaller (or equal)
            // that that of the current explosion radius...
            if (Vector3.Distance(transform.position, npc.transform.position) <= panicRadius) {

                // Verify if the NPC is already dead
                if (!npc.IsDead) {

                    // ...'kill' the NPC...
                    npc.IsDead = true;

                    // ...and update the UI display
                    explosionManager.UpdateKillCount();
                }

            // If the distance between the Explosion and the NPC is bigger
            // than that of the current explosion radius...
            } else {

                // ...set its stun time as 0 (so it starts running immediatelly 
                // but with half its original speed)...
                npc.StunTime = 0.0f;

                // ...set the npc as Stunned...
                npc.IsStunned = true;

                // ...and also as Panicking
                npc.IsPanicking = true;
            }
        }
    }

    //private void OnDrawGizmos() {

    //    Gizmos.color = Color.red;
    //    Gizmos.DrawWireSphere(transform.position, panicRadius);

    //    Gizmos.color = Color.yellow;
    //    Gizmos.DrawWireSphere(transform.position, stunRadius);

    //    Gizmos.color = Color.green;
    //    Gizmos.DrawWireSphere(transform.position, killRadius);

    //    Gizmos.color = Color.black;
    //    Gizmos.DrawWireSphere(transform.position, maxRadius);

    //    Gizmos.color = Color.white;
    //    Gizmos.DrawWireSphere(transform.position, currentRadius);
    //}
}
