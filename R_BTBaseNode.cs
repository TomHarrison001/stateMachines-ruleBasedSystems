using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class R_BTBaseNode
{
    public enum R_BTNodeStates { SUCCESS, FAILURE };

    // current state of the node
    protected R_BTNodeStates btNodeState;

    // return node state
    public R_BTNodeStates BTNodeState { get { return btNodeState; } }

    // evaluate conditions
    public abstract R_BTNodeStates Evaluate();
}
