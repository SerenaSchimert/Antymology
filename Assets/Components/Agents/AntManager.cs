using Antymology.Terrain;
using Antymology.Terrain;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using System;

    public class AntManager : MonoBehaviour
    {

        #region Fields/Properties

        public float maxHealth  = 100;
        public float health = 100;
        public float healthDepletionRate;
        public float timestep = 0.1f;
        public float timeElapsed = 0;

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

        public void Initialize(WorldManager world)
        {
            worldScript = world;
            maxHealth = 10;
            health = 10;
            healthDepletionRate = 0.05f;
        }

        // Time step method from below (although fairly intuitive anyway...)
        // https://answers.unity.com/questions/1220440/how-to-display-call-a-function-every-second.html
        // Update is called once per frame
     void Update()
        {
            timeElapsed += Time.deltaTime;
            if (timeElapsed >= timestep) {
                timeElapsed = timeElapsed % timestep;
                depleteHealth();

                dig();
                move();
            }

          }

        #endregion Initialize/Update

    #region Methods

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

            if ((block is MulchBlock) && (health < 50)) ;

            Vector3 forward = transform.forward;
            Vector3 back = -transform.forward;
            Vector3 right = transform.right;
            Vector3 left = -transform.right;

            float minOffset = 10;
            float offset;

            offset = validateMove(forward);
            if (offset < minOffset) minOffset = offset;

            offset = validateMove(back);
            if (offset < minOffset) minOffset = offset;

            offset = validateMove(right);
            if (offset < minOffset) minOffset = offset;

            offset = validateMove(left);
            if (offset < minOffset) minOffset = offset;

            if (minOffset < 1)
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

        offset = validateMove(forward);
            Tuple<string, Vector3, float> next = Tuple.Create("forward", forward, offset);
        if (offset != -10 && offset != 10) moveSet.Add(next);

        offset = validateMove(back);
        next = Tuple.Create("back", back, offset); 
        if (offset != -10 && offset != 10) moveSet.Add(next);

        offset = validateMove(right);
        next = Tuple.Create("right", right, offset);
        if (offset != -10 && offset != 10) moveSet.Add(next);

        offset = validateMove(left);
        next = Tuple.Create("left", left, offset);
        if (offset != -10 && offset != 10) moveSet.Add(next);



        if (moveSet.Count != 0) {

            Tuple<string, Vector3, float> move = moveSet[(int) UnityEngine.Random.Range(0, moveSet.Count)];

            float rotatey = 0;

            if (move.Item1 == "back") rotatey = 180;
            else if (move.Item1 == "right") rotatey = -90;
            else if (move.Item1 == "left") rotatey = 90;

            print("move");

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

    // Queen ant -> produces nest blocks -> separate script? -> probably, yes. So tags can be used...
    // Producing nest block consumes 1/3 of queen's max health
    // Probably want health exchanges to mostly be with queen...

    // Can create new ants every generation but not increase the current generation at all (the 'evaluation' phase?)

    #endregion Methods
    }



