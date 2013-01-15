# Core/ViewClients 

This folder contains multiple implementations of the storage abstraction
used to access view data. It is taken from Lokad.CQRS and allows to
switch between Windows Azure Storage and Filesystem storage without rewriting 
the code.

See:

* ViewClient - helper wrapper around IRawViewContainer, which can deal with
  transient connectivity errors. This is the one you should normally use.
* IRawViewRoot and IRawViewContainer - raw interface for reading and writing views.
* AzureViewContainer - Windows Azure implementations of view storage.
* FileViewContainer - file I/O implementations of view storage.

Use PlatformClient in order to create an appropriate instance of view client,
based on the configuration value.