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
            //TagProjection();
            CommentProjection();
        }

        #region tag projection

        private static void TagProjection()
        {
            const int seconds = 1;
            var data = LoadTagData();
            Console.WriteLine("Next post offset: {0}", data.NextOffset);
            while (true)
            {
                long nextOffcet = data.NextOffset;
                Thread.Sleep(seconds * 1000);
                IPlatformClient reader = new FilePlatformClient(@"C:\LokadData\dp-store");

                var records = reader.ReadAll(nextOffcet);
                bool emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffset = dataRecord.NextOffset;

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
                    Console.WriteLine("Next post offset: {0}", data.NextOffset);
                    SaveTagData(data);
                }
            }
        }

        static Sample3TagData LoadTagData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-tag-count.dat");

            if (!File.Exists(path))
                return new Sample3TagData { NextOffset = 0, Distribution = new Dictionary<string, long>() };

            return File.ReadAllText(path).FromJson<Sample3TagData>();
        }

        static void SaveTagData(Sample3TagData data)
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
            Console.WriteLine("Next comment offset: {0}", data.NextOffset);
            while (true)
            {
                long nextOffcet = data.NextOffset;
                Thread.Sleep(seconds * 1000);
                IPlatformClient reader =
                    new FilePlatformClient(@"C:\LokadData\dp-store");

                var records = reader.ReadAll(nextOffcet);
                bool emptyData = true;
                foreach (var dataRecord in records)
                {
                    data.NextOffset = dataRecord.NextOffset;

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
                    Console.WriteLine("Next comment offset: {0}", data.NextOffset);
                    SaveCommentData(data);
                }
            }
        }

        static Sample3CommentData LoadCommentData()
        {
            string path = Path.Combine(Directory.GetCurrentDirectory(), "sample3-comment.dat");

            if (!File.Exists(path))
                return new Sample3CommentData { NextOffset = 0, Distribution = new Dictionary<long, int>() };

            return File.ReadAllText(path).FromJson<Sample3CommentData>();
        }

        static void SaveCommentData(Sample3CommentData data)
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
