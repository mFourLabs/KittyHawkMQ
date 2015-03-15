# KittyHawk MQTT .NET Library

A .NET implementation of the MQTT protocol. The library was designed to run across several Microsoft platforms by creating abstractions around platform dependent functionality. Most of the source code is shared across the different projects with only the implementations of these abstractions being different. Documentation for using the library can be found [here](http://www.kittyhawkmq.com/kittyhawkmq-client-api-documentation-latest/).

## System Requirements

The project was built using Visual Studio 2013 update 4 and Windows 8.1 OS. The latest version of .NET Microframework must also be installed.

## Project Layout

* MqttLib - Contains both the Win32 and the WinRT(PCL) projects. There are 2 seperate .csproj files within the folder.
* MqttLibMf42 - Contains the .NET Microframework v4.2 project.
* MqttLibPhone8 - Contains the Windows Phone 8 project.
* MqttLib_Tests - Units test run against the Win32 version of the source code.

All build output goes into (Solution Folder)\bin\\(Platform Folder)

## Platform Abstractions

These are the main abstraction points for platform specific implementations:

* \Net\(PlatformName)SocketAdapter.cs - Implements the ISocketAdapter for each platform. See \Interfaces\ISocketAdapter.cs for more information on the interface calls.
* \Client\Mqtt(PlatformName)Client.cs - Implements the top-level MqttClient client interface for each platform.
* \Settings\Mqtt(PlatformName)Settings.cs - Implements any unique settings required for each platform.

## Platform Initialization

Each project is built with its own version of an MqttClient implementation. These classes have a public static method named CreateClient (or CreateSecureClient) that creates an MqttClient instance instantiated with the appropriate ISockectAdapter concrete class.

Also, each platform has its own class called MqttPlatformSettings, derived from a common MqttSettings class. The MqttSettings base class has common settings for all platforms. The derived class can override any of these settings within its own MqttPlatformSettings implementation for the specific need of that platform.

## Whirlwind Tour of the Source Code

### AssemblyCommon.cs & \Properties\AssemblyInfo.cs

AssemblyCommon.cs contains all assembly attributes common to all projects. Each project has its own version of AssemblyInfo.cs.

### \Client

Contains the MqttClient implementation for the platform. ActiveClientCollection keeps track of clients attached to the library. Currently, it only supports 1 active client at a time. SubscriptionClient tracks all active subscriptions for a client.

### \Collections

Due to differences in collection implementations across the platforms, and to keep dependencies at a minimum, these classes implement some basic collections needed by the library.

### \Exceptions

A few custom exceptions used by the library.

### \Interfaces

Most of the interfaces used by the library are located here.

### \Messages

Mqtt message implementations.

### \Net

Network and protocol implementations. In here are the platform specific implementations of ISocketAdapter.

### \Plugins

An early attempt at a poor man's plugin interface. Currently only logging is implemented through the plugin feature.

### \Settings

All settings related to the protocol or platform.

### \Utilities

Some basic functions needed throughout the library. Data validators, encoders/decoders, timers, etc.
