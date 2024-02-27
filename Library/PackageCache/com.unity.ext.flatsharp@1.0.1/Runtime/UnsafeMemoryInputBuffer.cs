using System;
using System.Text;
using FlatSharp.Internal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace FlatSharp.Runtime.Extensions
{
    public static class SerializerExtensions
    {
        public static unsafe T Parse<T>(this ISerializer<T> serializer, int size, byte* data, FlatBufferDeserializationOption options = FlatBufferDeserializationOption.Progressive) where T : class
        {
            return serializer.Parse(new UnsafeMemoryInputBuffer(size, data), options);
        }
        public static T Parse<T>(this ISerializer<T> serializer, NativeArray<byte> data, FlatBufferDeserializationOption options = FlatBufferDeserializationOption.Progressive) where T : class
        {
            return serializer.Parse(new UnsafeMemoryInputBuffer(data), options);
        }
    }
    unsafe readonly struct UnsafeMemoryInputBuffer : IInputBuffer
    {
        private readonly int m_Size;
        private readonly byte* m_Data;

        public UnsafeMemoryInputBuffer(int size, byte* data)
        {
            m_Size = size;
            m_Data = data;
        }

        public UnsafeMemoryInputBuffer(NativeArray<byte> nativeArray) : this (nativeArray.Length, (byte*)nativeArray.GetUnsafePtr())
        {
        }

        public byte ReadByte(int offset) => ScalarSpanReader.ReadByte(GetSpan().Slice(offset));

        public sbyte ReadSByte(int offset) => ScalarSpanReader.ReadSByte(GetSpan().Slice(offset));

        public ushort ReadUShort(int offset) => ScalarSpanReader.ReadUShort(GetSpan().Slice(offset));

        public short ReadShort(int offset) => ScalarSpanReader.ReadShort(GetSpan().Slice(offset));

        public uint ReadUInt(int offset) => ScalarSpanReader.ReadUInt(GetSpan().Slice(offset));

        public int ReadInt(int offset) => ScalarSpanReader.ReadInt(GetSpan().Slice(offset));

        public ulong ReadULong(int offset) => ScalarSpanReader.ReadULong(GetSpan().Slice(offset));

        public long ReadLong(int offset) => ScalarSpanReader.ReadLong(GetSpan().Slice(offset));

        public float ReadFloat(int offset) => ScalarSpanReader.ReadFloat(GetSpan().Slice(offset));

        public double ReadDouble(int offset) => ScalarSpanReader.ReadDouble(GetSpan().Slice(offset));

        public string ReadString(int offset, int byteLength, Encoding encoding) => ScalarSpanReader.ReadString(GetSpan().Slice(offset, byteLength), encoding);

        public ReadOnlySpan<byte> GetReadOnlySpan()
        {
            return new ReadOnlySpan<byte>(m_Data, m_Size);
        }

        public ReadOnlyMemory<byte> GetReadOnlyMemory()
        {
            throw new NotImplementedException();
        }

        public Span<byte> GetSpan()
        {
            return new Span<byte>(m_Data, m_Size);
        }

        public Memory<byte> GetMemory()
        {
            throw new NotImplementedException();
        }

        public TItem InvokeLazyParse<TItem>(IGeneratedSerializer<TItem> serializer, in GeneratedSerializerParseArguments arguments)
        {
            return serializer.ParseLazy(this, in arguments);
        }

        public TItem InvokeProgressiveParse<TItem>(IGeneratedSerializer<TItem> serializer, in GeneratedSerializerParseArguments arguments)
        {
            return serializer.ParseProgressive(this, in arguments);
        }

        public TItem InvokeGreedyParse<TItem>(IGeneratedSerializer<TItem> serializer, in GeneratedSerializerParseArguments arguments)
        {
            return serializer.ParseGreedy(this, in arguments);
        }

        public TItem InvokeGreedyMutableParse<TItem>(IGeneratedSerializer<TItem> serializer, in GeneratedSerializerParseArguments arguments)
        {
            return serializer.ParseGreedyMutable(this, in arguments);
        }

        public bool IsReadOnly => false;
        public bool IsPinned => true;
        public int Length => m_Size;
    }
}
