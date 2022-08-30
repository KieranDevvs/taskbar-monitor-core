# taskbar-monitor-core
DeskBand with monitoring charts (CPU and memory) for Windows

This app shows some cool graphs displaying CPU and memory usage on the taskbar (as a Desk Band).

Fork of https://github.com/leandrosa81/taskbar-monitor, ported to the .NET 6 runtime. Also includes the start of a WPF deskband (which I may or may not finish).

Effort has gone into refactoring bits and pieces, when installing the app, we no longer spawn a shell and execute a command in the background to register the DLL. We hook `Kernel32.dll` to call `DllRegisterServer/DllUnregisterServer` directly. Ive also cleaned up many of the NRT warnings.

- COM objects are mostly supported in .NET Core / 5+ but are missing some features and have extra nuances.
- Dont enable trimming in the deskband or the installer as the runtime will complain about missing feature flags required for WinForms / WPF / DeskBands.
- The project currently is self hosted i.e the user shouldnt need to install the runtime (ive not tested this in practice).
- I managed to get trimming to work (it got the installer assembly down to about 12MB with all the runtime assemblies), but it was very brittle and im not sure if it was   worth the extra effort so I gave up.

![image](https://user-images.githubusercontent.com/21192520/187538343-294b0399-9ed7-4621-a440-062a99c37ce6.png)
