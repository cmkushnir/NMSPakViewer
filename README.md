# NMSPakViewer
No Man's Sky .pak file viewer.

In-memory viewer.

On startup:
- Loads the paths of all game and mod .pak files.
- For each .pak file extract the manifest and build an in-memory item tree.
- Merge all game .pak item trees.

For Steam versions of the game the app should find the game folder automatically.
For other cases the user is prompted to select the game folder.
The found|selected game folder is saved to|loaded from:</br>
HKEY_CURRENT_USER\SOFTWARE\HelloGames\NoMansSky\InstallDir</br>
If InstallDir is invalid the app will re-find|prompt to select the game folder on next start.

Toolbar:
- Combobox lists all mod and game .pak files. Select a specific .pak file to have the breadcrumb control use its item tree, or none to use the merged item tree.
- Breadcrumb control to select a specific item in the current item tree.
When an item is selected its contents are extracted (in-memory) from its parent .pak file, converted as needed, and the appropriate viewer control used to display the data.
- Copy button copies the current path to the clipboard e.g. for pasting into an AMUMSS .lua script.</br>
- Save button will open the Save File dialog to save the item to disk; the path is not saved.</br>
MBIN items can be saved as either .mbin or .exml files (use the Save as Type combobox in the Save File dialog).
- GitHub button opens the default browser to the GitHub project page.

Folding support is available for items that can be viewed as xml data.</br>
If a mod .pak file is selected then any items that replace a game item will be shown in a diff viewer, if one is available for the item type.</br>
Folding is disabled when viewing an xml diff, as there is no easy way to sync foldings between viewers.</br>

<h2>.PAK Entry Types:</h2>

Unsupported:</br>
The following types are only present in .pak specific entry trees, they are excluded from the merged entry tree:
- .BIN - Many, no useful info to view.
- .SPV - There are ~46K in one folder alone, no useful info to view.
- .WEM - Many, need wwise code to convery from .wem to other playable audio format.

Supported:
- .TXT, .CSV, .JSON, .XML, .LUA - Extracted data is passed as-is to Avalon Editor.
- .DDS - Pfim is used to convert entries to bitmaps for display.  The bitmaps are stretched to the window size.  Pfim does not support all .dds types.
- .MBIN - libMBIN is used to load the entry data and convert it to .exml format.
Note: The libMBIN.dll in the application folder is used to decompile both game and mod .pak files.
When the game is updated an updated version of libMBIN.dll is required to view the game .pak files; however, it may not work on old mod .pak files.

<h2>Depends on:</h2>

https://github.com/icsharpcode/SharpZipLib</br>
SharpZipLib decompress .pak file entries.

https://github.com/icsharpcode/AvalonEdit</br>
AvalonEdit views entries that can be converted to text.</br>
Use Ctrl-F to open the search panel in top-right corner of editor.

https://github.com/monkeyman192/MBINCompiler</br>
libMBIN decompiles .mbin entries and optionally save as .exml files.

https://github.com/nickbabcock/Pfim</br>
Pfim converts (most) .dds entries to bitmaps for viewing.

https://github.com/mmanela/diffplex</br>
DiffPlex calculates diffferences between entries with text viewers e.g. mbin, xml.

Download the latest versions as needed.  In particular, download the latest libMBIN whenever it's been updated to support a new version of the game.
