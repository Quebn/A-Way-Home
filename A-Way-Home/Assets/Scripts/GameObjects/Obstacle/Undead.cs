using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Undead : Obstacle, ITrap, IActionWaitProcess, ILightning, ISelectable, ICommand
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
    private Node targetNode;
    private bool isCommanded = false;

    public override bool isBurnable => true;
    public override bool isFragile => !canPhase;
    public override bool isMeltable => !canPhase;
    public override bool isCorrosive => !canPhase;

    private int  xPositionDiff => (int)(currentTargetNode.worldPosition.x - this.worldPos.x);
    private int  yPositionDiff => (int)(currentTargetNode.worldPosition.y - this.worldPos.y);
    private bool canMove => currentTargetIndex < travelSpeed;
    private bool isMoving {get => animator.GetBool("isMoving"); set => animator.SetBool("isMoving", value); }
    private bool isImmobile => hitpoints <= 0;
    private bool hasPath => path.Count > 0;

    private static List<Node> gridNodes;

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
        if(isImmobile && canRevive)
        {
            if(deathTimer <= 0)
                Mobilized();
            else
                deathTimer--;
            PlayerActions.FinishProcess(this);
            return;
        }
        if(TryGetPath() && !isImmobile && !isCommanded)
        {
            ForceDehighlight();
            isMoving = true;
            currentTargetIndex = 0;
            currentTargetNode = path[0];
            ClearNodes();
            StartCoroutine(FollowPath());
            return;
        }
        if(isCommanded)
            isCommanded = false;
        PlayerActions.FinishProcess(this);
    }

    public bool OnCommand(List<Node> nodes)
    {
        if(nodes.Count == 0)
            return false;
        return GoToNode(nodes[0]);
    }

    public void OnSelect(Tool tool)
    {
        if(tool != Tool.Command)
            return;
        gridNodes = GetWalkableNodes();
        for(int i = 0 ; i < gridNodes.Count; i++)
            gridNodes[i].RevealNode();
    }

    public List<Node> OnSelectedHover(Vector3 mouseWorldPos, List<Node> currentNodes)
    {
        Vector2 origin = NodeGrid.GetMiddle(mouseWorldPos);
        Node node = NodeGrid.NodeWorldPointPos(origin);
        Debug.Assert(!gridNodes.Contains(nodes[0]));
        if(node == currentNodes[0])
            return currentNodes;
        List<Node> nodeList = new List<Node>();
        DehighlightNode(currentNodes[0]);
        if(gridNodes.Contains(node))
            node.HighlightObstacle(Node.colorRed, Tool.Inspect);
        nodeList.Add(node);
        return nodeList;
    }

    public void OnDeselect()
    {
        Node.ToggleNodes(gridNodes, NodeGrid.nodesVisibility);
        if(nodes.Count > 0)
            nodes[0].Dehighlight();
    }

    public List<Node> IgnoredToggledNodes()
    {
        List<Node> list = new List<Node>(gridNodes);
        list.Add(nodes[0]);
        return list;
    }

    private void DehighlightNode(Node node)
    {
        if(!gridNodes.Contains(node))
            return;
        node.RevealNode();
        if(!node.hasObstacle)
            return;
        node.GetObstacle().Dehighlight();
    }

    private static List<Node> GetWalkableNodes()
    {
        List<Node> nodes = new List<Node>();
        foreach(Node node in NodeGrid.Instance.grid.Values)
            if(node.IsWalkable())
                nodes.Add(node);
        return nodes;
    }

    private bool GoToNode(Node node)
    {
        List<Vector3> targetPositions = new List<Vector3>();
        if(!node.IsWalkable() || !NodeGrid.Instance.grid.ContainsValue(node))
            return false;
        targetPositions.Add(node.worldPosition);
        Debug.Assert(targetPositions.Count == 1, "ERROR: Crab target positions more than 1.");
        if(TryGetPath(targetPositions))
        {
            ForceDehighlight();
            isMoving = true;
            currentTargetIndex = 0;
            currentTargetNode = path[0];
            targetNode = node;
            ClearNodes();
            StartCoroutine(FollowPathCommand());
            isCommanded = true;
        }
        return hasPath;
    }

    private bool TryGetPath(List<Vector3> targetPositions)
    {
        path = !canPhase ? Pathfinding.FindPath(this.worldPos, targetPositions) : Pathfinding.FindPathPhased(this.worldPos, targetPositions, NodeGrid.Instance.grid);
        return hasPath;
    }


    private bool TryGetPath()
    {
        List<Vector3> targetPositions = new List<Vector3>();
        targetPositions.Add(Character.instance.currentPosition);
        path = !canPhase ? Pathfinding.FindPath(this.worldPos, targetPositions) : Pathfinding.FindPathPhased(this.worldPos, targetPositions, NodeGrid.Instance.grid);
        return hasPath;
    }


    private IEnumerator FollowPath()
    {
        while(isMoving)
        {
            if(this.transform.position == currentTargetNode.worldPosition)
            {
                currentTargetIndex ++;
                if(!canPhase && currentTargetNode.hasObstacle)
                {
                    if(currentTargetNode.GetObstacle().isTrampleable)
                        Destroy(currentTargetNode.GetObstacle());
                    else{
                        isMoving = false;
                        currentTargetNode.GetObstacle().Destroy(this);
                        PlayerActions.FinishProcess(this);
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

    private IEnumerator FollowPathCommand()
    {
        while(isMoving)
        {
            if(this.transform.position == currentTargetNode.worldPosition)
            {
                currentTargetIndex ++;
                if(!canPhase && currentTargetNode.hasObstacle)
                {
                    if(currentTargetNode.GetObstacle().isTrampleable)
                        Destroy(currentTargetNode.GetObstacle());
                    else{
                        isMoving = false;
                        currentTargetNode.GetObstacle().Destroy(this);
                        PlayerActions.FinishCommand();
                        yield break;
                    }
                }
                if(this.transform.position == targetNode.worldPosition)
                {
                    isMoving = false;
                    if(canPhase && currentTargetNode.IsType(NodeType.Obstacle) && currentTargetNode.hasObstacle)
                    {
                        PlayerActions.FinishCommand();
                        yield break;
                    }
                    OnStop();
                    PlayerActions.FinishCommand();
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
        if(currentTargetNode.hasObstacle && canPhase)
            return;
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
