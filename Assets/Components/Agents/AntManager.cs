using Antymology.Terrain;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntManager : MonoBehaviour
{

    #region Fields/Properties

    public const float maxHealth = 100;
    public float health = 100;
    public float healthDepletionRate = 0.05f;
    public const float timestep = 0.2f;
    public float timeElapsed = 0;
    public const float mulchValue = 30;

    // FITNESS METRIC FOR SELECTING NEXT GENERATION'S PARENT PAIR
    public float lifetimeHealthtoQueen = 0; // health given to queen over course of ant's life

    // Is queen
    public bool queen;  // Is the ant the queen?
    // The queen will not inherit genes, the genes will be pre-set 
    public float queenEatPoint = 5; // don't want to dig too much
    public int queenstepsOffAcid = 10; // queen gets far off that acid

    // genes -> worker ants can evolve -> for probabilities, lower values are actually higher (size of range in random selection)
    public float eatPoint; // health level below which ant will consume mulch when on mulch block and without trapping itself
    public float probDig; // dig probability given not being trapped is checked already
    public float stepsOffAcid; // number of steps off of acid
    public float healthToQueen; // amount of queen's missing health to re-fill (i.e., full, half of what's missing etc.)
    public float probHealthtoAnt; // probability of giving health (25% of current health) to non-queen ant if other ant's health is < 50 and this ant's health is > 50
    public float forwardBias; // an added bias to go forward if available before random selection of movement from available moveset

    // other fields
    public bool fleeState = false; // ants fleeing acid -> lasts until number of non-backward steps off of acid -> after one backward step to turn around
    public float stepsOff = 0; // non-backward steps taken off of acid so far

    public WorldManager worldScript;

    // gene variables

    #endregion

    #region Initialize/Update

    // Start is called before the first frame update
    void Awake()
    {
        // Extract rigid body
        Rigidbody rigidbody = GetComponent<Rigidbody>();
    }

    public void Initialize(WorldManager world, bool isQueen)
    {
        worldScript = world;
        queen = isQueen;

        if (queen) {
            eatPoint = queenEatPoint;
            stepsOffAcid = queenstepsOffAcid;
        }

    }

    public void InitializeWorker(float eatPoint, float probDig, float stepsOffAcid, float healthToQueen, float probHealthtoAnt, float forwardBias) {

        this.eatPoint = eatPoint;
        this.probDig = probDig;
        this.stepsOffAcid = stepsOffAcid;
        this.healthToQueen = healthToQueen;
        this.probHealthtoAnt = probHealthtoAnt;
        this.forwardBias = forwardBias;
    }

    void Update()
        {
            
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= timestep) {

                checkAcid();
                timeElapsed = timeElapsed % timestep;
                depleteHealth();
                dig();
                move();
            }

          }

    #endregion Initialize/Update

    #region Methods

    // Increase health drop rate *2 if standing on acid
    void checkAcid() {

        if (stepsOff == stepsOffAcid)
        {
            fleeState = false;
            stepsOff = 0;
        }

        if (worldScript.GetBlock((int)transform.position.x, (int)transform.position.y - 1, (int)transform.position.z) is AcidicBlock)
        {
            healthDepletionRate = 0.1f;
            fleeState = true;
        }
        else healthDepletionRate = 0.05f;

    }

    // Ant dies -> already implemented in WorldManager
    // reduce ant health -> use fixed timestep, i.e. time.deltaTime
    void depleteHealth() {
        health -= healthDepletionRate;
    }

    /*
    // Ant may give health to another ant on same block (zero-sum exchange)
    // Which ant is exchanging should be random for now
    void exchangeHealth() { 

    }
    */

    // Ants action 'dig' -> if on top of block and block is not container block, 'dig' up block (fall to block below??), and remove block from the map
    // Ants standing on AcidBlock have health decrease rate multiplied by 2
    // If ant ontop of mulch block -> consume mulch block and remove from world -> maybe to in world manager instead?
    // Make sure that only one of the ants on the block can consume it-> definitely sounds like maybe should be in world manager...
    void dig() {

        Vector3 position = transform.position;
        AbstractBlock block = worldScript.GetBlock((int) position.x, (int) position.y - 1, (int) position.z);
        if (!(block is ContainerBlock)) {

            Vector3 forward = transform.forward;
            Vector3 back = -transform.forward;
            Vector3 right = transform.right;
            Vector3 left = -transform.right;

            float minOffset = 10;
            float offset;
            bool tooHigh = true;

            offset = validateMove(forward);
            if (offset < minOffset) minOffset = offset;
            if (offset > -3) tooHigh = false;

            offset = validateMove(back);
            if (offset < minOffset) minOffset = offset;
            if (offset > -3) tooHigh = false;

            offset = validateMove(right);
            if (offset < minOffset) minOffset = offset;
            if (offset > -3) tooHigh = false;

            offset = validateMove(left);
            if (offset < minOffset) minOffset = offset;
            if (offset > -3) tooHigh = false;

            if ((block is MulchBlock) && (health < eatPoint) && minOffset < 2) {

                // dig up and consume mulch
                AbstractBlock airblock = new AirBlock();
                worldScript.SetBlock((int)position.x, (int)position.y - 1, (int)position.z, airblock);
                transform.position = new Vector3(position.x, position.y - 1, position.z);

                health += mulchValue;  // replenish health
             }

            else if ((minOffset < 0 && UnityEngine.Random.Range(0, 5) == 0  && !queen && !fleeState) || tooHigh )
            {
                AbstractBlock airblock = new AirBlock();
                worldScript.SetBlock((int)position.x, (int)position.y - 1, (int)position.z, airblock);
                transform.position = new Vector3(position.x, position.y - 1, position.z);
            }
        }

    }

    // Ant action 'move' -> random direction (up/down/right/left) for now, but can only move to a block that is within 2 units of height difference
    void move() {

        Vector3 forward = transform.forward;
        Vector3 back = -transform.forward;
        Vector3 right = transform.right;
        Vector3 left = -transform.right;

        Vector3 position= transform.position;

        List<Tuple<string, Vector3, float>> moveSet = new List<Tuple<string, Vector3, float>>();

        float offset;
        Tuple<string, Vector3, float> next;

        if (!fleeState || !(fleeState && stepsOff == 0))
        {
            offset = validateMove(forward);
            next = Tuple.Create("forward", forward, offset);
            if (offset != -10 && offset != 10) moveSet.Add(next);
        }

        if (!fleeState || (fleeState && stepsOff == 0))
        {
            offset = validateMove(back);
            next = Tuple.Create("back", back, offset);
            if (offset != -10 && offset != 10) moveSet.Add(next);
        }

        if (!fleeState || !(fleeState && stepsOff == 0))
        {
            offset = validateMove(right);
            next = Tuple.Create("right", right, offset);
            if (offset != -10 && offset != 10) moveSet.Add(next);
        }

        if (!fleeState || !(fleeState && stepsOff == 0))
        {
            offset = validateMove(left);
            next = Tuple.Create("left", left, offset);
            if (offset != -10 && offset != 10) moveSet.Add(next);
        }

        if (moveSet.Count != 0) {

            Tuple<string, Vector3, float> move;

            if (moveSet[0].Item1 == "forward" && UnityEngine.Random.Range(0, forwardBias) == 0) move = moveSet[0];
            else move = moveSet[(int)UnityEngine.Random.Range(0, moveSet.Count)];

            float rotatey = 0;

            if (move.Item1 == "back") rotatey = 180;
            else if (move.Item1 == "right") rotatey = -90;
            else if (move.Item1 == "left") rotatey = 90;

            transform.Rotate(0, rotatey, 0, Space.Self);
            transform.position = new Vector3(position.x + move.Item2.x, position.y + move.Item3, position.z + move.Item2.z);
        }

    }

    float validateMove(Vector3 move) {

        Vector3 position = transform.position;

        for (int offset = -2; offset <= 2; offset++) {

            if (!(worldScript.GetBlock((int)(position.x + move.x), (int)(position.y + (offset - 1)), (int)(position.z + move.z)) is AirBlock)) {

                if (worldScript.GetBlock((int)(position.x + move.x), (int)(position.y + offset), (int)(position.z + move.z)) is AirBlock) return offset;

            } else if (offset == -2) return -10;
        }

        return 10;
    }

    // Producing nest block consumes 1/3 of queen's max health

    // health exchanges
    void OnCollisionEnter(Collision other) {

        if (!queen && !other.gameObject.GetComponent<AntManager>().queen)
        {

            if (other.gameObject.GetComponent<AntManager>().health < 50 && health > 50)
            {

                if (UnityEngine.Random.Range(0, probHealthtoAnt) == 0)
                {
                    health -= 25;
                    other.gameObject.GetComponent<AntManager>().health += 25;
                }
            }
            else if (other.gameObject.GetComponent<AntManager>().health > 50 && health < 50)
            {

                if (UnityEngine.Random.Range(0, other.gameObject.GetComponent<AntManager>().probHealthtoAnt) == 0)
                {
                    health += 25;
                    other.gameObject.GetComponent<AntManager>().health -= 25;
                }
            }

        }
        else if (queen)
        {
            float healthExchange = other.gameObject.GetComponent<AntManager>().healthToQueen * (maxHealth - health);
            health += healthExchange;
            other.gameObject.GetComponent<AntManager>().health -= healthExchange;
        }
        else if (other.gameObject.GetComponent<AntManager>().queen) {

            float healthExchange = healthToQueen * (maxHealth - other.gameObject.GetComponent<AntManager>().health);
            health -= healthExchange;
            other.gameObject.GetComponent<AntManager>().health += healthExchange;

        }
    }

    // Can create new ants every generation but not increase the current generation at all (the 'evaluation' phase?)

    #endregion Methods
}



