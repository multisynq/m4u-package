using System.Collections.Generic;
using UnityEngine;

[SerializeField]
public class SyncVarPerPlayer<T> {
  
  string varId;
  Dictionary<string,T> values = new();

  public SyncVarPerPlayer(string _varId, T _myValue) {
    myValue = _myValue;
    varId = _varId;

    // subscribe to changes by other joined players in the session
  }

  public T myValue {
    get {
      return values[CroquetBridge.Instance.croquetViewId];
    }
    set {
      values[CroquetBridge.Instance.croquetViewId] = value;
    }
  }

  public T getValue( string playerId ) {
    return values[playerId];
  }

}