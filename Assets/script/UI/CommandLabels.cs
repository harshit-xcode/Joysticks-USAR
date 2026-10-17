// Keep existing serialized command values and prefab references compatible with older scenes.
public static class CommandLabels
{
    public static string For(BlockType type)
    {
        switch (type)
        {
            case BlockType.Lava: return "Jump";
            case BlockType.Start: return "Start";
            case BlockType.Stop: return "Stop";
            case BlockType.Walk: return "Walk";
            default: return type.ToString();
        }
    }
}
