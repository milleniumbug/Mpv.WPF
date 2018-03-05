
# Mpv<span />.WPF

WPF user control to easily play video. Powered by [mpv](https://github.com/mpv-player/mpv).

#### Notes:

* This software is not yet ready to be used in a production environment.
* No documentation has yet been written.

If you encounter any bugs or would like to see a feature added then please open an issue. Contributions are very welcome!

## Download

This package is available via [NuGet](https://www.nuget.org/packages/Mpv.WPF).

## Usage

### Prerequisites

See Mpv<span />.NET documentation [here](https://github.com/hudec117/Mpv.NET#prerequisites).

### User Control

MpvPlayer is the user control that contains the mpv video player. It does not contain any other controls.

The MpvPlayer cannot be created declared in XAML since it requires to be instantiated with the path to the libmpv DLL.

To instantiate MpvPlayer:

```csharp
// Class-scope
private MpvPlayer player;
private const string libMpvPath = @"lib\mpv-1.dll";

// Constructor/Initialisation
player = new MpvPlayer(libMpvPath);

// Add the player as a child to
// an element.
playerHost.Children.Add(player);
```

### Enabling youtube-dl

To enable youtube-dl there are a few more steps to take.

1. Download youtube-dl from https://mpv.srsfckn.biz/.
2. Place "youtube-dl.exe" into the same folder as "mpv-1.dll".
3. Like when you installed "mpv-1.dll", include "youtube-dl.exe" in your project and set it to copy to output directory.
4. Download [ytdl_hook.lua](https://github.com/mpv-player/mpv/blob/master/player/lua/ytdl_hook.lua) from the mpv repository. 
5. Place "ytdl_hook.lua" into your project, into a "scripts" folder if you like. 
6. As previously,
7. Open the script and change the value of "try_ytdl_first" (On line 7) to true. 
8. Change the value of "path" (On line 13) to the relative path from your executable to your "youtube-dl.exe".
9. Lastly, you will need to execute `player.EnableYouTubeDl` along with a relative path to the "ytdl_hook.lua" script which will load the script into mpv.
10. Done! 

Notes:
* The "ytdl_hook.lua" script allows youtube-dl to see when mpv attempts to load a file, which could possibly be a web link.
* Make sure to escape the path! (E.g. "lib\\\\youtube-dl.exe" instead of "lib\youtube-dl.exe")
* You can set the video quality that youtube-dl should try and retrieve by modifying the YouTubeDlQuality property on a player object.

## Related Projects

* [Mpv.NET](https://github.com/hudec117/Mpv.NET) - A .NET wrapper for the mpv C API.
* Mpv.WinForms - Upcoming user control library for Windows Forms.

## Licensing

See Mpv<span />.NET documentation [here](https://github.com/hudec117/Mpv.NET#licensing).
