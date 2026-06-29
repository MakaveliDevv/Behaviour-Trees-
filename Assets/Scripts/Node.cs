using System.Collections.Generic;
using System.Linq;

public class ReactiveSequence : Node
{
    public ReactiveSequence(string name, int priority = 0) : base(name, priority) {}

    public override Status Process()
    {
        foreach (Node child in children)
        {
            Status status = child.Process();

            if(status != Status.Succes) return status;
        }

        return Status.Succes;
    }
} 

public class RandomSelector : PrioritySelector
{
    protected override List<Node> SortChildren() => children.Shuffle().ToList();

    public RandomSelector(string name, int priority) : base(name, priority) {}
}

public class PrioritySelector : Selector
{
    private List<Node> sortedChildren;
    private List<Node> SortedChildren => sortedChildren ??= SortChildren();

    protected virtual List<Node> SortChildren() => children.OrderByDescending(child => child.priority).ToList();

    public PrioritySelector(string name, int priority = 0) : base(name, priority) {}

    public override void Reset()
    {
        base.Reset();
        sortedChildren = null;
    }

    public override Status Process()
    {
        foreach (Node child in SortedChildren)
        {
            switch (child.Process())
            {
                case Status.Running:
                    return Status.Running;

                case Status.Succes:
                    return Status.Succes;   
                default:
                    continue;
            }
        }

        return Status.Failure;
    }
}

public class Selector : Node
{
    public Selector(string name, int priority = 0) : base(name, priority) {}

    public override Status Process()
    {
        if(currentChild < children.Count)
        {
            switch (children[currentChild].Process())
            {
                case Status.Running:
                    return Status.Running;
                case Status.Succes:
                    Reset();
                    return Status.Succes;
                default:
                    currentChild++;
                    return Status.Running;
            }
        }

        Reset();
        return Status.Failure;
    }
}

public class Sequence : Node
{
    public Sequence(string name, int priority = 0) : base(name, priority) {}

    public override Status Process()
    {
        if(currentChild < children.Count)
        {
            switch (children[currentChild].Process())
            {
                case Status.Running:
                    return Status.Running;
                case Status.Failure:
                    Reset();
                    return Status.Failure;
                default:
                    currentChild++;
                    return currentChild == children.Count ? Status.Succes : Status.Running;
            }
        }

        Reset();
        return Status.Succes;
    }
}

public class BehaviourTree : Node
{
    public BehaviourTree(string name) : base(name) {}

    public override Status Process()
    {
        while(currentChild < children.Count)
        {
            Status status = children[currentChild].Process();
            if(status != Status.Succes)
                return status;
            
            currentChild++;
        }

        Reset();
        return Status.Succes;
    }  
}

public class Leaf : Node
{
    readonly IStrategy strategy;    

    public Leaf(string name, IStrategy strategy, int priority = 0) : base(name, priority)
    {
        this.strategy = strategy;
    }

    public override Status Process() => strategy.Process();
    public override void Reset() => strategy.Reset();
}

public class Node
{
    public enum Status { Succes, Failure, Running }

    public readonly string name;
    public readonly int priority;
    public readonly List<Node> children = new();
    protected int currentChild;

    public Node(string name = "Node", int priority = 0)
    {
        this.name = name;
        this.priority = priority;
    }

    public void AddChild(Node child) => children.Add(child);
    public virtual Status Process() => children[currentChild].Process();

    public virtual void Reset()
    {
        currentChild = 0;
        foreach (Node child in children)
        {
            child.Reset();
        }
    }
}