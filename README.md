PlanetSide 1 GameLogger
=======================
The frontend C# GUI for a extensible PlanetSide 1 (PS1) event logger loosely modeled after Wireshark. It is able to capture arbitrary game events, such as
state changes (leaving/joining world, entering/exiting a vehicle, etc.).
Currently it only captures unencrypted network packets as that is the most critical data source from PS1.

To achieve this, it communicates with a game-specific DLL over a named pipe using a special protocol.
Multiple loggers (at max 5) can run simultaneously on different instances of the game.

Created to assist the [PSForever](http://psforever.net/) project.

Notable Features
================
* Automatic DLL injection using `CreateRemoteThread`
* Customizable communication protocol between logger and DLL
* Simple and robust GCAP capture file format (similar to PCAP)
* Performant ListView for multi-million record viewing
* Capture record batching for low-overhead capturing

Planned Features
================
* Capture Record filtering (a la Wireshark)
* Automatic packet decoding and viewing
* Display of capture file statistics/metadata

License
=======
MIT. See LICENSE.md.
