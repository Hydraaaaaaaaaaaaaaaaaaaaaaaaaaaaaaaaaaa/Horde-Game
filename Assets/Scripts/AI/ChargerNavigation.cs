﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;

public class ChargerNavigation : MonoBehaviour
{
    public enum Type { SURVIVOR, PLAYER, BARRICADE, DEFENSES, TERRAIN, NULL };

    public Animator anim;

    [Serializable]
    public struct ObjTags
    {
        public string BarricadeTag;
        public string SurvivorTag;
        public string PlayerTag;
        public string DefensesTag;
        public string TerrainTag;
    }

    public ObjTags TypeTags;

    // Agent Data
    public NavMeshAgent agent;
    public NavMeshPath path;
    public Vector3 TargetPos = Vector3.zero;
    public GameObject EndPos;

    // Follow Bools
    public bool followPlayer = false;
    public bool followSurvivor = false;
    public bool charging = false;
    public bool canCharge = false;
    public bool needWindup = true;

    public bool holdingPlayer = false;

    // Gameobject Target Refs
    public GameObject player;
    public GameObject survivor;
    public GameObject barricade;
    public GameObject ChargeBarrier;

    // Transforms
    public Transform HoldingPos;

    // Speeds
    public float turningSpeed = 4.0f;
    public float acceleration = 4.0f;
    public float speed = 4.0f;
    public float chargeTurningSpeed = 0;
    public float chargeAcceleration = 7f;
    public float chargeSpeed = 10f;


    // Damages
    public int damage;
    public int chargeDamage;
    public int impactDamage;

    // Attack Ranges
    public float attackRange;
    public float chargeRange;
    public float chargeDistance;
    public float currentDist;

    // Cooldowns
    [Tooltip("Time in seconds between shots")]
    public float cooldown;
    public float currentCooldown;
    public float chargeWindupTime = 1f;
    public float currentChargeWindupTime;
    public float chargeCooldown = 20f;
    public float currentChargeCooldown;
    public float chargeChancePercentage = 20;

    public bool EnvironmentZombie = false;

    bool dead;

    void Start()
    {
        Physics.IgnoreCollision(GetComponent<SphereCollider>(), GameObjectManager.instance.endPos.GetComponent<BoxCollider>());

        path = new NavMeshPath();

        if (EnvironmentZombie)
        {
            EndPos = gameObject;
            TargetPos = EndPos.transform.position;
        }
        else
        {
            // Set the zombies target to the end of the map
            TargetPos = EndPos.transform.position;
        }
        // Get reference to the zombies agent
        agent = GetComponent<NavMeshAgent>();
        agent.CalculatePath(TargetPos, path);

        ChargeBarrier = transform.GetChild(1).gameObject;
        //player = GameObject.FindGameObjectWithTag("Player 1");
        currentChargeWindupTime = chargeWindupTime;

        GetComponent<Health>().OnDie = Die;
        dead = false;
    }

