using System.Collections;
using UnityEngine;

public class LeftBlock : CodeBlock
{
    private void Awake()
    {
        blockType = BlockType.MoveLeft;
    }

    public override IEnumerator Execute(PlayerController player)
    {
        yield return player.MoveLeftCoroutine();
    }
}
