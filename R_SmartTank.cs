using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static R_BTBaseNode;

// require R_StateMachine
[RequireComponent(typeof(R_StateMachine))]

public class R_SmartTank : AITank
{
    // consumables: hp +25, ammo +3, fuel +30
    // projectile: 15 dmg
    [SerializeField] private GameObject empty;
    private GameObject startPos, enemyLastSeen;

    // store ALL currently visible 
    public Dictionary<GameObject, float> enemyTanksFound = new();
    public Dictionary<GameObject, float> consumablesFound = new();
    public Dictionary<GameObject, float> enemyBasesFound = new();

    // store ONE from ALL currently visible
    public GameObject enemyTankPosition;
    public GameObject consumablePosition;
    public GameObject enemyBasePosition;

    // timer
    private float t;

    // store facts and rules (RBS)
    public Dictionary<string, bool> stats = new();
    public R_Rules rules = new();

    // store behavioural tree data (BT)
    public R_BTAction healthCheck;
    public R_BTAction fuelCheck;
    public R_BTAction ammoCheck;
    public R_BTAction targetSpottedCheck;
    public R_BTAction targetReachedCheck;
    public R_BTAction targetEscapedCheck;
    public R_BTSelector regenSequence;

    private void Awake()
    {
        InitialiseStateMachine();
    }

