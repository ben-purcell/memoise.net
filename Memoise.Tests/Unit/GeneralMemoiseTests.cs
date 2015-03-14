using Memoise.Tests.Support;
using Moq;
using NUnit.Framework;

namespace Memoise.Tests.Unit
{
    [TestFixture]
    public class GeneralMemoiseTests
    {
        private Mock<ITestable> _mockTestable;

        [SetUp]
        public void SetUp()
        {
            _mockTestable = new Mock<ITestable>();
        }

        [Test]
        public void NoReturnValueTests()
        {
            var memoised = MemoiseFactory.Create<ITestable>(_mockTestable.Object);

            memoised.Blah("hello");
            memoised.Blah("hello");
            memoised.Blah("hello");

            _mockTestable.Verify(x => x.Blah("hello"), Times.Exactly(3));
        }

        [Test]
        public void SingleParamTests()
        {
            _mockTestable
                .Setup(x => x.MethodA(It.IsAny<int>()))
                .Returns<int>(x => x * x);

            var memoised = MemoiseFactory.Create<ITestable>(_mockTestable.Object);

            var results = new []
            {
                memoised.MethodA(2),
                memoised.MethodA(4),
                memoised.MethodA(2),
                memoised.MethodA(4)
            };

            _mockTestable.Verify(x => x.MethodA(2), Times.Exactly(1));
            _mockTestable.Verify(x => x.MethodA(4), Times.Exactly(1));

            Assert.That(results, Is.EqualTo(new[] { 4, 16, 4, 16 }));
        }

        [Test]
        public void TwoParamTests()
        {
            _mockTestable
                .Setup(x => x.MethodB(It.IsAny<int>(), It.IsAny<int>()))
                .Returns<int, int>((a, b) => a + b);

            var memoised = MemoiseFactory.Create<ITestable>(_mockTestable.Object);

            var results = new []
            {
                memoised.MethodB(5, 6),
                memoised.MethodB(2, 2),
                memoised.MethodB(5, 6),
                memoised.MethodB(2, 2)
            };

            _mockTestable.Verify(x => x.MethodB(5, 6), Times.Once);
            _mockTestable.Verify(x => x.MethodB(2, 2), Times.Once);

            Assert.That(results, Is.EqualTo(new[] { 11, 4, 11, 4 }));
        }

        [Test]
        public void ThreeParamTests()
        {
            _mockTestable
                .Setup(x => x.MethodC(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns<int, int, int>((a, b, c) => a + b + c);

            var memoised = MemoiseFactory.Create<ITestable>(_mockTestable.Object);

            var results = new []
            {
                memoised.MethodC(5, 6, 7),
                memoised.MethodC(8, 9, 10),
                memoised.MethodC(5, 6, 7),
                memoised.MethodC(8, 9, 10)
            };

            _mockTestable.Verify(x => x.MethodC(5, 6, 7), Times.Once);
            _mockTestable.Verify(x => x.MethodC(8, 9, 10), Times.Once);

            Assert.That(results, Is.EqualTo(new[] { 18, 27, 18, 27 }));
        }
    }
}
