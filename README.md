# Simple-MonoGame-OpenAL
Super simple semi port of MonoGame Sound classes / OpenAL implementation. 

A quick and dirty version of sound effects cobbled together from MonoGame source.  Was rushed out to fix a null pointer error in MonoGame that only effects certain Android devices, see - https://github.com/MonoGame/MonoGame/issues/7011

Only supports Android using the OpenAL bundled with OpenTK.

Intended as a stop gap until the issue is fixed.  Warning, only to be used in emergencies! Is not feature complete and is likely to be unstable, use at your own risk only if you are effected by the issue. 

# Usage
Uses a simple audio manager.  The manager needs to be updated to remove stopped audio, so ensure it gets updated in your game update loop. Simply download, copy the folder to your project directory and make sure it is included, edit as required to fit your framework.

```cs
Audio.Update();
```

Has a basic loader class, works like Content.Load so the first time you load, it will load and parse the data, subsequent calls will return a new instance using cached data. 

```cs
var sound = P3.AudioLib.Loader.Load(uri);
```

Playing sound effects is easy, just call:

```cs
P3.Audio.PlaySound(filename, loop = false);
```
