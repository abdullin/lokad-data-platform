Core/StreamClients 
================

This folder contains multiple implementations of clients used to access
running **data platform server**. This access involves using RestAPI to directly
send messages to the server and also access to the underlying storage in
order to upload event batches efficiently (leveraging cloud storage scalability)

See:

* StreamClient - helper wrapper arount IRawStreamClient, which can deal with
  transient connectivity errors. This is the one you should normally use
* IRawStreamClient - raw interface for manipulating the data platform

* AzureStreamClientContainer - azure implementations of stream client 
  (used to connect to server running on azure)
* FileViewContainer - file I/O implementations of stream client
  (is used to connect to data platform running on a local file system).
* JsonStreamClientBase - functionality shared between Azure and File clients.

Use PlatformClient in order to create an appropriate instance of stream client,
based on the configuration value.