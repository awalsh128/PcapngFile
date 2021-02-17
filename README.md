# PCAP-NG File Reader

Reads PCAP Next Generation files and generates CLR objects from its data. Implemented according to the draft specification at http://www.winpcap.org/ntar/draft/PCAP-DumpFileFormat.html.

![.NET](https://github.com/awalsh128/PcapngFile/workflows/.NET/badge.svg)&nbsp;&nbsp;
<a href="https://www.nuget.org/packages/PcapngFile/">
   <img src="https://img.shields.io/nuget/v/PcapngFile.svg" alt="NuGet version (PcapngFile)"/>
</a>

## Usage

```csharp
using (var reader = new Reader("myfile.pcapng"))
{
   BlockBase block;
   while ((block = reader.ReadBlock()) != null)
   {
      // Act on received block. It's cast will be BlockBase 
      // but its true underlying type will be any of BlockBase's
      // children.
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

## Install NuGet Package
<pre><code>PM&gt; Install-Package PcapngFile </code></pre>

## Additional Information

See my <a href="http://awalsh128.blogspot.com/2013/04/pcap-ng-reader-for-net.html">blog post</a> for more information.
