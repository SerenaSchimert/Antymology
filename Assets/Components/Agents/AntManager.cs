using Antymology.Terrain;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AntManager : MonoBehaviour
{

    #region Fields/Properties

    public UnityEngine.UI.Text value;

    public int numNestBlocks = 0;

    // Metrics leading to ant death, health depletes by a steady rate overtime, moreso if on an acid block
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
    public int queenstepsOffAcid = 10; // queen gets far off that acid - high self preservation -? well, exept for the not eating bit...

    // genes -> worker ants can evolve -> for probabilities, lower values are actually higher (size of range in random selection)
    public float eatPoint; // health level below which ant will consume mulch when on mulch block and without trapping itself
    public float probDig; // dig probability given not being trapped is checked already
    public float stepsOffAcid; // number of steps off of acid
    public float healthToQueen; // amount of queen's missing health to re-fill (i.e., full, half of what's missing etc.)
    public float probHealthtoAnt; // probability of giving health (25% of current health) to non-queen ant if other ant's health is < 50 and this ant's health is > 50
    public float forwardBias; // an added bias to go forward if available before random selection of movement from available moveset

    // other fields
    public bool fleeState = false; // ants fleeing acid -> lasts until number steps off of acid -> one backward step to turn around + more non-backward steps
    public float stepsOff = 0; // steps taken to leave of acid so far

    public WorldManager worldScript;

    // gene variables

    #endregion

    #region Initialize/Update

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

    // Called from WorldManager where input genes were calculated
    public void InitializeWorker(float eatPoint, float probDig, float stepsOffAcid, float healthToQueen, float probHealthtoAnt, float forwardBias) {

        this.eatPoint = eatPoint;
        this.probDig = probDig;
        this.stepsOffAcid = stepsOffAcid;
        this.healthToQueen = healthToQueen;
        this.probHealthtoAnt = probHealthtoAnt;
        this.forwardBias = forwardBias;
    }

    public void setPosition(Vector3 pos) {

        transform.position = pos;
    }

    void Update()
        {
            
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= timestep) {

                checkAcid();
                //timeElapsed = timeElapsed % timestep;
                timeElapsed = 0;
                depleteHealth();
            if (queen && health >= maxHealth / 2) buildNest();
            else {
                dig();
                move();
            }
            }

          }

    #endregion Initialize/Update

    #region Methods

    // Increase health drop rate *2 if standing on acid
    void checkAcid() {

        // Check whether ant has fleed to the amount specified in their genes
        if (stepsOff == stepsOffAcid)
        {
            fleeState = false;
            stepsOff = 0;
        }

        if (worldScript.GetBlock((int)transform.position.x, (int)transform.position.y - 1, (int)transform.position.z) is AcidicBlock)
        {
            healthDepletionRate = 0.1f;
            fleeState = true;
            stepsOff = 0;
        }
        else healthDepletionRate = 0.05f;

    }

    // reduce ant health -> use fixed timestep, i.e. time.deltaTime
    // Eventually ant dies -> this is already checked/handled in WorldManager
    void depleteHealth() {
        health -= healthDepletionRate;
    }

    /*
    // Ant may give health to another ant on same block (zero-sum exchange)
    // Which ant is exchanging should be random for now
    void exchangeHealth() { 

    }
    */

    // Ants action 'dig' -> if on top of block and block is not container block, 'dig' up block (fall to block below...), and remove block from the map
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

            else if ((minOffset < 0 && UnityEngine.Random.Range(1, probDig) == 1  && !queen && !fleeState) || tooHigh )
            {
                AbstractBlock airblock = new AirBlock();
                worldScript.SetBlock((int)position.x, (int)position.y - 1, (int)position.z, airblock);
                transform.position = new Vector3(position.x, position.y - 1, position.z);
            }
        }

    }

    // Ant action 'move' -> random direction (up/down/right/left) for now (might be fun to add extra components to genes for this), but can only move to a block that is within 2 units of height difference
    void move() {

        Vector3 forward = transform.forward;
        Vector3 back = -transform.forward;
        Vector3 right = transform.right;
        Vector3 left = -transform.right;

        Vector3 position= transform.position;

        List<Tuple<string, Vector3, float>> moveSet = new List<Tuple<string, Vector3, float>>();

        float offset;
        Tuple<string, Vector3, float> next;

        if (!(fleeState && stepsOff == 0))
        {
            offset = validateMove(forward);
            next = Tuple.Create("forward", forward, offset);
            if (offset != -10 && offset != 10) moveSet.Add(next);
        }

        if (!(fleeState && stepsOff >= 1))
        {
            offset = validateMove(back);
            next = Tuple.Create("back", back, offset);
            if (offset != -10 && offset != 10) moveSet.Add(next);
        }

        offset = validateMove(right);
        next = Tuple.Create("right", right, offset);
        if (offset != -10 && offset != 10) moveSet.Add(next);

        offset = validateMove(left);
        next = Tuple.Create("left", left, offset);
        if (offset != -10 && offset != 10) moveSet.Add(next);

        if (moveSet.Count != 0) {

            Tuple<string, Vector3, float> move;

            // In fleestate any valid move back is taken, otherwise there may be a forward bias
            if (moveSet[0].Item1 == "forward" && UnityEngine.Random.Range(0, forwardBias) == 0 || (fleeState && stepsOff == 0)) move = moveSet[0];
            else move = moveSet[(int)UnityEngine.Random.Range(0, moveSet.Count)];

            float rotatey = 0;

            if (move.Item1 == "back") rotatey = 180;
            else if (move.Item1 == "right") rotatey = -90;
            else if (move.Item1 == "left") rotatey = 90;

            transform.Rotate(0, rotatey, 0, Space.Self);
            transform.position = new Vector3(position.x + move.Item2.x, position.y + move.Item3, position.z + move.Item2.z);

            if (fleeState) stepsOff++;
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
    void buildNest() {

        AbstractBlock nestBlock = new NestBlock();
        Vector3 oldPos = transform.position;
        move();
        worldScript.SetBlock((int)oldPos.x, (int)oldPos.y, (int)oldPos.z, nestBlock);
        health -= 0.33f * maxHealth;
        worldScript.numNestBlocks++;

    }

    // health exchanges
    void OnCollisionEnter(Collision other) {

        try
        {
            print(other.gameObject.GetComponent<AntManager>().queen);
        }
        catch(Exception e) {
            return;
        }

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



