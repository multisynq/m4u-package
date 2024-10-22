using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Text;
using UnityEngine;
using Multisynq;


public class BinaryPacker {

  public delegate void     PackAction(BinaryWriter writer, object obj, BinaryPacker packer);
  public delegate void   UnpackAction(BinaryReader reader, object obj, BinaryPacker packer);
  public delegate string AsStringFunc(object obj, BinaryPacker packer, int indent);

  private Dictionary<Type, List<PackAction>>     packerCache = new();
  private Dictionary<Type, List<UnpackAction>> unpackerCache = new();
  private Dictionary<Type, List<AsStringFunc>> asStringCache = new();

  private HashSet<uint>                        packedObjects = new();
  private Dictionary<uint, IWithNetId>       unpackedObjects = new();
  private HashSet<uint>                   stringifiedObjects = new();


  public void Awake() {
    CachePackers(typeof(PlayerData), typeof(EnemyData)); // Add all your serializable types here
  }

  public void CachePackers(params Type[] types) {
    foreach (var type in types) CacheTypePacker(type);
  }

  private void CacheTypePacker(Type type) {
    var packerList   = new List<PackAction>();
    var unpackerList = new List<UnpackAction>();
    var stringerList = new List<AsStringFunc>();

    foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) {
      CacheFieldPacker(field, packerList, unpackerList, stringerList);
    }

