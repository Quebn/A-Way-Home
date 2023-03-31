using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireField : Spawnable, ITrap
{

    protected override void OnSpawn()
    {
        DestroyNodeObstacle();
        base.OnSpawn();
        SetNodes(this.worldPos, NodeType.Walkable, this);
    }

    public void OnTrapTrigger(Character character)
    {
        // add burning effect on character;
    }

    public override void Remove()
    {
        if(GameData.levelData.obstacles.ContainsKey(this.id))
            GameData.levelData.obstacles.Remove(id);
        ClearNodes();
        Debug.Log("Fire Field Cleared");
        GameObject.Destroy(this.gameObject);
    }

}
