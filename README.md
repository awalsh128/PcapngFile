<p align="center">
  <img src="logo.png" alt="PCAP-NG File Reader logo" width="128" height="128">
</p>

<h1 align="center">.NET PCAP-NG File Reader</h1>

<p align="center">
  Reads PCAP Next Generation (pcapng) files and generates CLR objects from its data.<br/>
  Implemented according to the <a http="https://datatracker.ietf.org/doc/draft-ietf-opsawg-pcapng/">PCAP-NG draft specification</a>.
</p>

[![CI](https://github.com/awalsh128/PcapngFile/actions/workflows/ci.yml/badge.svg)](https://github.com/awalsh128/PcapngFile/actions/workflows/ci.yml)
[![NuGet version (PcapngFile)](https://img.shields.io/nuget/v/PcapngFile.svg)](https://www.nuget.org/packages/PcapngFile/)

## Requirements

.NET 10.0 or later. Version 1.x of the package supports .NET 5.0.

## Install

```
dotnet add package PcapngFile
```

## Usage

Read every block in order. Each block is returned as `BlockBase`, but its actual type is one of the block classes listed below.

```csharp
using PcapngFile;

using (var reader = new Reader("myfile.pcapng"))
{
   BlockBase block;
   while ((block = reader.ReadBlock()) != null)
   {
      switch (block)
      {
         case EnhancedPacketBlock packet:
            // ...
            break;
         case InterfaceDescriptionBlock iface:
            // ...
            break;
      }
   }

   reader.Reset();

   // Which is equivalent to.
   foreach (var readBlock in reader.AllBlocks)
   {
      // ...
   }
}
```

Or if you just want a particular block type.

```csharp
using (var reader = new Reader("myfile.pcapng"))
{
   foreach (var block in reader.EnhancedPacketBlocks)
   {
      // Act on received block of type EnhancedPacketBlock.
   }
}
```

A `Reader` can also be created from any `Stream`, including ones that can't seek such as network streams. Pass `leaveOpen: true` to keep the stream open after the reader is disposed. `Reset` and the automatic reset at the end of an enumeration need a seekable stream.

```csharp
using var stream = File.OpenRead("myfile.pcapng");
using var reader = new Reader(stream, leaveOpen: true);
```

### Timestamps

`EnhancedPacketBlock.Timestamp` is the raw value from the file. Convert it to a UTC `DateTime` with `GetTimestamp`. Passing the packet's interface description applies that interface's timestamp resolution (`if_tsresol`, base 10 or base 2) and offset (`if_tsoffset`); without it, the default resolution of microseconds and no offset are assumed. `precisionLoss` is `true` when the timestamp was finer than `DateTime`'s 100 ns tick and was truncated.

```csharp
var interfaces = reader.InterfaceDescriptionBlocks.ToList();
foreach (var packet in reader.EnhancedPacketBlocks)
{
   DateTime time = packet.GetTimestamp(interfaces[packet.InterfaceID], out bool precisionLoss);
}
```

Files written on big-endian and little-endian machines are both supported, as are files that mix the two across sections. Blocks and name resolution records of types the library doesn't support are skipped.

`InterfaceStatisticsBlock` and the obsolete `PacketBlock` have the same raw timestamps and `GetTimestamp` helpers. `InterfaceStatisticsBlock` also has `GetStartTime` and `GetEndTime`, which return `null` when the option is absent.

### Options

Each option has a property on its block, named after the spec field in its documentation (e.g. `InterfaceDescriptionBlock.TimestampResolution` is `if_tsresol`). Options that can repeat, such as comments, interface addresses and packet hashes, are lists (`Comments`, `IPv4Addresses`, `Hashes`); the singular property (`Comment`, `IPv4Address`, `Hash`) returns the first one. Vendor-specific options are in `CustomOptions`. Options with an invalid length are skipped and their codes listed in `InvalidOptionCodes`.

Sections with a major version other than 1 are skipped, as the spec requires.

## Block types

| Class                       | Contents                                                                                                   |
| --------------------------- | ---------------------------------------------------------------------------------------------------------- |
| `SectionHeaderBlock`        | Starts a section of the file: format version, hardware, OS and application that wrote it.                  |
| `InterfaceDescriptionBlock` | An interface that packets were captured on: link type, snap length, name, addresses, timestamp resolution. |
| `EnhancedPacketBlock`       | A captured packet with its interface, timestamp, lengths and flags.                                        |
| `SimplePacketBlock`         | A captured packet with only its data and original length.                                                  |
| `PacketBlock`               | A captured packet in the obsolete Packet Block format.                                                     |
| `NameResolutionBlock`       | Address to host name records, and the DNS server that resolved them.                                       |
| `InterfaceStatisticsBlock`  | Capture statistics for an interface: packets received, dropped and accepted by the filter.                 |

`Reader.GetClrType` and `Reader.GetStoreType<T>` convert between a `BlockType` value and its class.

## License

BSD 2-Clause. See [LICENSE](https://github.com/awalsh128/PcapngFile/blob/master/LICENSE).

## Additional Information

See my [blog post](http://awalsh128.blogspot.com/2013/04/pcap-ng-reader-for-net.html) for more information.
