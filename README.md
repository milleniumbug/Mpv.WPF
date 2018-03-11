# Mpv<span />.WPF

WPF user control to easily play video. Powered by [mpv](https://github.com/mpv-player/mpv).

[![Version](https://img.shields.io/nuget/v/Mpv.WPF.svg?style=flat-square)](https://www.nuget.org/packages/Mpv.WPF/)
[![Downloads](https://img.shields.io/nuget/dt/Mpv.WPF.svg?style=flat-square)](https://www.nuget.org/packages/Mpv.WPF/)

### Features

* Simple setup
* Auto play
* Playlist - Load, Next, Previous Move, Remove, Shuffle or Clear
* Optional youtube-dl support to play videos from hundreds of video sites.
	* Change the desired video quality.

#### Note:

* This software is not yet ready to be used in a production environment.

If you encounter any bugs or would like to see a feature added then please open an issue. Contributions are very welcome!

## Download

This package is available via [NuGet](https://www.nuget.org/packages/Mpv.WPF).

## Usage

### Prerequisites

See Mpv<span />.NET documentation [here](https://github.com/hudec117/Mpv.NET#prerequisites).

### User Control

MpvPlayer is the user control that contains the mpv video player. It does not contain any other controls.

The MpvPlayer cannot be declared in XAML since it needs to be instantiated with the path to the libmpv DLL.

To instantiate MpvPlayer:

```csharp
// Class-scope
private MpvPlayer player;
private const string libMpvPath = @"lib\mpv-1.dll";

// Constructor/Initialisation
player = new MpvPlayer(libMpvPath);

// Add the player as a child to
// a WPF element. (E.g. grid)
playerHost.Children.Add(player);
```

See [Mpv.WPF.Example](https://github.com/hudec117/Mpv.WPF/tree/master/src/Mpv.WPF.Example) in the source code.

### Enabling youtube-dl

youtube-dl is a program that allows you to download videos from YouTube and about another thousand video sites. This program can work in conjunction with mpv to allow you to stream videos on the MpvPlayer.

To enable youtube-dl follow these steps:

1. Download youtube-dl from https://mpv.srsfckn.biz/ or https://rg3.github.io/youtube-dl/download.html.
2. Place "youtube-dl.exe" into the same folder as "mpv-1.dll".
3. Like when you installed libmpv, include "youtube-dl.exe" in your project and set it to copy to output directory.
4. Download [ytdl_hook.lua](https://github.com/mpv-player/mpv/blob/master/player/lua/ytdl_hook.lua) from the mpv repository. 
5. Place "ytdl_hook.lua" into your project, into a "scripts" folder if you like. 
6. As previously, include "ytdl_hook.lua" in your project and set it to copy to output directory.
7. Open the script and make the following changes: 
    1. Change the value of "try_ytdl_first" to true. (Line 7)
        ```lua
        try_ytdl_first = true,
        ```
    2. Change the value of "path" to the relative path from your executable to your "youtube-dl.exe". (Line 13)
        ```lua
        path = "lib\\youtube-dl.exe",
        ```
8. Lastly, you will need to enable youtube-dl using the player like so:
    ```csharp
    const string ytdlHookPath = @"scripts\ytdl_hoook.lua";

    player.EnableYouTubeDl(ytdlHookPath);
    ```
10. Done!

Notes:
* In "ytdl_hook.lua" make sure to escape the path! (E.g. "lib\\\\youtube-dl.exe" instead of "lib\youtube-dl.exe")

## Related Projects

* [Mpv.NET](https://github.com/hudec117/Mpv.NET) - A .NET wrapper for the mpv C API.
* Mpv.WinForms - Upcoming user control library for Windows Forms.

## Licensing

See Mpv<span />.NET documentation [here](https://github.com/hudec117/Mpv.NET#licensing).
