using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R_BTSelector : R_BTBaseNode
{
    // the leaf nodes within the selector
    protected List<R_BTBaseNode> btNodes = new();

    // btNodes set in constructor
    public R_BTSelector(List<R_BTBaseNode> btNodes)
    {
        this.btNodes = btNodes;
    }

    // one must return true for a success (OR)
    public override R_BTNodeStates Evaluate()
    {
        foreach (R_BTBaseNode btNode in btNodes)
        {
            switch (btNode.Evaluate())
            {
                case R_BTNodeStates.SUCCESS:
                    return R_BTNodeStates.SUCCESS;
                case R_BTNodeStates.FAILURE:
                    continue;
                default:
                    continue;
            }
        }
        return R_BTNodeStates.FAILURE;
    }
}
