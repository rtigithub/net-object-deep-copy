﻿#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Runtime.CompilerServices;

#endregion

namespace unittests
{
    [TestClass]
    public class ObjectExtensionsTests
    {
        private class MySingleObject
        {
            public string One;
            private int two;

            public int Two
            {
                get { return two; }
                set { two = value; }
            }
        }

        private class MyNestedObject
        {
            public MySingleObject Single;
            public string Meta;
        }

        private class OverriddenHash
        {
            public override int GetHashCode()
            {
                return 42;
            }
        }

        /// <summary>
        /// Encapsulates an object, the container will always be seen as a mutable ref type.
        /// Simplifies testing deepcopying.
        /// </summary>
        /// <typeparam name="T">Type to be encapsulated</typeparam>
        private class Wrapper<T> : IEquatable<Wrapper<T>>
        {
            public T Value{ get; set; }

            public override bool Equals(object obj)
            {
                return Equals(obj as Wrapper<T>);
            }

            public bool Equals(Wrapper<T> other)
            {
                return other != null &&
                       EqualityComparer<T>.Default.Equals(Value, other.Value);
            }

            public override int GetHashCode()
            {
                return -1937169414 + EqualityComparer<T>.Default.GetHashCode(Value);
            }

            public override string ToString()
            {
                return Value.ToString();
            }
        }

        [TestMethod]
        public void Copy_XElementWithChildren()
        {
            XElement el = XElement.Parse(@"
                <root>
                    <child attrib='wow'>hi</child>
                    <child attrib='yeah'>hello</child>
                </root>");
            XElement copied = el.Copy();

            var children = copied.Elements("child").ToList();
            Assert.AreEqual(2, children.Count);
            Assert.AreEqual("wow", children[0].Attribute("attrib").Value);
            Assert.AreEqual("hi", children[0].Value);

            Assert.AreEqual("yeah", children[1].Attribute("attrib").Value);
            Assert.AreEqual("hello", children[1].Value);
        }

        [TestMethod]
        public void Copy_CopiesNestedObject()
        {
            MyNestedObject copied =
                new MyNestedObject() {Meta = "metadata", Single = new MySingleObject() {One = "single_one"}}.Copy();

            Assert.AreEqual("metadata", copied.Meta);
            Assert.AreEqual("single_one", copied.Single.One);
        }

        [TestMethod]
        public void Copy_CopiesEnumerables()
        {
            IList<MySingleObject> list = new List<MySingleObject>()
            {
                new MySingleObject() {One = "1"},
                new MySingleObject() {One = "2"}
            };
            IList<MySingleObject> copied = list.Copy();

            Assert.AreEqual(2, copied.Count);
            Assert.AreEqual("1", copied[0].One);
            Assert.AreEqual("2", copied[1].One);
        }

        [TestMethod]
        public void Copy_CopiesSingleObject()
        {
            MySingleObject copied = new MySingleObject() {One = "one", Two = 2}.Copy();

            Assert.AreEqual("one", copied.One);
            Assert.AreEqual(2, copied.Two);
        }

        [TestMethod]
        public void Copy_CopiesSingleBuiltInObjects()
        {
            Assert.AreEqual("hello there", "hello there".Copy());
            Assert.AreEqual(123, 123.Copy());
        }

        [TestMethod]
        public void Copy_CopiesSelfReferencingArray()
        {
            object[] arr = new object[1];
            arr[0] = arr;
            var copy=arr.Copy();
            Assert.ReferenceEquals(copy, copy[0]);
        }

        [TestMethod]
        public void ReferenceEqualityComparerShouldNotUseOverriddenHash()
        {
            var t = new OverriddenHash();
            var equalityComparer = new ReferenceEqualityComparer();
            Assert.AreNotEqual(42, equalityComparer.GetHashCode(t));
            Assert.AreEqual(equalityComparer.GetHashCode(t), RuntimeHelpers.GetHashCode(t));
        }

        static IEnumerable<T> ToIEnumerable<T>(System.Collections.IEnumerable enumerable)
        {
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return (T)enumerator.Current;
            }
        }

