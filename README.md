# Zen Trunk
## Introduction

Trunk started life as an investigation into whether it was possible to write a block storage engine and then build a database engine on top of it.

It's the conclusion of what happens when you read the book "Inside SQL Server 6.5" and find that the level of detail in the book is almost enough to actually cut code... Well I say "almost"; clearly it definitely is possible!

The codebase has been rewritten a few times over the years;

v0.1 started life as an APM-based implementation that was very difficult to follow. 

v0.2 was rewritten with System.Task-based calls; slightly less difficult to follow but still very tricky.

v0.3 was a comprehensive rewrite that switched to Task-based async/await and brought in TPL Dataflow. This change was a major milestone. The code became much easier to follow and understand!

v0.4 was where the locking mechanism was rewritten with full state machine/dataflow semantics (it took a while to get all the bugs out of that)

v0.5 saw the low-level memory management classes wrapped in SafeHandle based classes - this was done following the best practices used by the .NET Framework engineers so we _should_ never
ever leak handles or memory blocks no matter what the process does. This also meant that much of the dispose implementation patterns used in so many other classes could be simplified; no need for destructors!

v0.6 logical code layout changes - split the namespaces into separate assemblies and broke out the buffer-field serialization classes.

v0.7 improved unit testing closed lots of runtime bugs and really tidied up the runtime behaviour

v0.8 fixed the device mount/dismount logic which was rather broken and also fixed the database create logic

v0.9 resolved long standing issues in the caching buffer device

v0.10 corrected the implementation of the SQL parser where it builds the C# expression tree for CREATE DATABASE arguments - all file-spec arguments are now built into a full expression tree rather than taking
the shortcut of passing an initialised constant expression. Much better as it fits better with the way the expression tree visitor is supposed to work.

v0.11 finally saw the addition of audio sample support

v0.12 saw the audio index support added. The fixes and implementation refactoring here will be back-ported into the table index manager

I still have no idea when v1.0 will hit the streets but the journey has been lots of fun so far!!

## Architecture

Currently standing at 17 projects (excluding the bits associated with the WiX-based installer) the solution is split into a number of layers;

### Infrastructure helpers
These assemblies contain threading helpers and streaming primitives. The streaming classes are very useful for anyone wanting to implement pull-based streams, buffered streams etc.

### Virtual memory manager
This assembly handles wrapping of calls to low-level Win32 APIs VirtualAlloc, VirtualFree etc allowing the manipulation of *VirtualBuffer* objects that can be safely persisted via unbuffered asynchronous I/O operations for maximum speed.

Has an extension of FileStream class that supports sparse files and unbuffered scatter/gather I/O for even greater throughput; the scatter/gather support has been extensively tested however the sparse file support has not been fully utilised as yet.

Finally contains device class abstractions that allow a file (or collection of files) to be represented as a virtual device that knows how to read and write fixed-size pages. This is a core competency needed for block-based I/O across a collection of files.
Essential for databases but also somewhat handy in other applications from remote storage buffering, torrent engines, file-servers etc.

This little lot represents the neucleus for any data-storage engine that needs high-performance I/O and as such could be used for a torrent engine, high-performance logging system or (in this case) a database engine.

### Storage engine
This jumbo assembly contains the logic for handling ACID transactions of pages.

This single sentence means;

* it has a CachingBufferDevice that handles queuing reads and writes to the underlying storage to that it is possible to take advantage of the scatter/gather feature to read from or write to many different pages in a single operation.

* it has code to perform immediate writes to and recovery from a transaction log.

* it has locking primitives that allow all the ACID lock types including support for lock hierarchies and lock escalation.

* it has support for organising table data.

* it has support for indexing table data.

* it has support for organising audio sample data.

* it has support for indexing audio sample data.

* it supports identification and lock tracking by session and transaction identifiers.

### Query manager
This assembly handles the translation of SQL into C# expression trees that execute actions against the storage engine. The engine is powered by the amazing ANTLR v4 project. The SQL grammar is T-SQL although so far almost nothing has been implemented!

Basically CREATE DATABASE and SET TRANSACTION ISOLATION LEVEL. We might have BEGIN TRANS/COMMIT/ROLLBACK too now I think about it!

### Network manager
This assembly handles management of the network connection between client and the service including the establishment of session and transaction scopes. The network protocol is proprietary and utilises SuperSocket for TCP/IP based communications. No support for NamedPipes, SharedFiles or anything else at present.

### Trunk service
This assembly interfaces with the Windows Service Control Manager and provides the process host. The service is designed to be multi-instance capable (default instance and named instances)

### Installer support
A suite of components to support installing the service. Creating an installer capable of installing instance-based services is actually rather complicated - especially if you wish to 
correctly support product upgrade scenarios, selective uninstall/reinstall and keep everything in a good, consistent state. 
The solution used is based on Windows Installer for XML (WiX) and involves a surprising amount of C++ and custom stuff!

### Torrent Engine
A collection of bits - the goal of which was to create a BitTorrent engine that sat on top of the Virtual Memory Manager.
