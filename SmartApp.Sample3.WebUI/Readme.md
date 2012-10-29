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
                Starting everything. Press enter to initiate shutdown
                [08860,01,03:30:53.395] KillSwitch : -1
                HttpPort : 8080
                StoreLocation : C:\LokadData\dp-store
                [08860,10,03:30:53.474] Initializing
                [08860,10,03:30:53.689] Storage starting
                [08860,10,03:30:53.689] Storage ready
                [08860,10,03:30:53.692] Starting
                [08860,10,03:30:53.692] We are the master

1. Run Platform.Node.exe with administrative credentials from bin\server folder.

2. Run SmartApp.Sample3.Dump.exe from SmartApp.Sample3.Dump\bin\Debug to start converting StackOverflow data to events.
            
            Example output of Sample3.Dump
                Posts:
                        2154,86755111984 per second
                        Added 20000 posts
                Users:
                        24973,0849975076 per second
                        Added 240000 users
                Comments:
                        7975,45800039576 per second
                        Added 80000 posts
                Users:
                        25818,8020977499 per second
                        Added 260000 users

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

Sample 3 UI
--------
![Sample 3 Demo] (https://raw.github.com/Lokad/lokad-data-platform/master/SmartApp.Sample3.WebUI/Content/img/sample3.demo.png)