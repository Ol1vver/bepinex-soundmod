# BepInEx Sound Mod
Replaces sounds in the game at startup by matching the filenames. If there are multiple sounds with the same filename -- it'll unfortunately replace them all for now.
This should theoretically be compatible with any game; but this project was made for Unity 2021.3.16.

## Installation
1. Install BepInEx 5 for your game
2. Put the .dll inside of the BepInEx/plugins directory
3. Create a "sounds" directory or run the game once for the plugin to create one for you
4. Place sounds named exactly after the sound name in-game in OGG/WAV/AIFF/ACC/MPEG format in the "sounds" directory
5. Play!