    packerCache[type]   = packerList;
    unpackerCache[type] = unpackerList;
    asStringCache[type] = stringerList;
  }

  private void CacheFieldPacker(FieldInfo field, 
    List<PackAction>   packerList, 
    List<UnpackAction> unpackerList, 
    List<AsStringFunc> stringerList) {

    void Add3(PackAction s, UnpackAction d, AsStringFunc f, bool beFirst=false) {
      if (beFirst) {
        packerList.Insert(  0, s);
        unpackerList.Insert(0, d);
        stringerList.Insert(0, f);
      } else {
        packerList.Add(  s);
        unpackerList.Add(d);
        stringerList.Add(f);
      }
    }

    if (field.FieldType      == typeof(int)) {
      Add3( (writer, obj, packer) => writer.Write((int)field.GetValue(obj)),
            (reader, obj, packer) => field.SetValue(obj, reader.ReadInt32()),
            (obj, packer, indent) => $"\"{field.Name}\": {field.GetValue(obj)}" );
    }
    else if (field.FieldType == typeof(uint)) {
      bool beFirst = field.Name == "netId";
      Add3( (writer, obj, packer) => writer.Write((uint)field.GetValue(obj)),
            (reader, obj, packer) => field.SetValue(obj, reader.ReadUInt32()),
            (obj, packer, indent) => $"\"{field.Name}\": {field.GetValue(obj)}",
      beFirst );
    }
    else if (field.FieldType == typeof(bool)) {
      Add3( (writer, obj, packer) => writer.Write(Convert.ToByte(field.GetValue(obj))),
            (reader, obj, packer) => field.SetValue(obj, reader.ReadByte()!=0),
            (obj, packer, indent) => $"\"{field.Name}\": {field.GetValue(obj)}" );
    }
    else if (field.FieldType == typeof(byte)) {
      Add3( (writer, obj, packer) => writer.Write(Convert.ToByte(field.GetValue(obj))),
            (reader, obj, packer) => field.SetValue(obj, reader.ReadByte()),
            (obj, packer, indent) => $"\"{field.Name}\": {field.GetValue(obj)}" );
    }
    else if (field.FieldType == typeof(float)) {
      Add3( (writer, obj, packer) => writer.Write((float)field.GetValue(obj)),
            (reader, obj, packer) => field.SetValue(obj, reader.ReadSingle()),
            (obj, packer, indent) => $"\"{field.Name}\": {field.GetValue(obj)}" );
    }
    else if (field.FieldType == typeof(string)) {
      Add3( (writer, obj, packer) => WriteCompressedString(writer, (string)field.GetValue(obj)),
            (reader, obj, packer) => field.SetValue(obj, ReadCompressedString(reader)),
            (obj, packer, indent) => $"\"{field.Name}\": \"{field.GetValue(obj)}\"" );
    }
    else if (field.FieldType == typeof(Vector3)) {
      Add3( (writer, obj, packer) => WriteVector3(writer, (Vector3)field.GetValue(obj)),
            (reader, obj, packer) => field.SetValue(obj, ReadVector3(reader)),
            (obj, packer, indent) => $"\"{field.Name}\": {field.GetValue(obj)}" );
    }
    else if (field.FieldType.IsEnum) {
      Add3( (writer, obj, packer) => writer.Write(Convert.ToByte(field.GetValue(obj))),
            (reader, obj, packer) => field.SetValue(obj, Enum.ToObject(field.FieldType, reader.ReadByte())),
            (obj, packer, indent) => $"\"{field.Name}\": \"{field.GetValue(obj)}\"" );
    }
    else if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>)) {
      Type elementType = field.FieldType.GetGenericArguments()[0];
      packerList.Add((writer, obj, packer) =>  {
        var list = (System.Collections.IList)field.GetValue(obj);
        writer.Write(list.Count);
        foreach (var item in list) packer.PackObj(writer, item);
      });
      unpackerList.Add((reader, obj, packer) =>  {
        int count = reader.ReadInt32();
        var list = (System.Collections.IList)Activator.CreateInstance(field.FieldType);
        for (int i = 0; i < count; i++) {
          list.Add(packer.UnpackObj(reader));
        }
        field.SetValue(obj, list);
      });
      stringerList.Add((obj, packer, indent) =>  {
        var list = (System.Collections.IList)field.GetValue(obj);
        StringBuilder sb = new StringBuilder();
        sb.Append($"\"{field.Name}\": [");
        for (int i = 0; i < list.Count; i++) {
          if (i > 0) sb.Append(",");
          sb.Append("\n");
          sb.Append(new string(' ', indent + 2));
          sb.Append(packer.AsStringInternal(list[i], indent + 2, false));
        }
        if (list.Count > 0) sb.Append("\n" + new string(' ', indent));
        sb.Append("]");
        return sb.ToString();
      });
    }
    else if (typeof(IWithNetId).IsAssignableFrom(field.FieldType)) {
      packerList.Add(  (writer, obj, packer) => packer.PackNetIdObj(writer, (IWithNetId)field.GetValue(obj)));
      unpackerList.Add((reader, obj, packer) => field.SetValue(obj, packer.UnpackNetIdObj(reader)));
      stringerList.Add((obj, packer, indent) =>  {
        var value = (IWithNetId)field.GetValue(obj);
        return $"\"{field.Name}\": {packer.AsStringInternal(value, indent, false)}";
      });
    }
    else if (field.FieldType.IsClass || field.FieldType.IsValueType) {
      // for any struct or class type, recursively cache its packers
      CacheTypePacker(field.FieldType);
      packerList.Add(  (writer, obj, packer) => packer.PackObj(writer, field.GetValue(obj)));
      unpackerList.Add((reader, obj, packer) => field.SetValue(obj, packer.UnpackObj(reader)));
      stringerList.Add((obj, packer, indent) =>  {
        return $"\"{field.Name}\": {packer.AsStringInternal(field.GetValue(obj), indent, false)}";
      });
    }
  }

  public byte[] ObjAsBytes(object obj) {
    packedObjects.Clear();
    using (MemoryStream ms = new MemoryStream())
    using (BinaryWriter writer = new BinaryWriter(ms)) {
      PackObj(writer, obj);
      return ms.ToArray();
    }
  }

  private void PackObj(BinaryWriter writer, object obj) {
    if (obj == null) {
      writer.Write(false);
      return;
    }
    writer.Write(true);

    Type type = obj.GetType();
    writer.Write(type.AssemblyQualifiedName);

    if (obj is IWithNetId IWithNetId) {
      writer.Write(IWithNetId.netId);
      if (packedObjects.Contains(IWithNetId.netId)) return; // Object already serialized
      packedObjects.Add(IWithNetId.netId);
    }

    if (!packerCache.TryGetValue(type, out var packerList)) {
      CacheTypePacker(type);
      packerList = packerCache[type];
    }

    foreach (var packAction in packerList) {
      packAction(writer, obj, this);
    }
  }

  public object Unpack(byte[] data) {
    unpackedObjects.Clear();
    using (MemoryStream ms = new MemoryStream(data))
    using (BinaryReader reader = new BinaryReader(ms)) return UnpackObj(reader);
  }

  private object UnpackObj(BinaryReader reader) {
    if (!reader.ReadBoolean()) return null;

    string typeName = reader.ReadString();
    Type type = Type.GetType(typeName);
    object obj = Activator.CreateInstance(type);

    if (obj is IWithNetId IWithNetId) {
      IWithNetId.netId = reader.ReadUInt32();
      if (unpackedObjects.TryGetValue(IWithNetId.netId, out IWithNetId existingObj)) return existingObj;
      unpackedObjects[IWithNetId.netId] = IWithNetId;
    }

    if (!unpackerCache.TryGetValue(type, out var unpackerList)) {
      CacheTypePacker(type);
      unpackerList = unpackerCache[type];
    }

    foreach (var deserializeAction in unpackerList) {
      deserializeAction(reader, obj, this);
    }

    return obj;
  }

  private void PackNetIdObj(BinaryWriter writer, IWithNetId obj) {
    if (obj == null) {
      writer.Write((uint)0);
      return;
    }
    writer.Write(obj.netId);
    if (!packedObjects.Contains(obj.netId)) PackObj(writer, obj);
  }

  private IWithNetId UnpackNetIdObj(BinaryReader reader) {
    uint netId = reader.ReadUInt32();
    if (netId == 0) return null;
    if (unpackedObjects.TryGetValue(netId, out IWithNetId obj)) return obj;
    return (IWithNetId)UnpackObj(reader);
  }

  public string AsString(object obj, int indentLevel = 0) {
    return AsStringInternal(obj, indentLevel, true);
  }

  private string AsStringInternal(object obj, int indentLevel, bool isTopLevel) {
    if (obj == null) return "null";

    Type type = obj.GetType();
    if (!asStringCache.TryGetValue(type, out var asStringList)) {
      CacheTypePacker(type);
      asStringList = asStringCache[type];
    }

    StringBuilder sb = new StringBuilder();
    string indent = new string(' ', indentLevel);
    string innerIndent = new string(' ', indentLevel + 2);

    if (obj is IWithNetId IWithNetId) {
      if (stringifiedObjects.Contains(IWithNetId.netId)) {
        return $"{{ \"netId\": {IWithNetId.netId}, \"isReference\":true }}";
      }
      stringifiedObjects.Add(IWithNetId.netId);
    }

    sb.Append("{");    
    bool isFirst = true;
    foreach (var asStringFunc in asStringList) {
      string fieldString = asStringFunc(obj, this, indentLevel + 2);
      if (!isFirst) sb.Append(",");
      sb.Append($"\n{innerIndent}{fieldString}");
      isFirst = false;
    }

    sb.Append($"\n{indent}}}");

    if (obj is IWithNetId) stringifiedObjects.Remove(((IWithNetId)obj).netId);

    return sb.ToString();
  }


  private static void Write7BitEncodedInt(BinaryWriter writer, int value) {
    uint v = (uint)value;
    while (v >= 128) {
      writer.Write((byte)(v | 128));
      v >>= 7;
    }
    writer.Write((byte)v);
  }

  private static int Read7BitEncodedInt(BinaryReader reader) {
    int count = 0;
    int shift = 0;
    byte b;
    do {
      if (shift == 5 * 7)  // 5 bytes max
        throw new FormatException("Invalid 7-bit encoded int");
      b = reader.ReadByte();
      count |= (b & 127) << shift;
      shift += 7;
    } while ((b & 128) != 0);
    return count;
  }
  private static void WriteCompressedString(BinaryWriter writer, string value) {
    byte[] bytes = Encoding.UTF8.GetBytes(value);
    Write7BitEncodedInt(writer, bytes.Length);
    writer.Write(bytes);
  }

  private static string ReadCompressedString(BinaryReader reader) {
    int length = Read7BitEncodedInt(reader);
    byte[] bytes = reader.ReadBytes(length);
    return Encoding.UTF8.GetString(bytes);
  }

  private static void WriteVector3(BinaryWriter writer, Vector3 vector) {
    writer.Write(vector.x);
    writer.Write(vector.y);
    writer.Write(vector.z);
  }

  private static Vector3 ReadVector3(BinaryReader reader) {
    return new Vector3(
      reader.ReadSingle(),
      reader.ReadSingle(),
      reader.ReadSingle()
    );
  }
  
  private static void WriteCompressedVector3(BinaryWriter writer, Vector3 vector) {
    writer.Write((int)(vector.x * 10000));
    writer.Write((int)(vector.y * 10000));
    writer.Write((int)(vector.z * 10000));
  }
  private static Vector3 ReadCompressedVector3(BinaryReader reader) {
    return new Vector3(
      reader.ReadInt32() / 10000f,
      reader.ReadInt32() / 10000f,
      reader.ReadInt32() / 10000f
    );
  }
}
