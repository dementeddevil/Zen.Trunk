# Zen Trunk
## Introduction

Trunk started life as an investigation into whether it was possible to write a block storage engine and then build a database engine on top of it.

It's the conclusion of what happens when you read the book "Inside SQL Server 6.5" and find that the level of detail in the book is almost enough to actually cut code...

The code-base is split into a number of layers;

### Infrastructure helpers
These assemblies contain threading helpers and streaming primitives

### Virtual memory manager
This assembly handles wrapping of calls to low-level Win32 APIs VirtualAlloc, VirtualFree etc allowing the manipulation of *VirtualBuffer* objects that can be safely persisted via unbuffered asynchronous I/O operations for maximum speed.

Has an extension of FileStream class that supports sparse files and scatter/gather I/O for even greater throughput.

Finally contains device class abstractions that allow a file (or collection of files) to be represented as a virtual device that knows how to read and write fixed-size pages.

This little lot represents the neucleus for any data-storage engine that needs high-performance I/O and as such could be used for a torrent engine, high-performance logging system or (in this case) a database engine.

### Storage engine
This jumbo assembly contains the logic handle ACID transactions to pages.

This single sentence means;

* it has a CachingBufferDevice that handles queuing reads and writes to the underlying storage to that it is possible to take advantage of the scatter/gather feature to read from or write to many different pages in a single operation.

* it has code to perform immediate writes to and recovery from a transaction log.

* it has locking primitives that allow all the ACID lock types and support lock hierarchies and lock escalation.

* it has support for organising table data.

* it has support for indexing table data.

* it supports identification and lock tracking by session and transaction identifiers.

### Query manager
This assembly handles the translation of SQL into C# expression trees that execute actions against the storage engine.

### Network manager
This assembly handles management of the network connection between client and the service including the establishment of session and transaction scopes.

### Trunk service
This assembly interfaces with the Windows Service Control Manager and provides the process host. The service is designed to be multi-instance capable (default instance and named instances)

### Installer support
A suite of components to support installing the service. Creating an installer capable of installing instance-based services is actually rather complicated - especially if you wish to correctly support product upgrade scenarios, selective uninstall/reinstall and keep everything in a good, consistent state. The solution used is based on Windows Installer for XML (WiX) and involves a surprising amount of C++ and custom stuff!

### Torrent Engine
A collection of bits - the goal of which was to create a BitTorrent engine that sat on top of the Virtual Memory Manager.
