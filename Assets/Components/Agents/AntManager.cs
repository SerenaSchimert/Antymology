using Antymology.Terrain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    public class AntManager : MonoBehaviour
    {

        #region Fields/Properties

        public float maxHealth;
        public float health;
        public float healthDepletionRate;
        public float timestep = 0.1f;
        public float timeElapsed = 0;

        public WorldManager worldScript;

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
            }
            
        }

        #endregion Initialize/Update

        #region Methods

        // Ant dies -> already implemented in WorldManager
        // reduce ant health -> use fixed timestep, i.e. time.deltaTime
        void depleteHealth() {
            health -= healthDepletionRate;
        }

        // Ant may give health to another ant on same block (zero-sum exchange)
        // Which ant is exchanging should be random for now

        // Ants action 'dig' -> if on top of block and block is not container block, 'dig' up block (fall to block below??), and remove block from the map
        // Ants standing on AcidBlock have health decrease rate multiplied by 2
        // If ant ontop of mulch block -> consume mulch block and remove from world -> maybe to in world manager instead?
        // Make sure that only one of the ants on the block can consume it-> definitely sounds like maybe should be in world manager...

        // Ant action 'move' -> random direction (up/down/right/left) for now, but can only move to a block that is within 2 units of height difference

        // Queen ant -> produces nest blocks -> separate script? -> probably, yes. So tags can be used...
        // Producing nest block consumes 1/3 of queen's max health
        // Probably want health exchanges to mostly be with queen...

        // Can create new ants every generation but not increase the current generation at all (the 'evaluation' phase?)

        #endregion Methods
    }
