# JOYSTIX BOT title menu

The title is JOYSTIX BOT. START, NEW GAME, LEVEL SELECT and EXIT use the same NodeCardFrame, LedPill, PortSquare and PortTriangle sprites as the gameplay circuit nodes. Labels appear in the card headers. No slogans, tiny captions, circular button containers or keyboard footer are shown.

Instruction: Drag from START to an option.

Connections run from START's output port to the chosen card's input port. Clicking START then an option also works. Enter arms START; Up/Down choose an option; Enter connects; Escape cancels. Card and port hit areas work while gameplay is paused.

NEW GAME clears campaign completion, unlocks, saved circuits and undo history, then starts at level 1 with later levels locked. LEVEL SELECT respects saved availability. EXIT quits the player or stops Unity Play Mode. RESUME returns to the current board and program. Audio retains saved music/SFX settings.

The existing Claude bot, level-platform diorama and animated Early Dusk sky remain. The skybox uses its own material instance with the original six textures. Background rotation, cloud drift and star twinkling use unscaled time. The atmosphere has a shader-error guard. Display geometry cannot register gameplay tiles. Scene lights' masks are restored and generated rendering resources are released when the menu is destroyed.
