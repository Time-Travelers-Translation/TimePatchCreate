# TimePatchCreate
The tool that creates a .pat patch file from a .3ds or .cia dump of Time Travelers, an extracted tt1.cpk folder, code.bin, and exheader.bin.

## Disclaimer
The tool is <b>NOT</b> necessary, if you just want to play the patched game!

Refer to [TimePatchApply](https://github.com/Time-Travelers-Translation/TimePatchApply) for that.

## Usage

The tool can be used either by double clicking, which enters a "Guided Mode", or by calling it with command line arguments, which enters "Inline Mode".

### Guided Mode

In "Guided Mode" you will walk through a clear step-by-step set of instructions to create a patch from the game and an extracted tt1.cpk folder, code.bin, and exheader.bin.

First, you enter the path to the <b>decrypted</b> .3ds or .cia of the game "Time Travelers" (you are expected to dump the game from your own cartridge or 3DS console via GodMode9):

![first](https://github.com/user-attachments/assets/35605c66-095c-4e85-ba36-95f7a033816d)

Next, you enter the directory holding your patched files:

![second](https://github.com/user-attachments/assets/55d154d3-6796-4823-ad0c-2a86ecd93a38)

And lastly, you enter the directory in which the Luma3DS LayeredFS-ready set of files should be created:

![third](https://github.com/user-attachments/assets/0a273b00-e87f-4a05-8c59-821dfde9d50b)

### Inline Mode

The "Inline Mode" is the standard way to call an application from a command line.<br>
It's mainly used in scripts to automate certain processes by providing the paths as space-separated arguments.

![inline](https://github.com/user-attachments/assets/ff159ecb-6962-4d99-8b94-cc25ad9f1219)

## Patch Structure

The structure of the directory holding your patched files should look like this:

![files](https://github.com/user-attachments/assets/f4855455-98a9-4838-9c56-d9afd30a0de1)

This folder holds the patched versions of ``code.bin`` and ``exheader.bin``, that should be provided to the users.<br>
Refer to [Code Patches](https://github.com/Time-Travelers-Translation/Documentation/blob/main/README.md#code-patches) to read up on how to patch the ``code.bin`` and what the english translation provides.

Additionally, the directory ``tt1_ctr`` contains patched files from the game's tt1.cpk.
