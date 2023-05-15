using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeCoconut : TreeObstacle, ILightning, IActionWaitProcess
{
    [SerializeField] private List<GameObject> logs;
    [SerializeField] private bool hasNut;
    private bool isFalling = false;

    public void OnLightningHit(int damage)
    {
        Damage(damage);
        isFalling = (hitpoints == 1);
    }

    public void OnPlayerAction()
    {
        if(!isFalling)
            PlayerActions.FinishProcess(this);
    }

    protected override void OnCutDown()
    {
        for(int i = 0 ; i < placeableNodes[currentCursorLocation].Count; i++)
        {
            Node node = placeableNodes[currentCursorLocation][i]; 
            if(LogNotPlaceable(node))
                continue;
            GameObject.Instantiate(
                hasNut && isFruitplaceable(node) 
                    ? logs[2] 
                    : node.currentType == NodeType.Water 
                        ? logs[1] 
                        : logs[0],
                node.worldPosition,
                Quaternion.identity
            );
        }
        isFalling = false;
        PlayerActions.FinishProcess(this);
    }
}
