using System.Collections.Generic;

// Stores only editable commands. START and STOP are always supplied by ProgramSequence.
public sealed class ProgramCommandList
{
    readonly List<BlockType> commands = new List<BlockType>();
    public IReadOnlyList<BlockType> Commands => commands;
    public int Count => commands.Count;

    public bool Append(BlockType type)
    {
        if (type != BlockType.Walk && type != BlockType.Lava) return false;
        commands.Add(type);
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (index < 0 || index >= commands.Count) return false;
        commands.RemoveAt(index);
        return true;
    }

    public void Clear() { commands.Clear(); }
    public List<BlockType> BuildSequence() { return ProgramSequence.WithFixedEndpoints(commands); }
}
