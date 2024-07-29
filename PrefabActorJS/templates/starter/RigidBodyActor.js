//------------------------------------------------------------------------------------------
//-- RigidBodyActor ------------------------------------------------------------------------
//------------------------------------------------------------------------------------------
// Some Actors also need a RigidBody. This is a mixin that adds a RigidBody to the actor.
export class RigidBodyActor extends mix(GameActor).with(AM_RapierRigidBody) {
  static okayToIgnore() { return [...super.okayToIgnore(), '$rigidBody'] } // reduces the warnings from snapshotting due to $ prefix
}
GameActor.register('RigidBodyActor');