using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class R_BTSequence : R_BTBaseNode
{
    // the leaf nodes within the sequencer
    protected List<R_BTBaseNode> btNodes = new();

    // btNodes set in constructor
    public R_BTSequence(List<R_BTBaseNode> btNodes)
    {
        this.btNodes = btNodes;
    }

    // all must return true for a success (AND)
    public override R_BTNodeStates Evaluate()
    {
        bool failed = false;
        foreach (R_BTBaseNode btNode in btNodes)
        {
            if (failed) break;

            switch (btNode.Evaluate())
            {
                case R_BTNodeStates.SUCCESS:
                    btNodeState = R_BTNodeStates.SUCCESS;
                    continue;
                case R_BTNodeStates.FAILURE:
                    btNodeState = R_BTNodeStates.FAILURE;
                    failed = true;
                    break;
                default:
                    btNodeState = R_BTNodeStates.FAILURE;
                    failed = true;
                    break;
            }
        }
        return btNodeState;
    }
}
