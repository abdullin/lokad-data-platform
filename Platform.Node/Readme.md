# Platform.Node

This is actual data platform server, runnable as a console. You can configure 
it via command-line parameters. Run it with --help or /? to see command-line
options.

Important! Data Platform needs to use HTTP port (by default 8080), which usually
requires running it under administrative privileges.

See also:

* Messages folder - in-memory messages which define and drive the server
* Services folder - various services which implement actual server functionality
* Code files in this folder - core functionality related to running server,
  each file has its own comments
