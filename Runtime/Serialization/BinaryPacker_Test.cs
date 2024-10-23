using System.Collections.Generic;
using UnityEngine;
using Multisynq;

public class WithNetId: IWithNetId {
  static public uint currNetId = 1;
  public uint netId { get; set; }
  public WithNetId() {
    netId = currNetId++;
  }
}

// Example usage
public class PlayerData : WithNetId {
  public int health;
  public bool boop;
  public string name;
  public Vector3 position;
  public EnemyData targetedEnemy;
}

public enum EnemyType : byte { Minion, Boss }

public class EnemyData : WithNetId {
  public EnemyType enemyType;
  public Vector3 spawnPoint;
  public Quaternion spawnRot;
  public int life;
  public int armCount = 2;
  public Color32 color = new Color32(0, 255, 0, 255);
  public List<PlayerData> targets;  // New field for targets
}

// Usage example
public class BinaryPacker_Test: MonoBehaviour {
  private BinaryPacker packer = new();
  float timer = 3f;
  void Start() {
    packer.CacheTypePacker(typeof(PlayerData));
    packer.CacheTypePacker(typeof(EnemyData));
  }
  public void Update() {
    if (timer > 0) {
      timer -= Time.deltaTime;
      if (timer <= 0) {
        Test();
      }
    }
  }

  void Test() {
    
    // Example usage
    EnemyData enemy = new EnemyData { 
      enemyType = EnemyType.Boss, 
      spawnPoint = new Vector3(10.0333331f, 0.0033302f, 10.00033333f),
      life = 1000,
      targets = new List<PlayerData>()  // Initialize the targets list
    };

    PlayerData player1 = new PlayerData { 
      health = 100, 
      name = "Player1", 
      position = new Vector3(1.0033304f, 2.0033305f, 3.0033333306f), 
      targetedEnemy = enemy,
      boop=true
    };

    PlayerData player2 = new PlayerData { 
      health = 90, 
      name = "Player2", 
      position = new Vector3(4.0007f, 5.0008f, 6.0009f), 
      targetedEnemy = enemy,
      boop=false
    };

    // Add players to enemy's targets
    enemy.targets.Add(player1);
    enemy.targets.Add(player2);

    // Serialize and deserialize player1
    byte[] playerBytes = packer.ObjAsBytes(player1);
    PlayerData unpackedPlayer = (PlayerData)packer.Unpack(playerBytes);
    
    string playerStr = packer.AsString(unpackedPlayer);
    Debug.Log($"<color=#44ff44> ===BinarySerializer_Test=== </color>\n{playerStr}");
    Debug.Log($"<color=#44ff44> ===BinarySerializer_Test=== </color>\n{playerStr}");
    Debug.Log($"<color=#44ff44> ===BinarySerializer_Test=== </color>\n{playerStr}");
    Debug.Log($"Lengths: playerBytes[{playerBytes.Length}] playerStr[{playerStr.Length}]");
    Debug.Log($"<color=#44ffff>===playerStr===</color>\n{playerStr}");

    // Optionally, you can also serialize and deserialize the enemy to see the targets
    byte[] enemyBytes = packer.ObjAsBytes(enemy);
    EnemyData deserializedEnemy = (EnemyData)packer.Unpack(enemyBytes);
    
    string enemyStr = packer.AsString(deserializedEnemy);
    Debug.Log($"Lengths: enemyBytes[{enemyBytes.Length}] enemyStr[{enemyStr.Length}]");
    Debug.Log($"<color=#44ffff>===enemyStr===</color>\n{enemyStr}");
  }
}

