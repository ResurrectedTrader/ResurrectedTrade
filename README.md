# Resurrected Trade Agent

# !! Easily detected online, no reported bans, but use at your own risk !!

## What is [resurrected.trade](https://resurrected.trade)?

It's a website for trading items as well as mule management in Diablo II: Resurrected.

It tries to be equivalent of [poe.trade](https://poe.trade) in terms of search functionality, 
trying to provide a better experience for trading items that can't just be referred to as "Shako" etc.
Namely, looking for jewels, rares, with specific rolls, level requirements for low level dueling etc.

Check out this show-case video showing the features:

[![resurrected.trade](https://img.youtube.com/vi/H9arCErFMdI/0.jpg)](https://www.youtube.com/watch?v=H9arCErFMdI)


## What is in this repository?

Agent (and libraries for integration) for [resurrected.trade](https://resurrected.trade)

The website itself and server components might be open-sourced in the future.

## What is Resurrected Trade Agent?

It's an application that you run on your computer, that monitors for instances
of Diablo II: Resurrected, and syncs your characters/items to [resurrected.trade](https://resurrected.trade) by reading
the games memory, similarly how an anti-virus might scan memory of your applications.

It does not inject anything or modify the game in any way.

## Why did you make this?

Because I hate:

1. Lobby filled with games trying to trade, preventing me from finding actual games.
2. Character switching cooldown preventing me to quickly find that rune to finish the runeword.
3. The poor experience buying and selling items, having to type out stats/rolls by hand, or having to make screenshots.

## Is this "legal"?

🤷 Probably not, but I'd hope Blizzard would not ban for this, as it does not provide you with a gameplay advantage.
Players did get banned for using map hacks, which used simillar methods (by reading the games memory), however they do provide a game play advantage.
There are also simillar applications based on Overwolf that are supposedly "Blizzard approved".
I am not aware of any cases of users using this and being banned, but this is not to say that this will not happen, hence use at your own risk.

## I don't trust your binaries, how do I build this myself?

The project can be simply built using Visual Studio or `dotnet build` command line tool, assuming you have 
.NET SDKs installed. 

There should not be any special build steps outside of the standard C# toolchain.

Please note that debug builds assume that all of the services run on your own machine (for the purposes of development),
so if you do plan to use your own builds with [resurrected.trade](https://resurrected.trade), make sure you are building a
release build.

The "official" builds come in two flavours:

1. FrameworkDependant - relies on having .NET Core 3.1 installed on your machine beforehand.
2. Self contained - ships .NET Core 3.1 time as part of the application (hence why it's so large)

## I can't launch the application, it shows up as "Windows protected your PC"?

This is because this application is new, has not yet had many users, therefore Windows does not trust it.

You can click "More Info" and "Run Anyway" to start it.

![image](https://user-images.githubusercontent.com/104942311/168426474-77b1dc15-79d1-494b-b023-43a4df5cb857.png)

This would not happen if you were to build the application yourself, but you'd probably miss out on potential updates (or have to keep rebuilding)
which are needed as new versions of Diablo II: Resurrected are released.

## I want to help

You can already help by:

1. Using the site/agent, and listing your items. A trading site is only useful if there are traders using it.
2. Spread the word about this project to help it gain more users/traction, especially in your local (non-English) gaming community.
3. Help out with translations in [Transifex](https://www.transifex.com/resurrected-trade/resurrected-trade/dashboard/)

## I have a suggestion/feature request/bug report

1. Check existing Github issues, to make sure someone hasn't opened one that matches what you want to poen
2. Open an issue on Github

## Credits

Projects that helped make this happen: 

1. [youdz/d2-stash-organizer](https://github.com/youdz/d2-stash-organizer)
2. [dschu012/D2SLib](https://github.com/dschu012/D2SLib)
3. [nokka/d2-armory-gui](github.com/nokka/d2-armory-gui)
4. [ThePhrozenKeep/D2MOO](https://github.com/ThePhrozenKeep/D2MOO)
5. BRE

Diablo and Blizzard Entertainment are trademarks or registered trademarks of Blizzard Entertainment, Inc. in the U.S. and/or other countries.
