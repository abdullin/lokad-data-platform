# Core/StreamStorage

Stream storage folder contains classes that are used to manipulate 
(read and write) actual underlying event storage directly. This is an
implementation of append-only storage which efficiently leverages
cloud storage capabilities.

See:

* Azure - for azure implementation of append-only storage
* Files - for file-based implementation of append-only storage

These classes are used in the code:

* by server to store incoming messages in separate containers
* by client to directly read messages stored by the server
* by client to create and save upload batches before handing them off
  to the server
