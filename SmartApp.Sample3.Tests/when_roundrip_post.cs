using System;
using NUnit.Framework;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Tests
{
    public class when_roundrip_post
    {
        [Test]
        public void given_filled_post()
        {
            var source = new Post
                {
                    Id = 101,
                    AnswerCount = 4,
                    Body = "Post body test",
                    CommentCount = 5,
                    CreationDate = new DateTime(2011, 12, 1, 13, 13, 13),
                    FavoriteCount = 9,
                    LastEditDate = new DateTime(2012, 12, 1, 13, 13, 13),
                    OwnerUserId = 45,
                    PostTypeId = 23,
                    Tags = new[] { "c#", ".net", "java" },
                    Title = "Post title test",
                    ViewCount = 555
                };

            var bin = source.ToBinary();
            var restored = Post.TryGetFromBinary(bin);

            ShouldBeEqual(source, restored);
        }

        [Test]
        public void given_empty_post()
        {
            var source = new Post();

            var bin = source.ToBinary();
            var restored = Post.TryGetFromBinary(bin);

            ShouldBeEqual(source, restored);
        }

        static void ShouldBeEqual(Post source, Post restored)
        {
            Assert.AreEqual(source.Id, restored.Id, "Id");
            Assert.AreEqual(source.AnswerCount, restored.AnswerCount, "AnswerCount");
            Assert.AreEqual(source.Body ?? "", restored.Body, "Body");
            Assert.AreEqual(source.CommentCount, restored.CommentCount, "CommentCount");
            Assert.AreEqual(source.CreationDate, restored.CreationDate, "CreationDate");
            Assert.AreEqual(source.FavoriteCount, restored.FavoriteCount, "FavoriteCount");
            Assert.AreEqual(source.LastEditDate, restored.LastEditDate, "LastEditDate");
            Assert.AreEqual(source.OwnerUserId, restored.OwnerUserId, "OwnerUserId");
            Assert.AreEqual(source.PostTypeId, restored.PostTypeId, "PostTypeId");
            CollectionAssert.AreEqual(source.Tags, restored.Tags, "Tags");
            Assert.AreEqual(source.Title ?? "", restored.Title, "Title");
            Assert.AreEqual(source.ViewCount, restored.ViewCount, "ViewCount");
        }
    }
}