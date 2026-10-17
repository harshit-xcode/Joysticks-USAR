// CodeBlock.cs
// Abstract base class for all node blocks in the Machine Learning Lab node graph.
// Every block (StartBlock, WalkBlock, LavaBlock [Jump], LeftBlock, RightBlock, ForceBlock, WaitBlock, StopBlock)
// inherits from this and implements Execute() as a coroutine.

using System.Collections;
using UnityEngine;

public enum BlockType
{
    Start = 0,
    Walk = 1,
    Lava = 2,      // Legacy enum value preserved for Jump node compatibility
    Stop = 3,
    MoveLeft = 4,  // Sidestep 1 tile Left (+Z in grid)
    MoveRight = 5, // Sidestep 1 tile Right (-Z in grid)
    Force = 6,     // Dash 2 tiles Forward (+X in grid), stopping at obstacles
    Wait = 7       // Hold position for 1 beat
}

public abstract class CodeBlock : MonoBehaviour
{
    [Header("Block Info")]
    public BlockType blockType;

    // Executes this node's action on the player in the 3D isometric scene.
    public abstract IEnumerator Execute(PlayerController player);
}
