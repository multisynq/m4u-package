using System;
using UnityEngine;

namespace Multisynq {


[AddComponentMenu("Multisynq/EntityComponent")]
public class Mq_Entity_Comp : Mq_Comp
{
    public override Mq_System croquetSystem { get; set; } = Mq_Entity_System.Instance;

    public string croquetActorId = ""; // the actor identifier (M###)
    public int croquetHandle = -1; // unique integer ID assigned by this client's bridge
    // specify the accompanying actor class here
    // specify addressable name / pawn name

    // static and watched properties from the Croquet actor (as requested on a
    // CroquetActorManifest script) are held here, and accessible using static
    // methods on the Croquet class:
    //   ReadActorString(prop)
    //   ReadActorStringArray(prop)
    //   ReadActorFloat(prop)
    //   ReadActorFloatArray(prop)
    public StringStringSerializableDict actorProperties = new StringStringSerializableDict();


}

[Serializable]
public class StringStringSerializableDict : SerializableDictionary<string, string> { }

}