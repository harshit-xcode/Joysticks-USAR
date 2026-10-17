using UnityEngine;

// Central registry of node display labels, sub-badges, colors, and intentional circuit wire colors.
public static class CommandLabels
{
    public static string For(BlockType type)
    {
        switch (type)
        {
            case BlockType.Start:     return "START";
            case BlockType.Walk:      return "WALK";
            case BlockType.Lava:      return "JUMP";
            case BlockType.Stop:      return "LOOP";
            case BlockType.MoveLeft:  return "LEFT";
            case BlockType.MoveRight: return "RIGHT";
            case BlockType.Force:     return "STRIDE";
            case BlockType.Wait:      return "PULSE";
            default:                  return type.ToString().ToUpperInvariant();
        }
    }

    public static string SubBadgeFor(BlockType type)
    {
        switch (type)
        {
            case BlockType.Start:     return "EMITTER";
            case BlockType.Walk:      return "+1 TILE";
            case BlockType.Lava:      return "SKIP 1";
            case BlockType.MoveLeft:  return "SIDE L";
            case BlockType.MoveRight: return "SIDE R";
            case BlockType.Stop:      return "REPEAT";
            case BlockType.Force:     return "+2 TILES";
            case BlockType.Wait:      return "FLIP GATE";
            default:                  return "MODULE";
        }
    }

    public static Color LedColorFor(BlockType type)
    {
        switch (type)
        {
            case BlockType.Start:     return new Color(0.16f, 0.96f, 0.72f); // Emerald Emitter
            case BlockType.Walk:      return new Color(0.00f, 0.90f, 1.00f); // Electric Cyan
            case BlockType.Lava:      return new Color(1.00f, 0.77f, 0.00f); // Solar Gold
            case BlockType.MoveLeft:  return new Color(0.70f, 0.53f, 1.00f); // Neon Violet
            case BlockType.MoveRight: return new Color(1.00f, 0.32f, 0.53f); // Neon Coral
            case BlockType.Stop:      return new Color(1.00f, 0.63f, 0.16f); // Loopback Amber
            case BlockType.Force:     return new Color(0.38f, 0.95f, 0.45f);
            case BlockType.Wait:      return new Color(0.18f, 1.00f, 0.82f); // EMP Aqua-Mint
            default:                  return Color.cyan;
        }
    }

    // Intentional single-wire color:
    // - Open/Incomplete circuit: Warm Standby Amber so player sees the loop isn't closed yet.
    // - Closed/Armed circuit (START -> ... -> END): Unified Powered Cyber Cyan across all wires!
    public static Color IntentionalWireColor(bool isCircuitClosed)
    {
        return isCircuitClosed
            ? new Color(0.00f, 0.90f, 1.00f, 0.96f)  // Powered Cyber Cyan (Ready to Execute)
            : new Color(0.95f, 0.65f, 0.22f, 0.85f); // Unclosed Circuit Amber (Needs wiring to LOOP/END)
    }

    public static Color WireColorFor(int wireIndex, BlockType sourceType)
    {
        return IntentionalWireColor(true);
    }

    public static string IconNameFor(BlockType type)
    {
        switch (type)
        {
            case BlockType.Start:     return "Icon_Start";
            case BlockType.Walk:      return "Icon_Walk";
            case BlockType.Lava:      return "Icon_Jump";
            case BlockType.Stop:      return "Icon_End";
            case BlockType.MoveLeft:  return "Icon_Left";
            case BlockType.MoveRight: return "Icon_Right";
            case BlockType.Force:     return "Icon_Force";
            case BlockType.Wait:      return "Icon_Wait";
            default:                  return "Icon_Walk";
        }
    }
}
