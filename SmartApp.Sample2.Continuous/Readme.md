Lokad Data Platform Sample - Smart App #2
========

This smart app calculates distribution of events in events stream by length, 
showing it to the console. As more data comes in - calculations will be updated.

It needs some data to come into the data platform. You can use either 
Stack Overflow dump tool from Sample 3 or manually add events to the store 
with TestClient.

Preparing
---------

0. Download dump of [StackOverflow (SO) data](http://media10.simplex.tv/content/xtendx/stu/stackoverflow/). 

1. Run `bin\server folder\Platform.Node.exe` as administrator:

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

2. Run `SmartApp.Sample3.Dump.exe` from SmartApp.Sample3.Dump\bin\Debug to 
start converting StackOverflow data to events.

        Example output of Sample3.Dump
            Users:
                    15280,4859255646 per second
                    Added 40000 users
            Comments:
                    7451,20281786608 per second
                    Added 20000 posts
            Users:
                    18767,8781634393 per second
                    Added 60000 users
            Users:
                    21466,4217133285 per second
                    Added 80000 users


Sample
------

Run `SmartApp.Sample2.Continuous.exe` from `SmartApp.Sample2.Continuous\bin\Debug`
Application dumps to console window distribution of events in events stream by length.

        Example output:
            [324]: 1419
            [283]: 1497
            [174]: 2807
            [65]: 3222
            [293]: 1433