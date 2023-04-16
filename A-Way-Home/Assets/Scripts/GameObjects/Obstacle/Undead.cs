using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Undead : Obstacle, ITrap, IActionWaitProcess, ILightning
{
    [SerializeField] private Animator animator;
    [SerializeField] private int travelSpeed; 
    [SerializeField] private bool canPhase; 
    [SerializeField] private bool canRevive; 
    [SerializeField] private int deathTimer;
    private int maxHitpoints;
    private List<Node> path;
    private Node currentTargetNode;
    private int currentTargetIndex;

    public override bool isBurnable => true;
    public override bool isFragile => !canPhase;
    public override bool isMeltable => !canPhase;
    public override bool isCorrosive => !canPhase;

    private int  xPositionDiff => (int)(currentTargetNode.worldPosition.x - this.worldPos.x);
    private int  yPositionDiff => (int)(currentTargetNode.worldPosition.y - this.worldPos.y);
    private bool canMove => currentTargetIndex < travelSpeed;
    private bool isMoving {get => animator.GetBool("isMoving"); set => animator.SetBool("isMoving", value); }
    private bool isImmobile => hitpoints <= 0;


    protected override void Initialize()
    {
        base.Initialize();
        maxHitpoints = hitpoints;
        SetNodes(this.worldPos, canPhase ? NodeType.Walkable : NodeType.Obstacle, this);
        // currentNodePos = this.nodes[0].worldPosition;
        TryGetPath();
    }

    protected override void OnHighlight(Tool tool)
    {
        if(canPhase)
            return;
        base.OnHighlight(tool);
    }

    public void OnLightningHit()
    {
        if(canPhase || isImmobile)
            return;
        Damage(1);
    }

    public void OnTrapTrigger(Character character)
    {
        character.TriggerDeath();
    }

    public void OnPlayerAction()
    {
        SetPath();
        StartCoroutine(FollowPath());
    }

    private bool TryGetPath()
    {
        List<Vector3> targetPositions = new List<Vector3>();
        targetPositions.Add(Character.instance.currentPosition);
        path = !canPhase ? Pathfinding.FindPath(this.worldPos, targetPositions) : Pathfinding.FindPathPhased(this.worldPos, targetPositions, NodeGrid.Instance.grid);
        return path.Count > 0;
    }

    private void SetPath()
    {
        if(isImmobile && canRevive)
        {
            if(deathTimer <= 0)
                Mobilized();
            else
                deathTimer--;
            PlayerActions.FinishProcess(this);
            return;
        }
        if(TryGetPath() && !isImmobile)
        {
            ForceDehighlight();
            ClearNodes();
            isMoving = true;
            currentTargetIndex = 0;
            currentTargetNode = path[0];
            Node endNode = path[travelSpeed - 1];
            return;
        }
        PlayerActions.FinishProcess(this);
    } 

    private IEnumerator FollowPath()
    {
        while(isMoving)
        {
            if(this.transform.position == currentTargetNode.worldPosition)
            {
                currentTargetIndex ++;
                if(!canPhase)
                {
                    if(currentTargetNode.hasObstacle && currentTargetNode.GetObstacle().isTrampleable)
                        Destroy(currentTargetNode.GetObstacle());
                    else if(currentTargetNode.IsObstacle(typeof(GroundSpike)) || currentTargetNode.IsObstacle(typeof(PoisonMiasma)) || currentTargetNode.IsObstacle(typeof(FireField)))
                    {
                        isMoving = false;
                        currentTargetNode.GetObstacle().Destroy(this);
                        yield break;
                    }
                }
                if(Character.instance.isDead || !canMove)
                {
                    isMoving = false;
                    if(canPhase && currentTargetNode.IsType(NodeType.Obstacle) && currentTargetNode.hasObstacle)
                    {
                        PlayerActions.FinishProcess(this);
                        yield break;
                    }
                    OnStop();
                    PlayerActions.FinishProcess(this);
                    yield break;
                }
                Debug.Assert(path.Count > currentTargetIndex, $"ERROR: Tried to access index {currentTargetIndex} with path of size {path.Count}");
                currentTargetNode = path[currentTargetIndex];
            }
            UpdateAnimation();
            this.transform.position = Vector3.MoveTowards(this.transform.position, currentTargetNode.worldPosition, 5f * Time.deltaTime);
            yield return null;
        }
    }


    private void OnStop()
    {
        if(currentTargetNode.hasObstacle)
        {
            if(currentTargetNode.IsObstacle(typeof(Bat)))
            {
                Bat bat = currentTargetNode.GetObstacle() as Bat;
                bat.Move();
            }
            if(canPhase) return;
        }
        Debug.Assert(!currentTargetNode.hasObstacle || !canPhase, "ERROR: Node still has an obstacle");
        SetNodes(currentTargetNode.worldPosition, canPhase ? NodeType.Walkable : NodeType.Obstacle, this);
    }

    private void UpdateAnimation()
    {
        // Debug.Log($"Pos: {xPositionDiff}, {yPositionDiff}");
        if(xPositionDiff > 0 && yPositionDiff == 0)
            animator.Play("Right");
        else if(xPositionDiff < 0 && yPositionDiff == 0)
            animator.Play("Left");
        else if(xPositionDiff == 0 && yPositionDiff > 0)
            animator.Play("Backward");
        else if(xPositionDiff == 0 && yPositionDiff < 0)
            animator.Play("Forward");
    }

    public override void Damage(int damage)
    {
        if(canPhase)
            return;
        hitpoints -= damage;
        Debug.Log($"hitpoints:{hitpoints}");
        if(hitpoints <= 0 )
            Remove(false, false);
    }

    public override void Remove()
    {
        TriggerDeath(true);
    }

    public void Remove(bool forceClear = true, bool killPhasers = false)
    {
        TriggerDeath(forceClear, killPhasers);
    }

    private void TriggerDeath(bool forceClear = false, bool killPhasers = false)
    {
        // Debug.Assert(false, "ERROR: UNIMPLEMENTED");
        if(!killPhasers && canPhase)
            return;
        if(canRevive)
            Immobilized(forceClear);
        else
            StartCoroutine(PlayDeathAnimation());
    }

    private void Mobilized()
    {
        hitpoints = maxHitpoints;
        // animator.Play("Revive");
        SetNodes(this.worldPos, NodeType.Obstacle, this);
    }

    private void Immobilized(bool forceClear)
    {
        hitpoints = 0;
        animator.Play("Death");
        if(forceClear)
            Remove();
        else
            SetNodes(this.worldPos, NodeType.Walkable, this);
    }

    private IEnumerator PlayDeathAnimation()
    {
        ClearNodes();
        animator.Play("Death");
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length);
        this.gameObject.SetActive(false);
    }



}
