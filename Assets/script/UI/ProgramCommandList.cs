using System.Collections.Generic;

// Stores ordered editable commands between START and END.
public sealed class ProgramCommandList
{
    readonly List<BlockType> commands = new List<BlockType>();
    public IReadOnlyList<BlockType> Commands => commands;
    public int Count => commands.Count;

    public bool Append(BlockType type)
    {
        if (!ProgramSequence.IsEditableCommand(type)) return false;
        commands.Add(type);
        return true;
    }

    public void SetCommands(IEnumerable<BlockType> newCommands)
    {
        commands.Clear();
        if (newCommands == null) return;
        foreach (var t in newCommands)
        {
            if (ProgramSequence.IsEditableCommand(t))
                commands.Add(t);
        }
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
