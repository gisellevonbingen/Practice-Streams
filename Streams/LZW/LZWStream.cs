﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Streams.IO;

namespace Streams.LZW
{
    public class LZWStream : WrappedByteStream
    {
        public const int EoiCode = -2;

        public static int GetUsingBits(int value)
        {
            if (value < 512)
            {
                return 9;
            }
            else
            {
                return (int)Math.Ceiling(Math.Log2(value));
            }

        }

        protected readonly BitStream BaseBitStream;
        protected readonly CompressionMode Mode;
        public LZWProcessor Processor { get; private set; }

        protected int ReadingDataKey { get; private set; } = -1;
        protected IReadOnlyList<byte> ReadingData { get; private set; } = Array.Empty<byte>();
        protected int ReadingPosition { get; private set; } = 0;

        public LZWStream(Stream baseStream, CompressionMode mode) : this(baseStream, mode, false)
        {

        }

        public LZWStream(Stream baseStream, CompressionMode mode, bool leaveOpen) : this(baseStream, mode, new LZWProcessor(), leaveOpen)
        {

        }

        public LZWStream(Stream baseStream, CompressionMode mode, LZWProcessor processor, bool leaveOpen) : base(baseStream, leaveOpen)
        {
            this.BaseBitStream = new BitStream(baseStream, leaveOpen);
            this.Mode = mode;
            this.Processor = processor;
        }

        public override bool CanRead => this.BaseStream.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => this.BaseStream.CanWrite;

        public override long Length => throw new NotSupportedException();

        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        protected int ReadCode()
        {
            var nextKey = this.Processor.NextKey;
            var bits = GetUsingBits(nextKey + 1);
            var code = 0;

            for (var i = 0; i < bits; i++)
            {
                var b = this.BaseBitStream.ReadBit();

                if (b == -1)
                {
                    return EoiCode;
                }

                var shift = bits - i - 1;
                code |= b << shift;
            }

            return code;
        }

        protected void WriteCode(int key)
        {
            var nextKey = this.Processor.NextKey;
            var bits = GetUsingBits(nextKey - 1);

            for (var i = 0; i < bits; i++)
            {
                var shift = bits - i - 1;
                var mask = 1 << shift;
                var bit = (key & mask) >> shift;
                this.BaseBitStream.WriteBit(bit);
            }

        }

        protected bool ReadData()
        {
            var code = this.ReadCode();

            if (code == EoiCode)
            {
                this.ReadingDataKey = code;
                return false;
            }
            else
            {
                this.ReadingDataKey = this.Processor.Decode(code);
                return true;
            }

        }

        public override int ReadByte()
        {
            if (this.ReadingPosition >= this.ReadingData.Count)
            {
                if (this.ReadData() == false)
                {
                    return -1;
                }
                else
                {
                    this.ReadingPosition = 0;
                    this.ReadingData = this.Processor.Table[this.ReadingDataKey].Values;
                }

            }

            var data = this.ReadingData[this.ReadingPosition++];
            return data;
        }

        public override void WriteByte(byte value)
        {
            this.WriteData(value);
        }

        protected void WriteData(int value)
        {
            var key = this.Processor.Encode(value);

            if (key == -1)
            {
                return;
            }

            this.WriteCode(key);
        }

        protected override void Dispose(bool disposing)
        {
            if (this.Mode == CompressionMode.Compress)
            {
                this.WriteData(-1);
                this.WriteCode(EoiCode);
            }

            base.Dispose(disposing);
        }
    }

}
