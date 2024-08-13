import { GameActor } from "./GameActor";

//------------------------------------------------------------------------------------------
// KeyMovedActor -------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

// Here we define a new actor. Actors and pawns can be extended with mixins to give them
// new methods and properties. TestActor is extended by AM_Spatial to give it a position
// in 3D space.

// The init method executes when the actor is created. In KeyMovingActor's init we create 
// subscriptions to listen for keyboard events. 
// When any user presses W,A,S or D, the actor moves in the corresponding direction.

export class KeyMovedActor extends GameActor {

  init(options) {
    super.init(options);
    this.subscribe("input", "wDown", this.moveFwd);
    this.subscribe("input", "aDown", this.moveLeft);
    this.subscribe("input", "sDown", this.moveBack);
    this.subscribe("input", "dDown", this.moveRight);
  }

  moveFwd()   { move(   0,  0,  0.1); }
  moveBack()  { move(   0,  0, -0.1); }
  moveLeft()  { move(-0.1,  0,    0); }
  moveRight() { move( 0.1,  0,    0); }

  move(x, y, z) {
    const translation = this.translation;
    if (x) translation[0] += x;
    if (y) translation[1] += y;
    if (z) translation[2] += z;
    this.set({ translation });
  }
  
}
KeyMovedActor.register('KeyMovedActor'); // All Worldcore actors must be registered after they're defined.
