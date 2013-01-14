using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Platform;
using Platform.StreamClients;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Dump
{
    class Program
    {
        public static string RawDataPath;
        public static string StorePath;
        public static string StoreConnection;
        static void Main()
        {
            RawDataPath = ConfigurationManager.AppSettings["RawDataPath"];
            StorePath = ConfigurationManager.AppSettings["StorePath"];
            StoreConnection = ConfigurationManager.AppSettings["StoreConnection"];

            Console.WriteLine("This is Sample3.Dump tool");
            Console.WriteLine("Using settings from the .config file");
            Console.WriteLine("  RawDataPath (put stack overflow dump here): {0}", RawDataPath);
            Console.WriteLine("  StoreDataPath: {0}", StorePath);
            Console.WriteLine("  StoreConnection: {0}", StoreConnection);
            
            _reader =  PlatformClient.GetStreamReaderWriter(StorePath, StoreConnection);
            Thread.Sleep(2000); //waiting for server initialization

            var threads = new List<Task>
                {
                    Task.Factory.StartNew(DumpComments,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness),
                    Task.Factory.StartNew(DumpPosts,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness),
                    Task.Factory.StartNew(DumpUsers,
                        TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)
                };

            Task.WaitAll(threads.ToArray());
        }

        private static IRawStreamClient _reader;

        static IEnumerable<string> ReadLinesSequentially(string path)
        {
            using (var rows = File.OpenText(path))
            {
                while (true)
                {
                    var line = rows.ReadLine();
                    if (null != line)
                    {
                        yield return line;
                    }
                    else
                    {
                        yield break;
                    }
                }
            }
        }


        private static void DumpComments()
        {
            var path = Path.Combine(RawDataPath, "comments.xml");

            var sw = Stopwatch.StartNew();
            var buffer = new List<byte[]>(20000);
            int total = 0;
            foreach (var line in ReadLinesSequentially(path).Where(l => l.StartsWith("  <row ")))
            {
                total += 1;
                var comment = ParseComments(line);
                if (comment == null)
                    continue;
                buffer.Add(comment.ToBinary());
                
                if (buffer.Count == buffer.Capacity)
                {
                    _reader.WriteEventsInLargeBatch("", buffer.Select(x => new RecordForStaging(x)));
                    buffer.Clear();

                    var speed = total / sw.Elapsed.TotalSeconds;
                    Console.WriteLine("Comments:\r\n\t{0} per second\r\n\tAdded {1} posts", speed, total);
                }
                
                
            }
            _reader.WriteEventsInLargeBatch("", buffer.Select(x => new RecordForStaging(x)));
            Console.WriteLine("Comments import complete");
        }

        private static Comment ParseComments(string line)
        {
            try
            {
                long defaultLong;
                int defaultInt;
                DateTime defaultDate;

                var comment = new Comment
                    {
                        Id = long.TryParse(Get(line, "Id"), out defaultLong) ? defaultLong : -1,
                        PostId = long.TryParse(Get(line, "PostId"), out defaultLong) ? defaultLong : -1,
                        CreationDate =
                            DateTime.TryParse(Get(line, "CreationDate"), out defaultDate)
                                ? defaultDate
                                : DateTime.MinValue,
                        Text = HttpUtility.HtmlDecode(Get(line, "Text")),
                        UserId = long.TryParse(Get(line, "UserId"), out defaultLong) ? defaultLong : -1,
                        Score = int.TryParse(Get(line, "Score"), out defaultInt) ? defaultInt : -1,
                    };

                return comment;
            }
            catch (Exception)
            {
                return null;
            }
        }

        
        private static void DumpPosts()
        {
            var path = Path.Combine(RawDataPath, "posts.xml");


            var sw = Stopwatch.StartNew();

            var buffer = new List<byte[]>(20000);
            int total = 0;
            foreach (var line in ReadLinesSequentially(path).Where(l => l.StartsWith("  <row ")))
            {
                total += 1;
                var post = PostParse(line);
                if (post == null)
                    continue;

                buffer.Add(post.ToBinary());

                if (buffer.Count == buffer.Capacity)
                {
                    _reader.WriteEventsInLargeBatch("", buffer.Select(x => new RecordForStaging(x)));
                    buffer.Clear();
                    var speed = total / sw.Elapsed.TotalSeconds;
                    Console.WriteLine("Posts:\r\n\t{0} per second\r\n\tAdded {1} posts", speed, total);
                }
            }
            _reader.WriteEventsInLargeBatch("s3:post", buffer.Select(x => new RecordForStaging(x)));
            Console.WriteLine("Posts import complete");
        }

        private static Post PostParse(string line)
        {
            try
            {
                long defaultLong;
                DateTime defaultDate;
                var post = new Post
                {
                    Id = long.TryParse(Get(line, "Id"), out defaultLong) ? defaultLong : -1,
                    PostTypeId = long.TryParse(Get(line, "PostTypeId"), out defaultLong) ? defaultLong : -1,
                    CreationDate = DateTime.TryParse(Get(line, "CreationDate"), out defaultDate) ? defaultDate : DateTime.MinValue,
                    ViewCount = long.TryParse(Get(line, "ViewCount"), out defaultLong) ? defaultLong : -1,
                    Body = HttpUtility.HtmlDecode(Get(line, "Body")),
                    OwnerUserId = long.TryParse(Get(line, "OwnerUserId"), out defaultLong) ? defaultLong : -1,
                    LastEditDate = DateTime.TryParse(Get(line, "LastEditDate"), out defaultDate) ? defaultDate : DateTime.MinValue,
                    Title = HttpUtility.HtmlDecode(Get(line, "Title")),
                    AnswerCount = long.TryParse(Get(line, "AnswerCount"), out defaultLong) ? defaultLong : -1,
                    CommentCount = long.TryParse(Get(line, "CommentCount"), out defaultLong) ? defaultLong : -1,
                    FavoriteCount = long.TryParse(Get(line, "FavoriteCount"), out defaultLong) ? defaultLong : -1,
                    Tags = (">" + HttpUtility.HtmlDecode(Get(line, "Tags")) + "<").Split(new[] { "><" }, StringSplitOptions.RemoveEmptyEntries)
                };

                return post;
            }
            catch (Exception)
            {
                return null;
            }

        }

        private static void DumpUsers()
        {
            var path = Path.Combine(RawDataPath, "users.xml");

            var sw = Stopwatch.StartNew();

            var buffer = new List<byte[]>(20000);
            int total = 0;
            foreach (var line in ReadLinesSequentially(path).Where(l => l.StartsWith("  <row ")))
            {
                total += 1;
                var user = UserParse(line);

                if (user == null)
                    continue;

                buffer.Add(user.ToBinary());

                if (buffer.Count == buffer.Capacity)
                {
                    _reader.WriteEventsInLargeBatch("s3:user", buffer.Select(x => new RecordForStaging(x)));
                    buffer.Clear();
                    var speed = total/sw.Elapsed.TotalSeconds;
                    Console.WriteLine("Users:\r\n\t{0} per second\r\n\tAdded {1} users", speed, total);
                }
            }

            _reader.WriteEventsInLargeBatch("s3:user", buffer.Select(x => new RecordForStaging(x)));
            Console.WriteLine("Users import complete");
        }

        static User UserParse(string line)
        {
            try
            {
                long defaultLong;
                var user = new User
                    {
                        Id = long.TryParse(Get(line, "Id"), out defaultLong) ? defaultLong : -1,
                        Name = HttpUtility.HtmlDecode(Get(line, "DisplayName")),
                        Reputation = long.TryParse(Get(line, "Reputation"), out defaultLong) ? defaultLong : -1
                    };

                return user;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string Get(string line, string attributeName)
        {
            var start = line.IndexOf(attributeName + "=\"");
            var startOffset = start + attributeName.Length + 2;
            var end = line.IndexOf("\"", startOffset);

            if (start == -1 || end == -1)
                return "";

            return line.Substring(startOffset, end - startOffset);
        }
    }
}