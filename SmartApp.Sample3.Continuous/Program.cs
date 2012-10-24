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
    class Program
    {
        const string platformPath = @"C:\LokadData\dp-store";


        static void Main(string[] args)
        {
            var store = PlatformClient.GetStreamReader(platformPath);
            var views = PlatformClient.GetViewClient(platformPath, Conventions.ViewContainer);
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
        private static void TagProjection(IInternalStreamClient store, ViewClient views)
        {
            var data = views.ReadAsJsonOrGetNew<TagsDistributionView>(TagsDistributionView.FileName);
            Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                var records = store.ReadAll(new StorageOffset(data.NextOffsetInBytes), 50000);
                var emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    if (dataRecord.Key != "s3:post")
                        continue;

                    var post = Post.FromBinary(dataRecord.Data);

                    foreach (var tag in post.Tags)
                    {
                        if (data.Distribution.ContainsKey(tag))
                            data.Distribution[tag]++;
                        else
                            data.Distribution[tag] = 1;
                    }
                    data.EventsProcessed += 1;

                    emptyData = false;
                }
                views.WriteAsJson(data, TagsDistributionView.FileName);

                if (emptyData)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
                }
            }
        }

        
        private static void CommentProjection(IInternalStreamClient store, ViewClient views)
        {
            var data = views.ReadAsJsonOrGetNew<CommentDistributionView>(CommentDistributionView.FileName);
            Console.WriteLine("Next comment offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                var nextOffset = data.NextOffsetInBytes;

                var records = store.ReadAll(new StorageOffset(nextOffset), 10000);
                var emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    if (dataRecord.Key != "s3:comment")
                        continue;

                    var comment = Comment.FromBinary(dataRecord.Data);

                    if (data.Distribution.ContainsKey(comment.UserId))
                        data.Distribution[comment.UserId] += 1;
                    else
                        data.Distribution[comment.UserId] = 1;

                    data.EventsProcessed += 1;

                    emptyData = false;
                }
                views.WriteAsJson(data, CommentDistributionView.FileName);

                if (emptyData)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    Console.WriteLine("Next comment offset: {0}", data.NextOffsetInBytes);
                }
            }
        }

        private static void UserCommentsPerDayDistributionProjection(IInternalStreamClient store, ViewClient views)
        {
            var data = views.ReadAsJsonOrGetNew<UserCommentsDistributionView>(UserCommentsDistributionView.FileName);
            Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                var nextOffcet = data.NextOffsetInBytes;

                var records = store.ReadAll(new StorageOffset(nextOffcet), 10000);
                var emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    if (dataRecord.Key == "s3:user")
                    {
                        var user = User.FromBinary(dataRecord.Data);
                        data.UserNames[user.Id] = user.Name;
                        emptyData = false;
                    }

                    if (dataRecord.Key != "s3:comment") continue;

                    var comment = Comment.FromBinary(dataRecord.Data);

                    if (!data.Distribution.ContainsKey(comment.UserId))
                    {
                        data.Distribution.Add(comment.UserId, new long[7]);
                    }

                    var dayOfWeek = (int)comment.CreationDate.Date.DayOfWeek;
                    data.Distribution[comment.UserId][dayOfWeek]++;

                    data.EventsProcessed += 1;

                    emptyData = false;
                }

                views.WriteAsJson(data, UserCommentsDistributionView.FileName);

                if (emptyData)
                {
                    Thread.Sleep(1000);
                }
                else
                {
                    Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
                }
            }
        }
    }
}
