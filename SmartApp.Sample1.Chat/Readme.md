Lokad Data Plaform Sample #1
========

Simple chat application using DataPlatform store for communication
and message exchange between multiple clients

Running sample
-------------

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


3. Run `..\bin\sample1\sample1.exe` to launch one chat window, enter your name and start typing messages.
4. Launch multiple chat windows and see how messages are delivered to other windows