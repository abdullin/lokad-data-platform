using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Web;
using Platform;
using Platform.Messages;
using ServiceStack.ServiceClient.Web;
using ServiceStack.Text;

namespace SmartApp.Sample3.Dump
{
    class Program
    {
        private static IPlatformClient _reader;
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

        static void Main(string[] args)
        {
           var httpBase = string.Format("http://127.0.0.1:8080");
            _reader = new FilePlatformClient(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\Platform.Node\bin\Debug\store"),httpBase);
            Thread.Sleep(2000); //waiting for server initialization

            DumpPosts();
        }

        private static void DumpPosts()
        {
            var path = @"D:\Temp\Stack Overflow Data Dump - Aug 09\Content\posts.xml";

            long rowIndex = 0;

            Stopwatch sw = new Stopwatch();
            sw.Start();

            var jsonBytes = new List<byte[]>();
            foreach (var line in ReadLinesSequentially(path).Where(l => l.StartsWith("  <row ")))
            {
                rowIndex++;
                var json = ConvertToJson(line);
                if (json == null)
                    continue;

                var bytes = new List<byte>(Encoding.UTF8.GetBytes(json));
                bytes.Insert(0, 43); //flag for our example

                jsonBytes.Add(bytes.ToArray());

                if (rowIndex % 20000 == 0)
                {
                    _reader.ImportBatch("Post", jsonBytes.Select(x => new RecordForStaging(x)).ToList());
                    Console.WriteLine("Posts:\r\n\t{0} per second\r\n\tAdded {1} posts", rowIndex / sw.Elapsed.TotalSeconds, rowIndex);
                }
            }
        }

        private static string ConvertToJson(string line)
        {
            try
            {
                long defaultLong;
                DateTime defaultDate;
                var json = new Post
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

                return json.ToJson();
            }
            catch (Exception)
            {
                return null;
            }

        }

        private static string Get(string line, string attributeName)
        {
            var start = line.IndexOf(attributeName + "=\"");
            var end = line.Substring(start + attributeName.Length + 2).IndexOf("\"");

            if (start == -1 || end == -1)
                return "";

            return line.Substring(start + attributeName.Length + 2, end);
        }

        private static string GetTmpFilePath()
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "tmp-files");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            return Path.Combine(path, Guid.NewGuid() + ".tmp");
        }
    }

    struct Comment
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long UserId { get; set; }
        public DateTime CreateDate { get; set; }
        public string Text { get; set; }
    }

    public class Post
    {
        public long Id { get; set; }
        public long PostTypeId { get; set; }
        public DateTime CreationDate { get; set; }
        public long ViewCount { get; set; }
        public string Body { get; set; }
        public long OwnerUserId { get; set; }
        public DateTime LastEditDate { get; set; }
        public string Title { get; set; }
        public long AnswerCount { get; set; }
        public long CommentCount { get; set; }
        public long FavoriteCount { get; set; }
        public string[] Tags { get; set; }
    }
}
