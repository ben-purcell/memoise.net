using Memoise.Tests.Support;
using NUnit.Framework;

namespace Memoise.Tests.Unit
{
    [TestFixture]
    class OutAndRefTests
    {
        private OutAndRefTestable _testable;
        private IOutAndRefTestable _memoised;

        [SetUp]
        public void SetUp()
        {
            _testable = new OutAndRefTestable();

            _memoised = MemoiseFactory.Create<IOutAndRefTestable>(_testable);
        }

        [Test]
        public void OutParamsTest()
        {
            int a, b, c, d;

            _memoised.TrySomething("hello", out a);
            _memoised.TrySomething("goodbye", out b);
            _memoised.TrySomething("hello", out c);
            _memoised.TrySomething("goodbye", out d);

            Assert.That(a, Is.EqualTo("hello".GetHashCode()));
            Assert.That(b, Is.EqualTo("goodbye".GetHashCode()));
            Assert.That(c, Is.EqualTo("hello".GetHashCode()));
            Assert.That(d, Is.EqualTo("goodbye".GetHashCode()));

            Assert.That(_testable.TrySomethingCalls["hello"], Is.EqualTo(1));
            Assert.That(_testable.TrySomethingCalls["goodbye"], Is.EqualTo(1));
        }

        [Test]
        public void RefParamsTest()
        {
            var a = 0;
            var b = 0;
            var c = 0;
            var d = 0;

            _memoised.TrySomethingElse("hello", ref a);
            _memoised.TrySomethingElse("goodbye", ref b);
            _memoised.TrySomethingElse("hello", ref c);
            _memoised.TrySomethingElse("goodbye", ref d);

            Assert.That(a, Is.EqualTo("hello".GetHashCode()));
            Assert.That(b, Is.EqualTo("goodbye".GetHashCode()));
            Assert.That(c, Is.EqualTo("hello".GetHashCode()));
            Assert.That(d, Is.EqualTo("goodbye".GetHashCode()));

            Assert.That(_testable.TrySomethingElseCalls["hello"], Is.EqualTo(1));
            Assert.That(_testable.TrySomethingElseCalls["goodbye"], Is.EqualTo(1));
        }
    }
}
