using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Platform;
using Platform.Storage;
using ServiceStack.Text;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Continuous
{
    class Program
    {
        static void Main(string[] args)
        {
            var threads = new List<Task>();
            threads.Add(Task.Factory.StartNew(TagProjection, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness));
            threads.Add(Task.Factory.StartNew(CommentProjection, TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness));

            Task.WaitAll(threads.ToArray());
        }

        #region tag projection

        private static void TagProjection()
        {
            const int seconds = 1;
            var data = LoadTagData();
            Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                long nextOffcet = data.NextOffsetInBytes;
                Thread.Sleep(seconds * 1000);
                IInternalPlatformClient reader = new FilePlatformClient(@"C:\LokadData\dp-store");

                var records = reader.ReadAll(new StorageOffset(nextOffcet));
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

                if (!emptyData)
                {
                    Console.WriteLine("Next post offset: {0}", data.NextOffsetInBytes);
                    SaveTagData(data);
                }
            }
        }

        static TagsDistributionView LoadTagData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-tag-count.dat");

            if (!File.Exists(path))
                return new TagsDistributionView { NextOffsetInBytes = 0, Distribution = new Dictionary<string, long>() };

            return File.ReadAllText(path).FromJson<TagsDistributionView>();
        }

        static void SaveTagData(TagsDistributionView data)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-tag-count.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(data.ToJson());
            }
        }

        #endregion

        #region Comments

        private static void CommentProjection()
        {
            const int seconds = 1;
            var data = LoadCommentData();
            Console.WriteLine("Next comment offset: {0}", data.NextOffsetInBytes);
            while (true)
            {
                long nextOffcet = data.NextOffsetInBytes;
                Thread.Sleep(seconds * 1000);
                IInternalPlatformClient reader =
                    new FilePlatformClient(@"C:\LokadData\dp-store");

                var records = reader.ReadAll(new StorageOffset(nextOffcet));
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

                if (!emptyData)
                {
                    Console.WriteLine("Next comment offset: {0}", data.NextOffsetInBytes);
                    SaveCommentData(data);
                }
            }
        }

        static CommentDistributionView LoadCommentData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-comment.dat");

            if (!File.Exists(path))
                return new CommentDistributionView { NextOffsetInBytes = 0, Distribution = new Dictionary<long, int>() };

            return File.ReadAllText(path).FromJson<CommentDistributionView>();
        }

        static void SaveCommentData(CommentDistributionView data)
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-comment.dat");
            using (var sw = new StreamWriter(path, false))
            {
                sw.Write(data.ToJson());
            }
        }

        #endregion
    }

}
