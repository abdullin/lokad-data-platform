using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartApp.Sample3.Contracts
{
    public class Sample3TagData
    {
        public long NextOffset { get; set; }
        public int EventsProcessed { get; set; }

        public Dictionary<string, long> Distribution { get; set; }
    }

    public class Sample3CommentData
    {
        public long NextOffset { get; set; }
        public int EventsProcessed { get; set; }

        public Dictionary<long, int> Distribution { get; set; }
    }



    public class Comment
    {
        public long Id { get; set; }
        public long PostId { get; set; }
        public long UserId { get; set; }
        public DateTime CreationDate { get; set; }
        public string Text { get; set; }
        public int Score { get; set; }

        private const int Signature = 4343;

        public byte[] ToBinary()
        {
            using (var mem = new MemoryStream())
            using (var bin = new BinaryWriter(mem))
            {
                bin.Write(Signature);
                bin.Write(Id);
                bin.Write(PostId);
                bin.Write(UserId);
                bin.Write(CreationDate.ToBinary());
                bin.Write(Text ?? "");
                bin.Write(Score);
                return mem.ToArray();
            }
        }

        public static Comment FromBinary(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            using (var bin = new BinaryReader(mem))
            {
                var retrieved = bin.ReadInt32();
                if (retrieved != Signature)
                    throw new InvalidOperationException("Signature mismatch");

                return new Comment()
                           {
                               Id = bin.ReadInt64(),
                               PostId = bin.ReadInt64(),
                               UserId = bin.ReadInt64(),
                               CreationDate = DateTime.FromBinary(bin.ReadInt64()),
                               Text = bin.ReadString(),
                               Score = bin.ReadInt32()
                           };
            }
        }
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

        private const int Signature = 4344;

        public byte[] ToBinary()
        {
            using (var mem = new MemoryStream())
            using (var bin = new BinaryWriter(mem))
            {
                bin.Write(Signature);
                bin.Write(Id);
                bin.Write(PostTypeId);
                bin.Write(CreationDate.ToBinary());
                bin.Write(ViewCount);
                bin.Write(Body ?? "");
                bin.Write(OwnerUserId);
                bin.Write(LastEditDate.ToBinary());
                bin.Write(Title ?? "");
                bin.Write(AnswerCount);
                bin.Write(CommentCount);
                bin.Write(FavoriteCount);
                bin.Write(Tags == null ? 0 : Tags.Length);
                if (Tags != null)
                    foreach (var tag in Tags)
                    {
                        bin.Write(tag);
                    }

                return mem.ToArray();
            }
        }

        public static Post FromBinary(byte[] data)
        {
            using (var mem = new MemoryStream(data))
            using (var bin = new BinaryReader(mem))
            {
                var retrieved = bin.ReadInt32();
                if (retrieved != Signature)
                    throw new InvalidOperationException("Signature mismatch");

                var post = new Post
                {
                    Id = bin.ReadInt64(),
                    PostTypeId = bin.ReadInt64(),
                    CreationDate = DateTime.FromBinary(bin.ReadInt64()),
                    ViewCount = bin.ReadInt64(),
                    Body = bin.ReadString(),
                    OwnerUserId = bin.ReadInt64(),
                    LastEditDate = DateTime.FromBinary(bin.ReadInt64()),
                    Title = bin.ReadString(),
                    AnswerCount = bin.ReadInt64(),
                    CommentCount = bin.ReadInt64(),
                    FavoriteCount = bin.ReadInt64()
                };

                List<string> tags = new List<string>();
                var tagCount = bin.ReadInt32();
                for (int i = 0; i < tagCount; i++)
                {
                    tags.Add(bin.ReadString());
                }

                post.Tags = tags.ToArray();

                return post;
            }
        }
    }

}
