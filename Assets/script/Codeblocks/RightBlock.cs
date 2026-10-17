using System.Collections;
using UnityEngine;

public class RightBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.MoveRight;
    }

    public override IEnumerator Execute(PlayerController player)
    {
        yield return player.MoveRightCoroutine();
    }
}