    // AITankStart() used instead of Start()
    public override void AITankStart()
    {
        // throw error if empty not in prefab
        if (empty == null) Invoke(nameof(ThrowError), 0.5f);

        // set startPos
        startPos = Instantiate(empty, transform.position, Quaternion.identity);
        startPos.transform.localScale = Vector3.one;

        // add facts
        stats.Add("lowHealth", false);
        stats.Add("lowFuel", false);
        stats.Add("noAmmo", false);
        stats.Add("targetSpotted", false);
        stats.Add("targetReached", false);
        stats.Add("targetEscaped", false);
        stats.Add("targetOutOfRange", false);
        stats.Add("searchState", true);
        stats.Add("chaseState", false);
        stats.Add("fleeState", false);
        stats.Add("attackState", false);

        // add rules
        rules.AddRule(new R_Rule("searchState", "targetSpotted", typeof(R_ChaseState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("chaseState", "targetReached", typeof(R_AttackState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("chaseState", "targetEscaped", typeof(R_SearchState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("attackState", "targetOutOfRange", typeof(R_ChaseState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("attackState", "lowHealth", typeof(R_FleeState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("attackState", "lowFuel", typeof(R_FleeState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("attackState", "targetEscaped", typeof(R_SearchState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("attackState", "noAmmo", typeof(R_FleeState), R_Rule.Predicate.AND));
        rules.AddRule(new R_Rule("fleeState", "targetEscaped", typeof(R_SearchState), R_Rule.Predicate.AND));

        // add BT Actions
        healthCheck = new R_BTAction(HealthCheck);
        fuelCheck = new R_BTAction(FuelCheck);
        ammoCheck = new R_BTAction(AmmoCheck);
        targetSpottedCheck = new R_BTAction(TargetSpottedCheck);
        targetReachedCheck = new R_BTAction(TargetReachedCheck);
        targetEscapedCheck = new R_BTAction(TargetEscapedCheck);
        regenSequence = new R_BTSelector(new List<R_BTBaseNode> { healthCheck, ammoCheck, fuelCheck });
    }

    // throw error if prefab not used
    public void ThrowError()
    {
        throw new Exception("\"Empty\" prefab not set in R_SmartTank serialized fields. Please add \"Empty\" prefab in \"Rock > Prefabs\" to R_SmartTank prefabs.");
    }

    // AITankUpdate() in place of Update()
    public override void AITankUpdate()
    {
        // update all currently visible enemies, consumables and bases
        enemyTanksFound = VisibleEnemyTanks;
        consumablesFound = VisibleConsumables;
        enemyBasesFound = VisibleEnemyBases;

        // check for enemies, consumables and bases found
        enemyTankPosition = (enemyTanksFound.Count > 0) ? enemyTanksFound.First().Key : null;
        consumablePosition = (consumablesFound.Count > 0) ? consumablesFound.First().Key : null;
        enemyBasePosition = (enemyBasesFound.Count > 0) ? enemyBasesFound.First().Key : null;

        // set facts
        stats["lowHealth"] = TankCurrentHealth < 35f;
        stats["lowFuel"] = TankCurrentFuel < 20f;
        stats["noAmmo"] = TankCurrentAmmo == 0f;
        stats["targetSpotted"] = enemyTankPosition != null || consumablePosition != null || enemyBasePosition != null;
        stats["targetEscaped"] = !stats["targetSpotted"];
        stats["targetReached"] = false;
        stats["targetOutOfRange"] = false;
        if (stats["targetEscaped"]) return;
        if (enemyTankPosition != null)
        {
            stats["targetReached"] = Vector3.Distance(transform.position, enemyTankPosition.transform.position) < 35f &&
                                     Vector3.Distance(transform.position, enemyTankPosition.transform.position) > 10f;
            stats["targetOutOfRange"] = !stats["targetReached"];
        }
        else if (consumablePosition != null)
        {
            stats["targetReached"] = Vector3.Distance(transform.position, consumablePosition.transform.position) < 0f;
            stats["targetOutOfRange"] = !stats["targetReached"];
        }
        else if (enemyBasePosition != null)
        {
            stats["targetReached"] = Vector3.Distance(transform.position, enemyBasePosition.transform.position) < 25f;
            stats["targetOutOfRange"] = !stats["targetReached"];
        }
    }

    // add states to dictionary
    protected void InitialiseStateMachine()
    {
        Dictionary<Type, R_BaseState> states = new()
        {
            { typeof(R_SearchState), new R_SearchState(this) },
            { typeof(R_ChaseState), new R_ChaseState(this) },
            { typeof(R_FleeState), new R_FleeState(this) },
            { typeof(R_AttackState), new R_AttackState(this) }
        };

        GetComponent<R_StateMachine>().SetStates(states);
    }

    public void Search()
    {
        // don't search at full speed to conserve fuel
        FollowPathToRandomWorldPoint(0.7f);
        t += Time.deltaTime;
        // search for 5 seconds before generating a new random destination
        if (t > 5)
        {
            GenerateNewRandomWorldPoint();
            t = 0;
        }
    }

    public void Chase()
    {
        // declare var for destination position and speed (dependant on urgency)
        GameObject pos;
        float normalisedSpeed;
        // using behavioural tree to find if tank can fight another tank
        if (enemyTankPosition != null && regenSequence.Evaluate() == R_BTNodeStates.SUCCESS)
        {
            pos = enemyTankPosition;
            // maximum speed
            normalisedSpeed = 1f;
        }
        // second priority is consumables
        else if (consumablePosition != null)
        {
            pos = consumablePosition;
            // less urgency so lower speed
            normalisedSpeed = 0.7f;
        }
        else if (enemyBasePosition != null)
        {
            pos = enemyBasePosition;
            // lowest urgency so lowest speed
            normalisedSpeed = 0.5f;
        }
        else return;
        // go to position at set speed
        FollowPathToWorldPoint(pos, normalisedSpeed);
        TurretFaceWorldPoint(pos);
    }

    public void Retreat()
    {
        // first part of flee state
        // updates last seen enemy position
        if (enemyTankPosition != null)
        {
            if (enemyLastSeen) Destroy(enemyLastSeen);
            enemyLastSeen = Instantiate(empty, enemyTankPosition.transform.position, Quaternion.identity);
        }

        // falls back from last seen enemy position
        GameObject runPos = Instantiate(empty, transform.position, Quaternion.identity);
        runPos.transform.position = 2 * transform.position - enemyLastSeen.transform.position;
        TurretFaceWorldPoint(enemyLastSeen);
        FollowPathToWorldPoint(runPos, 1f);
        Destroy(runPos);
    }

    public void Flee()
    {
        // second part of flee state
        // returns to start point
        FollowPathToWorldPoint(startPos, 1f);
    }

    public void Attack()
    {
        // attacks enemy tanks before enemy bases
        if (enemyTankPosition != null)
            TurretFireAtPoint(enemyTankPosition);
        else if (enemyBasePosition != null)
            TurretFireAtPoint(enemyBasePosition);
    }

    // AIOnCollisionEnter() in place of OnCollisionEnter()
    public override void AIOnCollisionEnter(Collision collision) { }

    // BTNodeStates functions
    public R_BTNodeStates HealthCheck()
    {
        if (stats["lowHealth"])
            return R_BTNodeStates.FAILURE;
        else
            return R_BTNodeStates.SUCCESS;
    }

    public R_BTNodeStates FuelCheck()
    {
        if (stats["lowFuel"])
            return R_BTNodeStates.FAILURE;
        else
            return R_BTNodeStates.SUCCESS;
    }

    public R_BTNodeStates AmmoCheck()
    {
        if (stats["noAmmo"])
            return R_BTNodeStates.FAILURE;
        else
            return R_BTNodeStates.SUCCESS;
    }

    public R_BTNodeStates TargetSpottedCheck()
    {
        if (stats["targetSpotted"])
            return R_BTNodeStates.SUCCESS;
        else
            return R_BTNodeStates.FAILURE;
    }

    public R_BTNodeStates TargetReachedCheck()
    {
        if (stats["targetReached"])
            return R_BTNodeStates.SUCCESS;
        else
            return R_BTNodeStates.FAILURE;
    }

    public R_BTNodeStates TargetEscapedCheck()
    {
        if (stats["targetEscaped"])
            return R_BTNodeStates.SUCCESS;
        else
            return R_BTNodeStates.FAILURE;
    }

    /// <summary>
    /// Generate a path from current position to pointInWorld (GameObject)
    /// </summary>
    public void GeneratePathToWorldPoint(GameObject pointInWorld)
    {
        FindPathToPoint(pointInWorld);
    }

    /// <summary>
    ///Generate and Follow path to pointInWorld (GameObject) at normalizedSpeed (0-1)
    /// </summary>
    public void FollowPathToWorldPoint(GameObject pointInWorld, float normalizedSpeed)
    {
        FollowPathToPoint(pointInWorld, normalizedSpeed);
    }

    /// <summary>
    ///Generate and Follow path to a randome point at normalizedSpeed (0-1)
    /// </summary>
    public void FollowPathToRandomWorldPoint(float normalizedSpeed)
    {
        FollowPathToRandomPoint(normalizedSpeed);
    }

    /// <summary>
    ///Generate new random point
    /// </summary>
    public void GenerateNewRandomWorldPoint()
    {
        GenerateRandomPoint();
    }

    /// <summary>
    /// Stop Tank at current position.
    /// </summary>
    public void TankStop()
    {
        StopTank();
    }

    /// <summary>
    /// Continue Tank movement at last know speed and pointInWorld path.
    /// </summary>
    public void TankGo()
    {
        StartTank();
    }

    /// <summary>
    /// Face turret to pointInWorld (GameObject)
    /// </summary>
    public void TurretFaceWorldPoint(GameObject pointInWorld)
    {
        FaceTurretToPoint(pointInWorld);
    }

    /// <summary>
    /// Reset turret to forward facing position
    /// </summary>
    public void TurretReset()
    {
        ResetTurret();
    }

    /// <summary>
    /// Face turret to pointInWorld (GameObject) and fire (has delay).
    /// </summary>
    public void TurretFireAtPoint(GameObject pointInWorld)
    {
        FireAtPoint(pointInWorld);
    }

    /// <summary>
    /// Returns true if the tank is currently in the process of firing.
    /// </summary>
    public bool TankIsFiring()
    {
        return IsFiring;
    }

    /// <summary>
    /// Returns float value of remaining health.
    /// </summary>
    public float TankCurrentHealth
    {
        get
        {
            return GetHealthLevel;
        }
    }

    /// <summary>
    /// Returns float value of remaining ammo.
    /// </summary>
    public float TankCurrentAmmo
    {
        get
        {
            return GetAmmoLevel;
        }
    }

    /// <summary>
    /// Returns float value of remaining fuel.
    /// </summary>
    public float TankCurrentFuel
    {
        get
        {
            return GetFuelLevel;
        }
    }

    /// <summary>
    /// Returns list of friendly bases.
    /// </summary>
    protected List<GameObject> MyBases
    {
        get
        {
            return GetMyBases;
        }
    }

    /// <summary>
    /// Returns Dictionary(GameObject target, float distance) of visible targets (tanks in TankMain LayerMask).
    /// </summary>
    protected Dictionary<GameObject, float> VisibleEnemyTanks
    {
        get
        {
            return TanksFound;
        }
    }

    /// <summary>
    /// Returns Dictionary(GameObject consumable, float distance) of visible consumables (consumables in Consumable LayerMask).
    /// </summary>
    protected Dictionary<GameObject, float> VisibleConsumables
    {
        get
        {
            return ConsumablesFound;
        }
    }

    /// <summary>
    /// Returns Dictionary(GameObject base, float distance) of visible enemy bases (bases in Base LayerMask).
    /// </summary>
    protected Dictionary<GameObject, float> VisibleEnemyBases
    {
        get
        {
            return BasesFound;
        }
    }
}
