using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;

public enum NodeType{ Walkable, Water, Terrain, Obstacle, Poisoned}
public class Node
{
    public Vector3 worldPosition;
    public Vector2Int gridPos;
    public Node parent;
    public int gCost;
    public List<int> hCosts;
    public bool isConductive;

    private bool isOpen;
    private NodeType currentNodeType;
    private Obstacle obstacle; 
    private Obstacle platform;

    private bool isInspectable => obstacle is IInspect || platform is IInspect;
    private bool isShockable => obstacle is ILightning || platform is ILightning;
    private bool isGrowable => obstacle is IGrow || platform is IGrow;
    private bool isCommandable => obstacle is ICommand || platform is ICommand;
    private bool isTremorable => obstacle is ITremor || platform is ITremor;
    public bool hasObstacle => obstacle != null;
    public bool hasPlatform => platform != null;
    public bool canLightning => isOpen || isConductive;
    public int hCost => MinHCost(); 
    public int fCost => gCost + hCost;
    public NodeType currentType {  
        get => this.currentNodeType; 
        set {this.currentNodeType = value; UpdateColor();}
    }

    public static Color colorCyan   => new Color32(0, 255, 255, 150);
    public static Color colorWhite  => new Color32(255, 255, 255, 100);
    public static Color colorRed    => new Color32(255, 0, 0, 150);
    public static Color colorGreen  => new Color32(0, 255, 0, 150);
    public static Color colorBlue   => new Color32(0, 0, 255, 150);
    public static Color colorYellow   => new Color32(255, 255, 0, 150);
    public static Color colorPurple => new Color32(166, 0, 255, 150);
    public static Color colorClear  => Color.clear; 
    
    public Node(NodeType nodeType, Vector3 worldpos, Vector2Int grid, bool isOpen)
    {
        this.worldPosition = worldpos;
        this.gridPos = grid;
        this.currentType = nodeType;
        this.hCosts = new List<int>();
        this.isOpen = isOpen;
        this.isConductive = isOpen;
    }

    private int MinHCost()
    {
        if (!hCosts.Any<int>())
            Debug.LogWarning($"Node[{gridPos.x}, {gridPos.y}] has no Contents");

            // return 0;
        return hCosts.Min<int>();
        // int hcost = hCosts.Min<int>();
        // Debug.LogWarning($"Node[{gridPos.x}, {gridPos.y}] => MIN hCost: {hcost}");
        // return hcost;
    }

    private void SetColor(Color color)
    {
        if(NodeGrid.tilemap == null)
            return;
        NodeGrid.tilemap.SetColor((Vector3Int)this.gridPos ,color);
    }

    public bool IsWalkable()
    {
        return currentNodeType == NodeType.Walkable;
    }

    public bool IsWalkable(NodeType nodeType, Type type)
    {
        return (type == null || obstacle == null) ? 
            currentNodeType == nodeType || IsWalkable() : 
            IsObstacle(type) || IsWalkable();
    }

    public bool Is(NodeType nodeType, Type obstacleType)
    {
        if(obstacleType == null)
            return currentNodeType == nodeType;
        return obstacle == null? false : currentNodeType == nodeType && IsObstacle(obstacleType);
    }

    public bool IsType(NodeType nodeType)
    {
        return currentNodeType == nodeType;
    }

    public bool CheckIf(NodeType nodeType, bool containsObs)
    {
        return containsObs ? 
            IsType(nodeType) && obstacle != null: 
            IsType(nodeType) && obstacle == null;
    }

    public bool IsObstacle(Type type)
    {
        return obstacle != null && type.IsAssignableFrom(obstacle.GetType());
    }

    public Obstacle GetObstacle(bool asPlatform = false)
    {
        return asPlatform ? platform : obstacle;
    }

    public Obstacle GetObstacleByTool(Tool tool)
    {
        if(!hasObstacle && !hasPlatform)
            return null;
        if(hasObstacle)
            return obstacle.SelectableBy(tool)? obstacle : null;
        return platform.SelectableBy(tool)? platform : null;
    }

    public Type GetObstacleType()
    {
        return obstacle.GetType();
    }

    private void UpdateColor()
    {
        if(!NodeGrid.nodesVisibility){
            HideNode();
            return;
        }
        RevealNode();
    }

