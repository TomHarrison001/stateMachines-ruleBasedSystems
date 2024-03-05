using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R_BTAction : R_BTBaseNode
{
    // stores the function signature for the action
    public delegate R_BTNodeStates ActionNodeFunction();

    // called to evaluate this node
    private ActionNodeFunction btAction;

    // the function stored in the class constructor
    public R_BTAction(ActionNodeFunction btAction)
    {
        this.btAction = btAction;
    }

    // evaluates the action node
    public override R_BTNodeStates Evaluate()
    {
        switch (btAction())
        {
            case R_BTNodeStates.SUCCESS:
                btNodeState = R_BTNodeStates.SUCCESS;
                return btNodeState;
            case R_BTNodeStates.FAILURE:
                btNodeState = R_BTNodeStates.FAILURE;
                return btNodeState;
            default:
                btNodeState = R_BTNodeStates.FAILURE;
                return btNodeState;
        }
    }
}
