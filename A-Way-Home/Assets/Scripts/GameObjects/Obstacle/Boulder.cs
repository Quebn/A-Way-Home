using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Boulder : Obstacle, IInteractable
{

    [SerializeField] private Animator animator;
    private int hitpoints = 4;

    protected override void Initialize()
    {
        base.Initialize();
        SetNodes(this.worldPos, NodeType.Obstacle, this);
    //     animator = GetComponent<Animator>();
    //     spriteRenderer = GetComponent<SpriteRenderer>();
    //     InitializeNodes(this.transform.position);
    //     SetNodesType(NodeType.Obstacle, this);
    //     hitpoints = 4;
    }

    public void OnDehighlight()
    {
        if(currentTool != Tool.Tremor && currentTool != Tool.Lightning)
            return;
        spriteRenderer.color = Color.white;
    }

    public void OnHighlight()
    {
        if(currentTool != Tool.Tremor && currentTool != Tool.Lightning)
            return;
        spriteRenderer.color = Color.green;

    }

    public void OnInteract()
    {
        if(currentTool != Tool.Tremor && currentTool != Tool.Lightning)
            return;
        if(hitpoints > 0)
            hitpoints--;
        Debug.Log("Hit Boulder");
        if(hitpoints > 0)
            return;
        animator.Play("BigBoulder_Destroy");
        float delay = animator.GetCurrentAnimatorStateInfo(0).length;
        Invoke("OnDestroy", delay);
    }

    private void OnDestroy()
    {
        ClearNodes();
        this.gameObject.SetActive(false);
    }

}