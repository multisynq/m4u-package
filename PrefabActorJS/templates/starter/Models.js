// Starter Template Models

// Every object in Worldcore is represented by an actor/pawn pair. Spawning an actor
// automatically instantiates a corresponding pawn. The actor is replicated
// across all clients, while the pawn is unique to each client. In Unity, the pawn is
// a Unity object, usually generated from an instrumented prefab.

import { GameModelRoot } from "@croquet/game-models";
// START - AUTO INSERT imports  ### Do not edit code in Auto-Insert sections
import { KeyMovedActor } from "./KeyMovedActor";
// END   - AUTO INSERT imports

//------------------------------------------------------------------------------------------
//-- MyModelRoot ---------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

// The model root has an init as well. It creates a single child actor. When you create an
// actor you can pass it an options object. Here we give the actor an initial translation [0,0,0],
// and tell it which pawn to use.

export class MyModelRoot extends GameModelRoot {

  init(options) {
    super.init(options);
    console.log("Start model root!");
    this.test = KeyMovedActor.create({ translation: [0, 0, 0] });
    // START - AUTO INSERT MyModelRoot.init()   ### Do not edit code in Auto-Insert sections
    // END   - AUTO INSERT MyModelRoot.init()
  }

}
MyModelRoot.register("MyModelRoot"); // All Worldcore actors must be registered after they're defined.