    public void HideNode()
    {        
        if(NodeGrid.tilemap == null)
            return;
        SetColor(colorClear);
    }

    public void UpdateNodeColor()
    {
        UpdateColor();
    }

    public void RevealNode()
    {
        switch(currentNodeType)
        {
            case NodeType.Walkable:
                SetColor(Character.instance.NodeInPath(this) ? colorGreen : colorWhite);
                break;
            case NodeType.Terrain:
                HideNode();
                break;
            case NodeType.Water:
                SetColor(colorBlue);
                break;
            case NodeType.Obstacle:
                SetColor(colorRed);
                break;
        }
    }

    public void RevealNode(Color color)
    {
        if(NodeGrid.tilemap == null)
            return;
        SetColor(color);
    }

    public void ToggleNode(bool toggle)
    {
        if(toggle)
            RevealNode();
        else
            HideNode();
    }

    public void ToggleNode(Color color, bool toggle)
    {
        if(toggle)
            RevealNode(color);
        else
            HideNode();
    }

    public void Highlight(Color color, Tool tool)
    {
        if(tool == Tool.Inspect)
            ToggleNode(NodeGrid.nodesVisibility);
        else
            RevealNode(color);
        if(!hasObstacle && !hasPlatform)
            return;
        Obstacle obs = hasObstacle ? obstacle : platform; 
        obs.Highlight(tool);
    }

    public void HighlightObstacle(Color color, Tool tool)
    {
        RevealNode(color);
        if(!hasObstacle)
            return;
        obstacle.Highlight(tool);
    }

    public void Dehighlight()
    {
        ToggleNode(NodeGrid.nodesVisibility);
        if(!hasObstacle && !hasPlatform)
            return;
        Obstacle obs = hasObstacle ? obstacle : platform; 
        obs.Dehighlight();
    }
    public void SetObstacle(Obstacle obstacle, NodeType nodeType, bool isPlatform = false)
    {
        if(isPlatform)
            this.platform = obstacle;
        else
            this.obstacle = obstacle;
        this.currentNodeType = nodeType;
        UpdateColor();
    }

    public bool InspectObstacle()
    {
        if(!isInspectable)
            return false;
        IInspect interactable = ((hasObstacle) ? obstacle : platform ) as IInspect;
        interactable.OnInspect();
        return true;
    }
    
    private bool ShockObstacle()
    {
        if(!isShockable)
            return false;
        ILightning interactable = ((hasObstacle) ? obstacle : platform ) as ILightning;
        interactable.OnLightningHit();
        return true;
    }

    private bool ShockObstacleAfter(Vector2 lightningOrigin)
    {
        if(!isShockable)
            return false;
        ILightning interactable = ((hasObstacle) ? obstacle : platform ) as ILightning;
        if(interactable == null)
            return false;
        interactable.OnAftershock(lightningOrigin);
        return true;
    }

    public bool GrowObstacle()
    {
        if(!isGrowable)
            return false;
        IGrow interactable = ((hasObstacle) ? obstacle : platform ) as IGrow;
        interactable.OnGrow();
        return true;
    }

    public bool CheckTool(Tool tool)
    {
        if(!hasObstacle && !hasPlatform)
            return false;
        return hasObstacle ? obstacle.SelectableBy(tool) : platform.SelectableBy(tool);
    }

    public bool CommandObstacle(List<Node> nodes)
    {
        if(!isCommandable)
            return false;
        ICommand interactable = ((hasObstacle) ? obstacle : platform ) as ICommand;
        interactable.OnCommand(nodes);
        return true;
    }

    private bool TremorObstacle()
    {
        if(!isTremorable)
            return false;
        ITremor interactable = ((hasObstacle) ? obstacle : platform ) as ITremor;
        interactable.OnTremor();
        return true;
    }