    void Update()
    {
        Debug.DrawLine(transform.position, TargetPos);

        if (Mathf.Abs(GetComponent<NavMeshAgent>().velocity.x) > 0 &&
            Mathf.Abs(GetComponent<NavMeshAgent>().velocity.z) > 0)
        {
            anim.SetBool("Moving", true);
        }
        else
        {
            anim.SetBool("Moving", false);
        }

        if (charging)
        {
            ChargeBarrier.SetActive(true);
        }
        else
        {
            // If there was a player held
            if (ChargeBarrier.GetComponent<GrapplingScript>().Player != null)
            {
                ChargeBarrier.GetComponent<GrapplingScript>().Player.GetComponent<PlayerMovScript>().incapacitationLevel = 0;
                Debug.Log("Removed Incapacitated from the player");
            }

            ChargeBarrier.SetActive(false);
        }

        if (ChargeBarrier.activeSelf == false)
        {
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
        }

        // Attack Cooldown
        if (currentCooldown > 0)
            currentCooldown -= Time.deltaTime;

        if (currentChargeCooldown > 0)
            currentChargeCooldown -= Time.deltaTime;

        if (currentChargeCooldown <= 0)
            canCharge = true;

        if (agent != null)
        {
            // If they're not charging the player
            if (!charging)
            {
                anim.SetBool("Charging", false);

                // Setting speed and turning speed
                agent.angularSpeed = turningSpeed;
                agent.acceleration = acceleration;
                agent.speed = speed;

                // if the charger can charge check and ...
                if (canCharge)
                {
                    if (UnityEngine.Random.Range(0, 101) <= chargeChancePercentage)
                        StartCharge();
                }

                // If the charger didnt start charging yet
                if (!charging)
                {
                    // Rotating towards movement direction
                    Vector3 dir = this.GetComponent<NavMeshAgent>().velocity;

                    // If the agent is moving
                    if (dir != Vector3.zero)
                    {
                        // Rotate in the direction of the velocity
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(dir),
                            Time.deltaTime * (turningSpeed * 2)
                        );
                    }

                    // If the agent dosent have a path to follow
                    if (!agent.hasPath)
                    {
                        //TargetPos = EndPos.transform.position;
                        agent.SetDestination(TargetPos);
                    }
                    // If the agent isnt under attack
                    if (GetComponent<Health>().Attacker == null)
                    {
                        // If the agent is following a player
                        if (player != null)
                            PlayerNotNull();

                        // If the agent is following a Survivor
                        else if (survivor != null)
                            SurvivorNotNull();

                        // If the agent is following a Barricade
                        else if (barricade != null)
                            BarricadeNotNull();

                        // If the agent isnt following anything
                        else
                        {
                            TargetPos = EndPos.transform.position;
                            agent.SetDestination(TargetPos);
                        }
                    }
                    else
                    {
                        BeingAttacked();
                    }
                }

                // If the agent has a path to follow
                if (agent.hasPath)
                {
                    agent.SetDestination(TargetPos);
                    currentDist = Vector3.Distance(transform.position, TargetPos);
                }
            }
            // They're charging the player
            else
            {
                Charging();
            }
        }
    }
    Vector3 pDir;

    void Charging()
    {
        anim.SetBool("Charging", true);
        // Setting speed and turning speed
        agent.angularSpeed = chargeTurningSpeed;
        agent.acceleration = chargeAcceleration;
        agent.speed = chargeSpeed;

        // If the agent has a path to follow
        if (agent.hasPath)
        {
            TargetPos = transform.position + (pDir * 3);

            //NavMeshHit navhit;
            //NavMesh.SamplePosition(TargetPos, out navhit, 1000, 0);
            //TargetPos = navhit.position;

            agent.SetDestination(TargetPos);
            currentDist = Vector3.Distance(transform.position, TargetPos);

            int layermask = 1 << LayerMask.NameToLayer("SeeThrough");
            layermask = ~layermask;
            layermask = 1 << LayerMask.NameToLayer("Terrain");
            
            if (Physics.Linecast(transform.position, TargetPos, layermask, QueryTriggerInteraction.Ignore))
            {
                TargetPos = transform.position + (pDir * 1.5f);
                agent.SetDestination(TargetPos);

                if (Physics.Linecast(transform.position, TargetPos, layermask, QueryTriggerInteraction.Ignore))
                {
                    if (ChargeBarrier.GetComponent<GrapplingScript>().Player != null)
                    {
                        ChargeBarrier.GetComponent<GrapplingScript>().Player.GetComponent<Health>().Damage(impactDamage);
                        ChargeBarrier.GetComponent<GrapplingScript>().Player.GetComponent<PlayerMovScript>().incapacitationLevel = 0;
                        ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                    }

                    player = null;
                    charging = false;
                    // Finished charging, wait a second before you continue attacking
                    currentCooldown = cooldown * 2;
                }
            }
        }

        if (currentDist < 1.1f)
        {
            charging = false;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
        }
    }

    void StartCharge()
    {
        currentChargeCooldown = chargeCooldown;

        agent.angularSpeed = chargeTurningSpeed;
        agent.acceleration = chargeAcceleration;
        agent.speed = chargeSpeed;

        if (player == null)
        {
            // then remove the player reference so it dosent keep tracking to them
            followPlayer = false;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
            return;
        }

        // If the player needs to be revived
        if (player.GetComponent<ReviveSystem>().NeedRes == true)
        {
            // then remove the player reference so it dosent keep tracking to them
            followPlayer = false;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
            return;
        }
        Vector3 direction = player.transform.position - transform.position;

        // If the player is still alive
        if (player.GetComponent<Health>().health > 0)
        {
            direction.Normalize();

            int layermask = 0;
            pDir = direction;
            NavMeshHit navHit;
            NavMesh.SamplePosition(transform.position + direction, out navHit, 1, layermask);
            agent.SetDestination(transform.position + direction);
            TargetPos = agent.destination;
            charging = true;
            canCharge = false;
            needWindup = true;
        }
        // If the player is dead
        else
        {
            // then remove the player reference so it dosent keep tracking to them
            followPlayer = false;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
            TargetPos = EndPos.transform.position;
            agent.SetDestination(TargetPos);
            return;
        }
        
        Quaternion rot = transform.rotation;
        rot.SetLookRotation(new Vector3(direction.x, rot.eulerAngles.y, direction.z));

        transform.rotation = rot;
    }

    void BeingAttacked()
    {
        // If the Attacker is within attack range
        if (Vector3.Distance(transform.position, GetComponent<Health>().Attacker.transform.position) < attackRange)
        {
            if (GetComponent<Health>().Attacker.GetComponent<Health>().health > 0)
            {
                Attack(player);

                if (GetComponent<Health>().Attacker.GetComponent<Health>().health <= 0)
                {
                    // then remove the player reference so it dosent keep tracking to them
                    followPlayer = false;
                    ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                    player = null;
                    TargetPos = EndPos.transform.position;
                    agent.SetDestination(TargetPos);
                    GetComponent<Health>().Attacker = null;
                }
            }
            else
            {
                // then remove the player reference so it dosent keep tracking to them
                followPlayer = false;
                ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                player = null;
                TargetPos = EndPos.transform.position;
                agent.SetDestination(TargetPos);
                GetComponent<Health>().Attacker = null;
            }
        }
        // if there is still an attacker alive and nearby
        if (GetComponent<Health>().Attacker != null)
        {
            // Target pos becomes the attackers position
            TargetPos = GetComponent<Health>().Attacker.transform.position;
            agent.SetDestination(TargetPos);
        }
    }

    void PlayerNotNull()
    {
        // If player is out of range
        if (Vector3.Distance(transform.position, player.transform.position) > GetComponent<SphereCollider>().radius * 3)
        {
            // then remove the player reference so it dosent keep tracking to them
            followPlayer = false;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
            return;
        }
        else
        {
            // Set the target position of the AI to the position of the player
            TargetPos = player.transform.position;
            agent.SetDestination(TargetPos);
        }
        // If the player is within attack range
        if (Vector3.Distance(transform.position, player.transform.position) < attackRange)
        {
            // If the player needs to be revived
            if (player.GetComponent<ReviveSystem>().NeedRes == true)
            {
                // then remove the player reference so it dosent keep tracking to them
                followPlayer = false;
                ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                player = null;
                return;
            }

            // If the player is still alive
            if (player.GetComponent<Health>().health > 0)
            {
                Attack(player);

                // If the player dies from the attack
                if (player.GetComponent<Health>().health <= 0)
                {
                    // then remove the player reference so it dosent keep tracking to them
                    followPlayer = false;
                    ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                    player = null;
                    TargetPos = EndPos.transform.position;
                    agent.SetDestination(TargetPos);
                    return;
                }
            }
            // If the player is dead
            else
            {
                // then remove the player reference so it dosent keep tracking to them
                followPlayer = false;
                ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                player = null;
                TargetPos = EndPos.transform.position;
                agent.SetDestination(TargetPos);
                return;
            }
        }

        // If the player is within charge range
        if (Vector3.Distance(transform.position, player.transform.position) < chargeRange && !canCharge)
        {
            // If the charger is ready to charge
            if (currentChargeCooldown <= 0)
            {
                canCharge = true;
            }
            else
            {
                currentChargeCooldown -= Time.deltaTime;
            }
        }

        // if the player object is turned off
        if (player.activeSelf == false)
        {
            // Tell the AI to travel where the player was so it can track to last known position
            TargetPos = player.transform.position;

            // then remove the player reference so it dosent keep tracking to them
            followPlayer = false;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
            return;
        }
    }

    void SurvivorNotNull()
    {
        // If the player is within attack range
        if (Vector3.Distance(transform.position, survivor.transform.position) < attackRange)
        {
            Attack(survivor);
        }

        if (survivor.GetComponent<Health>().health <= 0)
        {
            // Remove the barricade reference and start going to exit again
            survivor = null;
            TargetPos = EndPos.transform.position;
        }
        else
        {
            TargetPos = survivor.transform.position;
            agent.SetDestination(TargetPos);
        }
    }

    void BarricadeNotNull()
    {
        // If the player is within attack range
        if (Vector3.Distance(transform.position, barricade.transform.position) < attackRange)
        {
            Attack(barricade);
        }

        // If the barricade is dead
        if (barricade.GetComponent<Health>().health <= 0)
        {
            // Remove the barricade reference and start going to exit again
            barricade = null;
            TargetPos = EndPos.transform.position;
        }
        else
        {
            TargetPos = barricade.transform.position;
            agent.SetDestination(TargetPos);
        }
    }

    void OnTriggerStay(Collider col)
    {
        if (!charging)
        {
            // If they ran into another enemy, dont bother continuing
            if (col.CompareTag("Enemy"))
            {
                return;
            }
            //// If they found a barricade
            //if (col.CompareTag(TypeTags.BarricadeTag))
            //{
            //    CheckForBarricade(col);
            //    return;
            //}
            // IF they found a player
            if (col.CompareTag(TypeTags.PlayerTag))
            {
                CheckForPlayer(col);
                return;
            }
            // If they found a survivor
            if (col.CompareTag(TypeTags.SurvivorTag))
            {
                CheckForSurvivor(col);
                return;
            }
        }
    }

    void CheckForBarricade(Collider col)
    {
        if (barricade == null)
        {
            barricade = col.gameObject;
            ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
            player = null;
            survivor = null;
            followPlayer = false;
            TargetPos = col.transform.position;
            agent.SetDestination(TargetPos);

        }
    }

    void CheckForPlayer(Collider col)
    {
        if (col.GetComponent<ReviveSystem>().NeedRes != true)
        {
            int layermask = 1 << LayerMask.NameToLayer("SeeThrough");
            layermask = 1 << LayerMask.NameToLayer("Enemy");
            layermask = ~layermask;

            if (survivor == null && barricade == null)
            {
                if (Physics.Linecast(transform.position, col.transform.position, layermask, QueryTriggerInteraction.Ignore))
                {
                    //Debug.Log("Can see player");
                    // If this player is closer than the other player in the scene
                    if (player == null)
                    {
                        player = col.gameObject;
                        followPlayer = true;
                        TargetPos = col.transform.position;
                        agent.SetDestination(TargetPos);
                    }
                    else if (Vector3.Distance(this.transform.position, player.transform.position) >
                            Vector3.Distance(this.transform.position, col.transform.position))
                    {
                        player = col.gameObject;
                        followPlayer = true;
                        TargetPos = col.transform.position;
                        agent.SetDestination(TargetPos);
                    }
                }
            }
        }
    }

    void CheckForSurvivor(Collider col)
    {
        int layermask = 1 << LayerMask.NameToLayer("SeeThrough");
        layermask = 1 << LayerMask.NameToLayer("Enemy");
        layermask = ~layermask;

        // Can they see a Survivor && is the survivor in range
        if (Physics.Linecast(this.transform.position, col.transform.position, layermask, QueryTriggerInteraction.Ignore))
        {
            // If the agent isnt following a survivor already
            if (survivor == null)
            {
                // Remove any prior priority to player tracking
                ChargeBarrier.GetComponent<GrapplingScript>().Player = null;
                player = null;
                followPlayer = false;

                // Set the survivor reference
                survivor = col.gameObject;
                followSurvivor = true;

                // Set survivor position
                TargetPos = col.transform.position;
                agent.SetDestination(TargetPos);
            }

            // If there is another survivor within range of the agent
            else if (Vector3.Distance(this.transform.position, survivor.transform.position) >
                    Vector3.Distance(this.transform.position, col.transform.position))
            {
                survivor = col.gameObject;
                followSurvivor = true;
                TargetPos = col.transform.position;
                agent.SetDestination(TargetPos);
            }
        }
    }

    void Attack(GameObject obj)
    {
        if (obj != null)
        {
            if (!charging)
            {
                if (currentCooldown <= 0)
                {
                    Debug.Log("Attacked");
                    currentCooldown = cooldown;
                    obj.GetComponent<Health>().Damage(damage);
                }
            }
        }
    }

    void Die(GameObject source = null)
    {
        if (!dead)
        {
            dead = true;
            if (source != null && source.CompareTag("Player"))
                ScoreManager.instance.ChargerKill(source);
            Destroy(gameObject);
        }
    }
}
