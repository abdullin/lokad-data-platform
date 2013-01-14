using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Platform.StreamClients;
using Platform.ViewClients;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Continuous
{
    // See Readme.md in this project for the description of the sample
    class Program
    {
        const string PlatformPath = @"C:\LokadData\dp-store";


        static void Main(string[] args)
        {
            var store = PlatformClient.GetStreamReader(PlatformPath);
            var views = PlatformClient.GetViewClient(PlatformPath, Conventions.ViewContainer);
            views.CreateContainer();
            var threads = new List<Task>
                {
                    Task.Factory.StartNew(() => TagProjection(store, views),
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness),
                    Task.Factory.StartNew(() => CommentProjection(store, views),
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness),
                    Task.Factory.StartNew(() => UserCommentsPerDayDistributionProjection(store, views),
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
                };

            Task.WaitAll(threads.ToArray());
        }
        private static void TagProjection(IRawStreamClient store, ViewClient views)
        {
            var data = views.ReadAsJsonOrGetNew<TagsDistributionView>(TagsDistributionView.FileName);
            var processingInfo = views.ReadAsJsonOrGetNew<ProcessingInfoView>(TagsDistributionView.FileName + ".info");
            Console.WriteLine("Next post offset: {0}", processingInfo.NextOffsetInBytes);
            while (true)
            {
                var nextOffcet = processingInfo.NextOffsetInBytes;
                processingInfo.LastOffsetInBytes = processingInfo.NextOffsetInBytes;
                processingInfo.DateProcessingUtc = DateTime.UtcNow;

                var records = store.ReadAll(new StorageOffset(nextOffcet), 10000);
                var emptyData = true;
                foreach (var dataRecord in records)
                {
                    processingInfo.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    var post = Post.TryGetFromBinary(dataRecord.Data);
                    if (post == null)
                        continue;


                    foreach (var tag in post.Tags)
                    {
                        if (data.Distribution.ContainsKey(tag))
                            data.Distribution[tag]++;
                        else
                            data.Distribution[tag] = 1;
                    }
                    processingInfo.EventsProcessed += 1;

                    emptyData = false;
                }

                views.WriteAsJson(processingInfo, TagsDistributionView.FileName + ".info");

                if (emptyData)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    try
                    {
                        views.WriteAsJson(data, TagsDistributionView.FileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception on writing view - {0}\r\n{1}", TagsDistributionView.FileName, ex.Message);
                    }
                    Console.WriteLine("Next post offset: {0}", processingInfo.NextOffsetInBytes);
                }
            }
        }

        private static void CommentProjection(IRawStreamClient store, ViewClient views)
        {
            var data = views.ReadAsJsonOrGetNew<CommentDistributionView>(CommentDistributionView.FileName);
            var processingInfo = views.ReadAsJsonOrGetNew<ProcessingInfoView>(CommentDistributionView.FileName + ".info");
            Console.WriteLine("Next comment offset: {0}", processingInfo.NextOffsetInBytes);
            while (true)
            {
                var nextOffset = processingInfo.NextOffsetInBytes;
                processingInfo.LastOffsetInBytes = processingInfo.NextOffsetInBytes;
                processingInfo.DateProcessingUtc = DateTime.UtcNow;

                var records = store.ReadAll(new StorageOffset(nextOffset), 10000);
                var emptyData = true;
                foreach (var dataRecord in records)
                {
                    processingInfo.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;
                    processingInfo.EventsProcessed += 1;

                    var user = User.TryGetFromBinary(dataRecord.Data);
                    if (user != null)
                    {
                        data.Users[user.Id] = user;
                        emptyData = false;
                        continue;
                    }
                    var comment = Comment.TryGetFromBinary(dataRecord.Data);

                    if (comment != null)
                    {

                        if (data.Distribution.ContainsKey(comment.UserId))
                            data.Distribution[comment.UserId] += 1;
                        else
                            data.Distribution[comment.UserId] = 1;
                        emptyData = false;
                    }

                }

                views.WriteAsJson(processingInfo, CommentDistributionView.FileName + ".info");
                if (emptyData)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    try
                    {
                        views.WriteAsJson(data, CommentDistributionView.FileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception on writing view - {0}\r\n{1}", CommentDistributionView.FileName, ex.Message);
                    }
                    
                    Console.WriteLine("Next comment offset: {0}", processingInfo.NextOffsetInBytes);
                }
            }
        }

        private static void UserCommentsPerDayDistributionProjection(IRawStreamClient store, ViewClient views)
        {
            var data = views.ReadAsJsonOrGetNew<UserCommentsDistributionView>(UserCommentsDistributionView.FileName);
            var processingInfo =
                views.ReadAsJsonOrGetNew<ProcessingInfoView>(UserCommentsDistributionView.FileName + ".info");
            Console.WriteLine("Next user offset: {0}", processingInfo.NextOffsetInBytes);
            while (true)
            {
                var nextOffcet = processingInfo.NextOffsetInBytes;

                var records = store.ReadAll(new StorageOffset(nextOffcet), 10000);
                var emptyData = true;
                foreach (var dataRecord in records)
                {
                    processingInfo.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    var user = User.TryGetFromBinary(dataRecord.Data);
                    if (user != null)
                    {
                        data.Users[user.Id] = user;
                        emptyData = false;
                        continue;
                    }

                    var comment = Comment.TryGetFromBinary(dataRecord.Data);
                    if (comment != null)
                    {

                        if (!data.Distribution.ContainsKey(comment.UserId))
                        {
                            data.Distribution.Add(comment.UserId, new long[7]);
                        }

                        var dayOfWeek = (int) comment.CreationDate.Date.DayOfWeek;
                        data.Distribution[comment.UserId][dayOfWeek]++;

                        processingInfo.EventsProcessed += 1;

                        emptyData = false;
                    }
                }

                views.WriteAsJson(processingInfo, UserCommentsDistributionView.FileName + ".info");

                if (emptyData)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    try
                    {
                        views.WriteAsJson(data, UserCommentsDistributionView.FileName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Exception on writing view - {0}\r\n{1}", UserCommentsDistributionView.FileName, ex.Message);
                    }

                    Console.WriteLine("Next user offset: {0}", processingInfo.NextOffsetInBytes);
                }
            }
        }
    }
}
