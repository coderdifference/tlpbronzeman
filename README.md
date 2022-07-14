# Tatical Legacy Pack - BronzeMan SaveScummer

## About
This tool is designed to run in the background and create automatical backups of Tactical Legacy Pack (TLP) saves. 

It is based on another project that provides automatic backup of WotC, EU/EW, and Chimera Squad ironman game saves. Original is available here: https://github.com/propagating/IronmanSaveBackup

While the original purpose of the ancestor project is the protection of XCOM 2 save games (working with any version of the game), as Ironman tends to cause unrecoverable crashes and heartache for many players, the primary purpose of this project is to allow "rolling back" unfortunate turns for players that are not interested in the ironman aspect. 

This tool only works with XCOM2 War of the Chosen - Tatical Legacy Pack DLC.

## How does it work

The Tatical Legacy Pack DLC game mode creates and tracks a single ironman save. This save is replaced at the start of every player turn, and every time you exit to the main menu during the player turn (exiting to main menu on the enemy turn does not update the save). This tool monitors the save game directory and creates a backup of the ironman save as soon as it changes. Backups are made under the configured backup folder using the structure:

{backup root}\Operation {op #}\Mission {mission #}\Turn {turn #}-{timestamp}.isb

The timestamp ensures that subsequent attempts of the same turn or exiting to the menu during the player turn will not replace the original backup created at turn start. 

## Using the tool:

You may start using the tool at any time, it does not need to be open when you start the game itself, however it must be running and save monitoring started in order to capture new saves.

1. Set the save game path.
   
* Click the text box next to "WoTC Save Folder" to open a folder browser.
* Navigate to the location where your game is creating saves (something like C:\Users\{your user}\Documents\My Games\XCOM2 War of the Chosen\XComGame\SaveData)
* Click "Select Folder"

2. Set the backup path (the root path where the backup folder structure and saves will be created)

* Click the text box next to "Backup Root Folder" to open a folder browser
* Navigate to where you want to store your save game backups
* Click "Select Folder"

3. Press the "Start Backup" button, the tool will begin monitoring for new saves
4. Play the game

As you play, the tool will monitor for updated ironman saves and back them up. You can also click the "Force Backup" button to immediately backup the current save file (remember the save is only updated when the player turn begins, or when exiting to main menu during the player turn).

## To restore a save

### Restoring a save for the current turn
1. Quit to main menu
2. Set the backed up save to restore 
3. Press the restore button
4. The tool will delete the current save from the TLP save game folder and replace it with the backed up save
5. Re-enter Legacy and continue the operation

### Restoring a save for a different operation, mission, or turn

Restoring a save for a different operation, mission, or turn, requires one extra step. The game binds the current save with the profile.bin, and will not load a save for a different operation, mission, or turn. However, you can force the game to update profile.bin by opening options and immediately clicking "Save and Exit". The game will rewrite the profile.bin file and bind the current save game. This step should be performed after the tool has restored the save but before re-entering Legacy and continuing the operation. If you do not do this, clicking "Continue Operation" will have no effect. If this happens, simply go back to main menu, edit and save options, and return to Legacy. "Continue Operation" should now work correctly again.

## Notes

* When selecting a backed up save to restore, the tool will open the backup folder at the last save made. 
* The most rececnt backed up save is shown at the bottom of the tool window. 
* When multiple saves of the same turn are available in the list of backed up saves, the one with the earliest timestamp will be from the very beginning of the turn.
* You must exit to main menu before restoring a save, but you do not need to exit the game.
