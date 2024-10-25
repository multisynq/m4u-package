using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;
using System.Linq;

namespace Multisynq {

  //========================= ||||||| =================
  [Serializable] public class SynqVar<T> {
    public SynqBehaviour syncedBehaviour;
    public string varName;
    private T _value;

    public string varId;
    public int varIdx;

    public T Set(T val){
      // SynqVar_Mgr.I.SendAsMsg(varIdx, varId, val, typeof(T));
      _value = val;
      return _value;
    }
    public T Get() { return _value; }

    // this means you can do: int myInt = mySynqVar; or Debug.Log($"mySynqVar={mySynqVar}");
    public static implicit operator T(SynqVar<T> synqVar) { return synqVar._value; }

    // ToString() is called implicitly when you do: Debug.Log($"mySynqVar={mySynqVar}");
    public override string ToString() { return _value.ToString(); }

    // constructor
    public SynqVar( SynqBehaviour sb, string name, T initialValue ) {
      syncedBehaviour = sb;
      varName = name;
      _value = initialValue;
    }

    void Test() {
      // SynqVar<int> myInt = new SynqVar<int>(0);
      // int myIntValue = myInt;
      // int bloop = myInt.Get();
      // myInt.Set(42);
    }

    public object LastValue      { get; set; }
    public bool   ConfirmedInArr { get; set; }
    public float  LastSyncTime   { get; set; }
  }

} // END namespace Multisynq