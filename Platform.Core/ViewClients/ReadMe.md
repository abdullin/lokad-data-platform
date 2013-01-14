Core/ViewClients 
================

This folder contains multiple implementations of storage abstraction
used to access view data. It is taken from Lokad.CQRS and allows to
switch between Azure and Filesystem storage without rewriting the code.

See:

* IViewRoot and IViewContainer - actual interface
* ViewClient - helper wrapper arount IViewContainer, which can deal with
  transient connectivity errors
* AzureViewContainer - azure implementations of view storage
* FileViewContainer - file I/O implementations of view storage