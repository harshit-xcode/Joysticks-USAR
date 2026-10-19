# Hazard failure feedback

A hazard death now ends the current attempt once. Respawn returns the robot to Start without restarting the program. The player's node graph remains intact and becomes editable as soon as execution ends.

The diagnostic snapshots the pass, command index/type, tile coordinate, beat, and hazard cause before respawn resets the world. Beats in player-facing messages start at 1. Permanent red spikes (legacy LavaTile), hydraulic phase spikes, and laser gates all use this path. A death during the end-of-command beat check stops before the next command is executed.

The failed command keeps a red border/LED. The affected tile keeps a red outline. A persistent panel explains the failure and asks the player to edit and retry. Editing the circuit, resetting, running again, or switching levels clears old feedback. Ordinary blocked moves retain the existing bump-and-continue behavior; no stationary-loop stop or pass limit was added.

Validation in Unity 6000.0.81f1 Play Mode covers first-death stopping, no automatic retry after respawn, immutable diagnostics, preserved circuit/editing, command/tile highlights, edit/reset cleanup, death during CompleteBeat, a gate closing under the robot, a jump landing on lava, and unchanged blocked-loop behavior. Existing L2, Wall Trick, Power Pulse and Final Weave solutions are also exercised.
