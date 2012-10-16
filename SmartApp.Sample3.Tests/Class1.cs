using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Tests
{
    public class CommentFixture
    {
        [Test]
        public void RoundTripForComment()
        {
            var source = new Comment()
                             {
                                 Id = 100,
                                 Text = "Comment for test",
                                 CreationDate = new DateTime(2011, 12, 1, 13, 13, 13),
                                 PostId = 12,
                                 Score = 2,
                                 UserId = 11
                             };

            var bin = source.ToBinary();
            var restored = Comment.FromBinary(bin);


            Assert.AreEqual(source.Id, restored.Id, "Id");
            Assert.AreEqual(source.Text, restored.Text, "Text");
            Assert.AreEqual(source.CreationDate, restored.CreationDate, "CreationDate");
            Assert.AreEqual(source.PostId, restored.PostId, "PostId");
            Assert.AreEqual(source.Score, restored.Score, "Score");
            Assert.AreEqual(source.UserId, restored.UserId, "UserId");
        }

        [Test]
        public void RoundTripForCommentEmptySource()
        {
            var source = new Comment();

            var bin = source.ToBinary();
            var restored = Comment.FromBinary(bin);


            Assert.AreEqual(source.Id, restored.Id, "Id");
            Assert.AreEqual(source.Text ?? "", restored.Text, "Text");
            Assert.AreEqual(source.CreationDate, restored.CreationDate, "CreationDate");
            Assert.AreEqual(source.PostId, restored.PostId, "PostId");
            Assert.AreEqual(source.Score, restored.Score, "Score");
            Assert.AreEqual(source.UserId, restored.UserId, "UserId");
        }

        [Test]
        public void RoundTripForPost()
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
            var restored = Post.FromBinary(bin);

            Assert.AreEqual(source.Id, restored.Id, "Id");
            Assert.AreEqual(source.AnswerCount, restored.AnswerCount, "AnswerCount");
            Assert.AreEqual(source.Body, restored.Body, "Body");
            Assert.AreEqual(source.CommentCount, restored.CommentCount, "CommentCount");
            Assert.AreEqual(source.CreationDate, restored.CreationDate, "CreationDate");
            Assert.AreEqual(source.FavoriteCount, restored.FavoriteCount, "FavoriteCount");
            Assert.AreEqual(source.LastEditDate, restored.LastEditDate, "LastEditDate");
            Assert.AreEqual(source.OwnerUserId, restored.OwnerUserId, "OwnerUserId");
            Assert.AreEqual(source.PostTypeId, restored.PostTypeId, "PostTypeId");
            Assert.AreEqual(source.Tags, restored.Tags, "Tags");
            Assert.AreEqual(source.Title, restored.Title, "Title");
            Assert.AreEqual(source.ViewCount, restored.ViewCount, "ViewCount");

        }

        [Test]
        public void RoundTripForPostEmptySource()
        {
            var source = new Post();

            var bin = source.ToBinary();
            var restored = Post.FromBinary(bin);

            Assert.AreEqual(source.Id, restored.Id, "Id");
            Assert.AreEqual(source.AnswerCount, restored.AnswerCount, "AnswerCount");
            Assert.AreEqual(source.Body ?? "", restored.Body, "Body");
            Assert.AreEqual(source.CommentCount, restored.CommentCount, "CommentCount");
            Assert.AreEqual(source.CreationDate, restored.CreationDate, "CreationDate");
            Assert.AreEqual(source.FavoriteCount, restored.FavoriteCount, "FavoriteCount");
            Assert.AreEqual(source.LastEditDate, restored.LastEditDate, "LastEditDate");
            Assert.AreEqual(source.OwnerUserId, restored.OwnerUserId, "OwnerUserId");
            Assert.AreEqual(source.PostTypeId, restored.PostTypeId, "PostTypeId");
            Assert.IsEmpty(restored.Tags, "Tags");
            Assert.AreEqual(source.Title ?? "", restored.Title, "Title");
            Assert.AreEqual(source.ViewCount, restored.ViewCount, "ViewCount");
        }
    }
}
