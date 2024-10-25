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
      SynqVar_Mgr.I.SendAsMsg(varIdx, varId, val, typeof(T));
      _value = val;
      return _value;
    }
    public T Get() { return _value; }
    // auto-getter
    public static implicit operator T(SynqVar<T> synqVar) { return synqVar._value; }

    // constructor
    public SynqVar( SynqBehaviour sb, string name, T initialValue ) {
      syncedBehaviour = sb;
      varName = name;
      _value = initialValue;
      // if (SynqBehaviour.currentlyConstructingSynqBehaviour == null) {
      //   Debug.LogError("SynqVar created outside of SynqBehaviour initialization!");
      //   return;
      // }

      // this.syncedBehaviour = SynqBehaviour.currentlyConstructingSynqBehaviour;
      // _value = initialValue;

      // // Find this instance's field name in the containing class
      // var fields = syncedBehaviour.GetType()
      //   .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

      // foreach (var field in fields) {
      //   if (field.FieldType == typeof(SynqVar<T>)) {
      //     // Get the actual instance from the field
      //     var fieldValue = field.GetValue(syncedBehaviour);
      //     // Compare references to find ourselves
      //     if (ReferenceEquals(fieldValue, this)) {
      //       varName = field.Name;
      //       break;
      //     }
      //   }
      // }
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