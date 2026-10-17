using System;

// Progress through the editable commands repeatedly, until level completion/cancellation.
public sealed class ProgramLoopCursor
{
    readonly int count;
    int next;
    bool complete;
    public ProgramLoopCursor(int commandCount)
    {
        if (commandCount <= 0) throw new ArgumentOutOfRangeException(nameof(commandCount));
        count = commandCount;
    }
    public bool TryNext(out int index)
    {
        index = -1;
        if (complete) return false;
        index = next;
        next = (next + 1) % count;
        return true;
    }
    public void RestartIteration() { if (!complete) next = 0; }
    public void Complete() { complete = true; }
}
