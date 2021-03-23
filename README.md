# NMSPakViewer
No Man's Sky .pak file viewer.

In-memory viewer.

On startup:
- Finds the game folder and loads the paths of all game and mod .pak files.  Currently only Steam version is supported.
- For each .pak file extract the manifest and build an in-memory file tree.
- Merges all game .pak file trees.

User can select a specific .pak file, or view the merged file tree.
A breadcrumb control is used to select a specific entry in the current file tree.
User can save any file to disk, paths are not saved.  MBIN entries can be saved as either .mbin or .exml files.

When a file is selected its contents are extracted (in-memory) from its parent .pak file, converted as needed, and the appropriate viewer control used to display the data.

<b>Entry types (notable):</b>

The following types are only present in .pak specific file trees, they are excluded from the merged file tree:
- .BIN - Many, no useful info to view.
- .SPV - There are ~46K in one folder alone.  May include in merged tree if find a way to decompile or otherwise view useful info.
- .WEM - Many, need wwise code to convery from .wem to other playable audio format.

Suported types:
- .TXT, .JSON, .XML, .LUA - Extracted data is passed as-is to Avalon Editor.
- .DDS - Pfim is used to convert entries to bitmaps for display.  The bitmaps are stretched to the window size.  Pfim does not support all .dds types.
- .MBIN - libMBIN is used to load the entry data and convert it to .exml format.
Note: The libMBIN.dll in the application folder is used to decompile both game and mod .pak files.
When the game is updated an updated version of libMBIN.dll is needed to view the game .pak files; however, it may not work on old mod .pak files.
May add ability to download appropriate libMBIN.dll for mod .pak files as needed if needed.

<b>Depends on:</b>

SharpZipLib - https://github.com/icsharpcode/SharpZipLib
Decompress .pak file entries.

AvalonEdit - https://github.com/icsharpcode/AvalonEdit
View entries that can be converted to text.
Use Ctrl-F to open search control in top-right corner of editor.

libMBIN - https://github.com/monkeyman192/MBINCompiler
Decompiles .mbin entries and optionally save as .exml files.

Pfim - https://github.com/nickbabcock/Pfim
Converts (most) .dds entries to bitmaps for viewing.

Download the latest versions as needed.  In particular, download the latest libMBIN whenever it's been updated to support a new version of the game.



