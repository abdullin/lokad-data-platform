# Platform.Core 

This is Core library for the Lokad Data Platform, which also acts as 
a storage client library. Some code in this library is used by Data Platform
server in Platform.Node

See:

* StreamClients folder - client code used to access streams via platform API
* StreamStorage folder - implementations of append-only storage used by both
  server and client
* ViewClients folder - client code used to read/write views stored on a data platform