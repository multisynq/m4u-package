//------------------------------------------------------------------------------------------
//-- GameActor -----------------------------------------------------------------------------
//------------------------------------------------------------------------------------------
// Every actor that needs a representation in the game engine should inherit
// from GameActor.
export class GameActor extends mix(Actor).with(AM_Spatial) {

  get pawn() { return 'GamePawn' } // if not otherwise specialised
  get gamePawnType() { return this._type } // Unity prefab to use
  get type() { return this._type || "primitiveCube" }
  get color() { return this._color || [0.5, 0.5, 0.5] }
  get alpha() { return this._alpha === undefined ? 1 : this._alpha }
}
GameActor.register('GameActor');
