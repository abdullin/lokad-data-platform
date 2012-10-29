Lokad Data Plaform Sample #1
========

This sample is dead simple example of reading events stream.

Preparing
---------

0. Download dump of StackOverflow (SO) data from here http://media10.simplex.tv/content/xtendx/stu/stackoverflow/
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

1. Run Platform.Node.exe with administrative credentials from bin\server folder.
2. Run SmartApp.Sample3.Dump.exe from SmartApp.Sample3.Dump\bin\Debug to start converting StackOverflow data to events.


Sample
------

Run SmartApp.Sample1.Continuous.exe from SmartApp.Sample1.Continuous\bin\Debug
Application dumps to console window "Next offset" of event stream.
        Example output:
            [25.10.2012 13:50:10] Next offset(real data): Offset 1441598494b
            [25.10.2012 13:50:11] Next offset(real data): Offset 1456085908b
            [25.10.2012 13:50:13] Next offset(real data): Offset 1458470552b