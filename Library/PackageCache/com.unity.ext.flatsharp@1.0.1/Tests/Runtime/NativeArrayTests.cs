using System;
using System.Collections.Generic;
using FlatSharp;
using NUnit.Framework;
using Unity.Collections;
using Unity.FlatSharpTests;
using UnityEngine;
using FlatSharp.Runtime.Extensions;

public class NativeArrayTests
{

    [Test]
    public void SerializePrimitiveAsList()
    {
        var orig = new Foo();
        var values = new List<int>(new[] { 1, 2, 3 });

        orig.intList = values;

        var maxBytesNeeded = Foo.Serializer.GetMaxSize(orig);
        var buffer = new byte[maxBytesNeeded];
        Foo.Serializer.Write(new SpanWriter(), new Span<byte>(buffer), orig);

        var parsed = Foo.Serializer.Parse(buffer);

        Assert.IsNotNull(parsed.intList);
        Assert.IsNull(parsed.vector2List);
        Assert.IsFalse(parsed.intNativeArray.HasValue);
        Assert.IsFalse(parsed.vector2NativeArray.HasValue);

        var origValue = orig.intList;
        var parsedValue = parsed.intList;
        Assert.AreEqual(origValue.Count, parsedValue.Count);
        for (var i = 0; i < origValue.Count; ++i)
            Assert.AreEqual(origValue[i], parsedValue[i]);
    }

    [Test]
    public void SerializeVector2AsList()
    {
        var orig = new Foo();
        var values = new List<Vector2>(new[] { Vector2.one });

        orig.vector2List = values;

        var maxBytesNeeded = Foo.Serializer.GetMaxSize(orig);
        var buffer = new byte[maxBytesNeeded];
        Foo.Serializer.Write(new SpanWriter(), new Span<byte>(buffer), orig);

        var parsed = Foo.Serializer.Parse(buffer);

        Assert.IsNull(parsed.intList);
        Assert.IsNotNull(parsed.vector2List);
        Assert.IsFalse(parsed.intNativeArray.HasValue);
        Assert.IsFalse(parsed.vector2NativeArray.HasValue);

        var origValue = orig.vector2List;
        var parsedValue = parsed.vector2List;
        Assert.AreEqual(origValue.Count, parsedValue.Count);
        for (var i = 0; i < origValue.Count; ++i)
            Assert.AreEqual(origValue[i], parsedValue[i]);
    }

    [Test]
    public unsafe void SerializePrimitiveAsNativeArray()
    {
        var orig = new Foo();
        using (var values = new NativeArray<int>(new[] { 1, 2, 3 }, Allocator.Temp))
        {
            orig.intNativeArray = values;

            var maxBytesNeeded = Foo.Serializer.GetMaxSize(orig);
            var buffer = new byte[maxBytesNeeded];
            var size = Foo.Serializer.Write(new SpanWriter(), new Span<byte>(buffer), orig);
            fixed (byte* data = buffer)
            {

                var parsed = Foo.Serializer.Parse(size, data);

                Assert.IsNull(parsed.intList);
                Assert.IsNull(parsed.vector2List);
                Assert.IsTrue(parsed.intNativeArray.HasValue);
                Assert.IsFalse(parsed.vector2NativeArray.HasValue);

                var origValue = orig.intNativeArray.Value;
                var parsedValue = parsed.intNativeArray.Value;
                Assert.AreEqual(origValue.Length, parsedValue.Length);
                for (var i = 0; i < origValue.Length; ++i)
                    Assert.AreEqual(origValue[i], parsedValue[i]);
            }
        }
    }

    [Test]
    public unsafe void SerializeVector2AsNativeArray()
    {
        var orig = new Foo();
        using (var values = new NativeArray<Vector2>(new[] { Vector2.zero, Vector2.one }, Allocator.Temp))
        {
            orig.vector2NativeArray = values;

            var maxBytesNeeded = Foo.Serializer.GetMaxSize(orig);
            var buffer = new byte[maxBytesNeeded];
            var size = Foo.Serializer.Write(new SpanWriter(), new Span<byte>(buffer), orig);

            fixed (byte* data = buffer)
            {
                var parsed = Foo.Serializer.Parse(size, data);

                Assert.IsNull(parsed.intList);
                Assert.IsNull(parsed.vector2List);
                Assert.IsFalse(parsed.intNativeArray.HasValue);
                Assert.IsTrue(parsed.vector2NativeArray.HasValue);

                var origValue = orig.vector2NativeArray.Value;
                var parsedValue = parsed.vector2NativeArray.Value;
                Assert.AreEqual(origValue.Length, parsedValue.Length);
                for (var i = 0; i < origValue.Length; ++i)
                    Assert.AreEqual(origValue[i], parsedValue[i]);
            }
        }
    }

}
