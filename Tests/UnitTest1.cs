using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /*
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TestShapeCollisionSame()
        {
            var rect1 = new Rect(0, 0, 10, 10);
            var rect2 = new Rect(0, 0, 10, 10);
            CollideData collideData = Helpers.shapesIntersect(rect1.toShape(), rect2.toShape());
            Assert.IsNotNull(collideData);
            Assert.AreEqual(4, collideData.intersectionPoints.Count);
            Shape shape = new Shape(collideData.intersectionPoints);
            Rect rect = shape.toRect();
            Assert.AreEqual(rect.area(), 100);
        }

        [TestMethod]
        public void TestShapeCollisionInside()
        {
            var rect1 = new Rect(0, 0, 10, 10);
            var rect2 = new Rect(5, 5, 10, 10);
            CollideData collideData = Helpers.shapesIntersect(rect1.toShape(), rect2.toShape());
            Assert.IsNotNull(collideData);
            Assert.AreEqual(4, collideData.intersectionPoints.Count);
            Shape shape = new Shape(collideData.intersectionPoints);
            Rect rect = shape.toRect();
            Assert.AreEqual(rect.area(), 25);
        }

        [TestMethod]
        public void TestShapeCollisionInside2()
        {
            var rect1 = new Rect(0, 0, 10, 10);
            var rect2 = new Rect(2, 5, 8, 10);
            CollideData collideData = Helpers.shapesIntersect(rect1.toShape(), rect2.toShape());
            Assert.IsNotNull(collideData);
            Assert.AreEqual(4, collideData.intersectionPoints.Count);
            Shape shape = new Shape(collideData.intersectionPoints);
            Rect rect = shape.toRect();
            Assert.AreEqual(rect.area(), 30);
        }
    }
    */
}
