using System;
using NUnit.Framework;
using SmartApp.Sample3.Contracts;

namespace SmartApp.Sample3.Tests
{
    public class when_roundtrip_comment
    {
        [Test]
        public void given_populated_comment()
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
            var restored = Comment.TryGetFromBinary(bin);

            ShouldBeEqual(source, restored);
        }

        [Test]
        public void given_empty_comment()
        {
            var source = new Comment();

            var bin = source.ToBinary();
            var restored = Comment.TryGetFromBinary(bin);


            ShouldBeEqual(source, restored);
        }

        static void ShouldBeEqual(Comment source, Comment restored)
        {
            Assert.AreEqual(source.Id, restored.Id, "Id");
            Assert.AreEqual(source.Text ?? "", restored.Text, "Text");
            Assert.AreEqual(source.CreationDate, restored.CreationDate, "CreationDate");
            Assert.AreEqual(source.PostId, restored.PostId, "PostId");
            Assert.AreEqual(source.Score, restored.Score, "Score");
            Assert.AreEqual(source.UserId, restored.UserId, "UserId");
        }
    }
}
