using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using ServiceStack.Text;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Continuous
{
    class Program
    {
        const string config = @"C:\LokadData\dp-store";


        static void Main(string[] args)
        {
            var store = PlatformClient.StreamClient(config,null);
            var views = PlatformClient.ViewClient(config).GetContainer(Conventions.ViewContainer).Create();
            var threads = new List<Task>
                {
                    Task.Factory.StartNew(() => TagProjection(store, views),
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness),
                    Task.Factory.StartNew(() => CommentProjection(store, views),
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
                };

            Task.WaitAll(threads.ToArray());
        }

        #region tag projection

        private static void TagProjection(IInternalStreamClient store, IViewContainer views)
        {
            var data = LoadTagData(views);
            Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                long nextOffcet = data.NextOffsetInBytes;

                var records = store.ReadAll(new StorageOffset(nextOffcet), 50000);
                bool emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    if (dataRecord.Key != "s3:post")
                        continue;

                    var post = Post.FromBinary(dataRecord.Data);
                    if (post == null)
                        continue;

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
                SaveTagData(data, views);

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

        static TagsDistributionView LoadTagData(IViewContainer views)
        {
            if (!views.Exists(TagsDistributionView.FileName))
                return new TagsDistributionView();

            using (var stream = views.OpenRead(TagsDistributionView.FileName))
            {
                return JsonSerializer.DeserializeFromStream<TagsDistributionView>(stream);
            }
        }

        static void SaveTagData(TagsDistributionView data, IViewContainer views)
        {
            using (var stream = views.OpenWrite(TagsDistributionView.FileName))
            {
                JsonSerializer.SerializeToStream(data, stream);
            }
        }

        #endregion

        #region Comments

        private static void CommentProjection(IInternalStreamClient store, IViewContainer views)
        {
            var data = LoadCommentData(views);
            Console.WriteLine("Next comment offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                long nextOffcet = data.NextOffsetInBytes;
                IInternalStreamClient reader =
                    store;

                var records = reader.ReadAll(new StorageOffset(nextOffcet), 10000);
                bool emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffsetInBytes = dataRecord.Next.OffsetInBytes;

                    if (dataRecord.Key != "s3:comment")
                        continue;

                    var comment =Comment.FromBinary(dataRecord.Data);
                    if (comment == null)
                        continue;


                    if (data.Distribution.ContainsKey(comment.UserId))
                        data.Distribution[comment.UserId]++;
                    else
                        data.Distribution[comment.UserId] = 1;

                    data.EventsProcessed += 1;

                    emptyData = false;
                }
                SaveCommentData(data, views);

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

        static CommentDistributionView LoadCommentData(IViewContainer views)
        {

            if (!views.Exists(CommentDistributionView.FileName))
                return new CommentDistributionView();

            using (var stream = views.OpenRead(CommentDistributionView.FileName))
            {
                return JsonSerializer.DeserializeFromStream<CommentDistributionView>(stream);
            }
        }

        static void SaveCommentData(CommentDistributionView data, IViewContainer views)
        {
            var exes = new Stack<Exception>();
            while(true)
            {
                    try
                {
                    using (var stream = views.OpenWrite(CommentDistributionView.FileName))
                    {
                        JsonSerializer.SerializeToStream(data, stream);
                        return;
                    }
                }
                catch (IOException e)
                {
                    exes.Push(e);
                    if (exes.Count >= 4)
                        throw new AggregateException(exes);

                    Thread.Sleep(200 * exes.Count);
                }
            }

        }

        #endregion
    }

}