    public static void RevealNodes(List<Node> nodeList)
    {
        if (nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            node.RevealNode();
    }

    public static void RevealNodes(List<Node> nodeList, Color color)
    {
        if (nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            node.RevealNode(color);
    }

    public static void RevealNodes(List<Node> nodeList, Color color, NodeType excludedType)
    {
        if (nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            if(!node.IsType(excludedType))
                node.RevealNode(color);
    }

    public static void HideNodes(List<Node> nodeList)
    {
        if (nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            node.HideNode();
    }

    public static void ToggleNodes(List<Node> nodeList, Color color,bool toggle)
    {
        if (nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            if(Character.instance.NodeInPath(node))
                node.ToggleNode(Node.colorGreen, toggle);
            else
                node.ToggleNode(toggle);
    }

    public static void ToggleNodes(List<Node> nodeList, bool toggle)
    {
        if(nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            if(Character.instance.NodeInPath(node))
                node.ToggleNode(Node.colorGreen, toggle);
            else
                node.ToggleNode(toggle);
    }

    public static void SetNodesObstacle(List<Node> nodeList, NodeType nodeType, Obstacle obstacle = null, bool isPlatform = false)
    {
        if(nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            node.SetObstacle(obstacle, nodeType, isPlatform);
    }

    public static void ShockNode(Node node)
    {
        if(node == null)
            return;
        node.ShockObstacle();
        List<Node> nodes = NodeGrid.GetNeighborNodeList(node, NodeGrid.Instance.grid, 1);
        if(nodes.Count == 0)
            return;
        for(int i = 0; i < nodes.Count; i++)
            nodes[i].ShockObstacleAfter(node.worldPosition);
            
    }

    public static void TremorNodes(List<Node> nodeList)
    {
        if(nodeList == null ||nodeList.Count == 0)
            return;
        foreach (Node node in nodeList)
            node.TremorObstacle();
    }

    public static void SetNodesType(List<Node> nodeList, NodeType type)
    {
        if(nodeList == null ||nodeList.Count == 0)
            return;
        foreach(Node node in nodeList)
            node.currentNodeType = type;
    }

    public static bool CheckNodesType(List<Node> nodeList, NodeType nodeType, Type type)
    {
        if(nodeList == null ||nodeList.Count == 0)
            return false;
        foreach (Node node in nodeList)
            if(node.IsWalkable(nodeType, type))
                return true;
        return false;
    }

    public static List<Obstacle> GetNodesObstacle(List<Node> nodeList, bool isPlatform = false)
    {
        List<Obstacle> obstacles = new List<Obstacle>();
        for(int i = 0; i < nodeList.Count; i++)
        {
            if(isPlatform)
            {
                if(!nodeList[i].hasPlatform)
                    continue;
                obstacles.Add(nodeList[i].platform);
            }
            else
            {
                if(!nodeList[i].hasObstacle)
                    continue;
                obstacles.Add(nodeList[i].obstacle);
            }
        }
        return obstacles;
    }

    public static Vector3 GetRandomWorldPos(Dictionary<Vector2Int, Node> grid)
    {
        return grid.Values.ToList()[UnityEngine.Random.Range(0, grid.Count)].worldPosition;
    }

    public static Vector3 GetRandomWorldPos(Dictionary<Vector2Int, Node> grid, NodeType nodeTypeGet, bool containsObs)
    {
        List<Node> nodes = grid.Values.ToList();
        Node node = nodes[UnityEngine.Random.Range(0, grid.Count)]; 
        while(!node.CheckIf(nodeTypeGet, containsObs))
            node = nodes[UnityEngine.Random.Range(0, grid.Count)];
        return node.worldPosition;
    }

    public static List<Vector3> GetRandomWorldPos(Dictionary<Vector2Int, Node> grid, int count)
    {
        Debug.Assert(count > 0, "ERROR: Count should be greater than 0!");
        Debug.Assert(grid.Count > 0, "ERROR: GridCount should be greater than 0!");
        List<Vector3> positions = new List<Vector3>(count);
        for (int i = 0; i < count ; i++)
            positions.Add(GetRandomWorldPos(grid));
        return positions;
    }

    public static List<Vector3> GetRandomWorldPos(Dictionary<Vector2Int, Node> grid, int count, NodeType nodeTypeGet, bool containsObs)
    {
        Debug.Assert(count > 0, "ERROR: Count should be greater than 0!");
        Debug.Assert(grid.Count > 0, "ERROR: GridCount should be greater than 0!");
        List<Vector3> positions = new List<Vector3>(count);
        for (int i = 0; i < count ; i++)
            positions.Add(GetRandomWorldPos(grid, nodeTypeGet, containsObs));
        return positions;
    }

}