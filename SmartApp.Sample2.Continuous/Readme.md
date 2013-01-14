Lokad Data Plaform Sample - Smart App #2
========

This smart app calculates distribution of events in events stream by length, 
showing it to the console. As more data comes in - calculations will be updated.

Preparing
---------

0. Run `bin\server folder\Platform.Node.exe` as administrator:

        Example output of Platform.Node
            [08448,01,03:20:32.083] KillSwitch : -1
            HttpPort : 8080
            StoreLocation : C:\LokadData\dp-store
            Starting everything. Press enter to initiate shutdown
            [08448,10,03:20:32.163] Initializing
            [08448,10,03:20:32.530] Storage starting
            [08448,10,03:20:32.530] Storage ready
            [08448,10,03:20:32.530] Starting
            [08448,10,03:20:32.530] We are the master

1. Launch one or more chat windows from Sample one (`..\bin\sample1\sample1.exe`) 
and start sending some messages.


Running the sample
------

Run `..bin\sample2\sample2.exe` and watch it update event size distribution, as more events are
pushed to data platform.

        Example output:
            [324]: 1419
            [283]: 1497
            [174]: 2807
            [65]: 3222
            [293]: 1433

Restart the application to see how it picks up at the end of the stream (no reprocessing of the entire stream).