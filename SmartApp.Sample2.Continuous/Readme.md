Lokad Data Plaform Sample - Smart App #2
========

This smart app calculates distribution of events in events stream by length, 
showing it to the console. As more data comes in - calculations will be updated.

Running the sample
---------
1. Build solution
2. Start DataPlatform server by running `..\bin\server\Platform.Node.exe` as administrator.

         Example output of Platform.Node
            [09528,01,07:51:06.535] KillSwitch : -1
            HttpPort : 8080
            StoreLocation : C:\LokadData\dp-store
            Starting everything. Press enter to initiate shutdown
            [09528,10,07:51:06.636] Initializing
            [09528,10,07:51:06.914] Storage starting
            [09528,10,07:51:06.914] Storage ready
            [09528,10,07:51:06.914] Starting
            [09528,10,07:51:06.914] We are the master


3. Launch one or more chat windows from Sample one (`..\bin\sample1\sample1.exe`) 
and start sending some messages.

4. Run `..bin\sample2\sample2.exe` and watch it update event size distribution, as more events are
pushed to data platform.

        Example output:
            [324]: 1419
            [283]: 1497
            [174]: 2807
            [65]: 3222
            [293]: 1433

5. Restart the application to see how it picks up at the end of the stream (no reprocessing of the entire stream).