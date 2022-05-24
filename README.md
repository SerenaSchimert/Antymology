# Assignment 3: Antymology

Made in Unity (Version: 2019.2.8f1)

Forked base code from: https://github.com/DaviesCooper/Antymology
- Environment/terrain is from this initial code base, but agent behaviour/genetics etc. is original code

![NestBuilding](https://github.com/SerenaSchimert/Antymology/blob/master/Images/NestBuilding.PNG)

A simulation of an ant colony formed through evolving generations (inheritance) with the goal of building the biggest nest.
The nest and other world materials are represented as differently colored blocks (red for nest, grey for stone, dark green for glass, light green for mulch, purple for acid, black for container, transparent for air). Queen ant is gold, regular ants are black.

Ants have depleting health that can be replenished by digging up and consuming mulch, and can only move to a block beside them (up, down, left, right) and within -2 to 2 blocks up or down. Only the single queen ant in each generation can place down nest blocks.

Camera controls are WASD to move and hold down middle mouse button while shifting mouse to pivot look direction (fly camera from original base code).

# Genes
- float eatPoint; // health level below which ant will consume mulch when on mulch block and without trapping itself <br />
- float probDig; // dig probability having already checked that digging wouldn't trap ant (walls low enough) <br />
- float stepsOffAcid; // number of steps off of acid once acid is encountered-> i.e. how far from acid to escape <br />
- float healthToQueen; // amount of queen's missing health to re-fill (i.e., full, half of what's missing etc.) <br />
- float probHealthtoAnt; // probability of giving health (always 25% of current health) to non-queen ant if other ant's health is < 50 and this ant's health is > 50 <br />
- float forwardBias; // relative bias to go forward if move is available instead of random selection of movements from available movesets <br />

# Inheritance
Measure of fitness: health given to the queen ant over the lifespan of this worker ant. This metric supports the queen in producing nest blocks, and ant must have survived half its generation dieing before being counted in the selection for fitness process.
The two ants with the highest fitness are used as the parents in creating the genes of the  next generation (the next generation is created when evolve is toggle true and either half the worker ants have died or the queen has died).
 The stages are (after first generation, which is randomly assigned genes):
   1) Evaluate fitness to find parents
   2) For each ant in the next generation
      - create a recombinant gene set from the parents
      - randomly mutate some portions of the genes
      - assign this gene set to a new ant

# Notes
Bugs are still present and genes are lacking somewhat in ability to increase encounters with the queen ant. Ants are also not trained (evolve is set to on in WorldManager, if you would like to set genes, turn this to false, than find where evolvedGenes are set and manually add a value, right  now those values are not set meaningfully again due to lack of ant training). The foundation, methods etc. are all there (note that all evolution/inheritance related methods are within WorldManager script), but the current execution needs work.
