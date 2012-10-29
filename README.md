Lokad Data Platform Sample
==========================

This is Data Platform sample from Lokad - showing principles of storing and processing large (GBytes and TBytes) data streams in the cloud. It is based on concepts from Lokad.CQRS, Lokad.Cloud and EventStore.

Some introductory information:

* [Introducing Data Platform](http://abdullin.com/journal/2012/10/20/introducing-lokad-data-platform.html)
* [Scalability targets of Data Platform](http://abdullin.com/journal/2012/10/20/scalability-targets-of-lokad-data-platform.html)

More documentation will be available, once we push through first production deployments internally.

Credits
-------

Thanks to the authors and maintainers of these projects for letting us stand on the shoulders of giants:

* [EventStore](http://geteventstore.com)
* [Service Stack](http://www.servicestack.net/) (and especially authors of async branch)
* [NLog](http://nlog-project.org/)
* [NUnit](http://www.nunit.org/)

Samples
-------

1. [Sample 1] (https://github.com/Lokad/lokad-data-platform/tree/master/SmartApp.Sample2.Continuous) - Sequential read event stream and shows "Next offset"
2. [Sample 2] (https://github.com/Lokad/lokad-data-platform/tree/master/SmartApp.Sample2.Continuous) - Show distribution of event by size
3. [Sample 3] (https://github.com/Lokad/lokad-data-platform/tree/master/SmartApp.Sample3.WebUI) - Show aggregated data from event steam.