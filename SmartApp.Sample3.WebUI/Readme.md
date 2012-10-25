Lokad Data Plaform Sample #3
========

Application demonstrate projections work. Now it contains 

    1. TagProjection - Post aggregation by tags
    2. CommentProjection - Comments aggregation by user
    3. UserCommentsPerDayDistributionProjection - User comments detstribution by day of week

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

1. Run Platform.Node.exe with administrative credentials from /bin/server folder.
2. Run SmartApp.Sample3.Dump.exe from SmartApp.Sample3.Dump\bin\Debug to start converting StackOverflow data to events.


Sample
------

Run SmartApp.Sample3.Continuous.exe from SmartApp.Sample3.Continuous\bin\Debug
Application start events processing

        Example output:
            Next comment offset: 0
            Next post offset: 0
            Next user offset: 382904
            Next comment offset: 382904
            Next comment offset: 759473
            Next user offset: 759473
            Next comment offset: 1135234
            Next user offset: 1135234

To view Results run web application SmartApp.Sample3.WebUI and open site. 

        Example:
            http://localhost:50438/