        static void AssertArraysAreEqual<T>(Array array1,Array array2,bool refsMustBeDifferent)
        {
            Assert.AreEqual(array1.GetType(), array2.GetType());
            Assert.AreEqual(array1.LongLength, array2.LongLength);

            foreach(var v in ToIEnumerable<T>(array1).Zip(
                ToIEnumerable<T>(array2),
                (x, y) => new { x, y } ))
            {
                Assert.AreEqual(v.x, v.y);
                if (refsMustBeDifferent) Assert.AreNotSame(v.x, v.y);
            }
        }

        [TestMethod]
        public void Copy_Copies1dArray()
        {
            var t1 = new int[]{ 1, 2, 3 };
            var t2 = t1.Copy();
            Assert.AreNotSame(t1, t2);
            AssertArraysAreEqual<int>(t1, t2,false);
        }

        [TestMethod]
        public void Copy_Copies1dRefElementArray()
        {
            var t1 = new Wrapper<int>[]
            {
                new Wrapper<int>{ Value = 1 } ,
                new Wrapper<int>{ Value = 2 } ,
                new Wrapper<int>{ Value = 3 } ,
            };
            var t2 = t1.Copy();
            Assert.AreNotSame(t1, t2);
            AssertArraysAreEqual<Wrapper<int>>(t1, t2, refsMustBeDifferent: true);
        }

        [TestMethod]
        public void Copy_Copies2dArray()
        {
            var t1 = new int[,] 
            { 
                { 1, 2 },
                { 3, 4 }, 
                { 5, 6 },
            };

            var t2 = t1.Copy();
            Assert.AreNotSame(t1, t2);
            AssertArraysAreEqual<int>(t1, t2,false);
        }

        [TestMethod]
        public void Copy_Copies2dRefElementArray()
        {
            var t1 = new Wrapper<int>[,]
            {
                { new Wrapper<int>{ Value = 1 } , new Wrapper<int>{ Value = 2 } },
                { new Wrapper<int>{ Value = 3 } , new Wrapper<int>{ Value = 4 } },
                { new Wrapper<int>{ Value = 5 } , new Wrapper<int>{ Value = 6 } },
            };
            var t2 = t1.Copy();
            Assert.AreNotSame(t1, t2);
            AssertArraysAreEqual<Wrapper<int>>(t1, t2, refsMustBeDifferent: true);
        }

        [TestMethod]
        public void Copy_Copies3dArray()
        {
            var t1 = new int[,,] 
            { 
                { 
                    { 1, 2 }, 
                    { 3, 4 }, 
                    { 5, 6 }
                }, 
                { 
                    { 7, 8 }, 
                    { 9, 10 }, 
                    { 11, 12 }
                }
            };
            var t2 = t1.Copy();
            Assert.AreNotSame(t1, t2);
            AssertArraysAreEqual<int>(t1, t2,false);
        }

        [TestMethod]
        public void Copy_Copies3dRefElementArray()
        {
            var t1 = new Wrapper<int>[,,]
            {
                {
                    { new Wrapper<int>{ Value = 1 } , new Wrapper<int>{ Value = 2 } },
                    { new Wrapper<int>{ Value = 3 } , new Wrapper<int>{ Value = 4 } },
                    { new Wrapper<int>{ Value = 5 } , new Wrapper<int>{ Value = 6 } },
                },
                {
                    { new Wrapper<int>{ Value = 7 } , new Wrapper<int>{ Value = 8 } },
                    { new Wrapper<int>{ Value = 9 } , new Wrapper<int>{ Value = 10 } },
                    { new Wrapper<int>{ Value = 11 } , new Wrapper<int>{ Value = 12 } },
                }
            };
            var t2 = t1.Copy();
            Assert.AreNotSame(t1, t2);
            AssertArraysAreEqual<Wrapper<int>>(t1, t2, refsMustBeDifferent: true);
        }
    }
}
