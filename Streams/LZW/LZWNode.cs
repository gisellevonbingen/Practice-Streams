﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Streams.LZW
{
    public class LZWNode : IEquatable<LZWNode>
    {
        public int HashCode { get; private set; }

        private readonly List<byte> _Values;
        public IReadOnlyList<byte> Values { get; }

        public LZWNode()
        {
            this.HashCode = 17;
            this._Values = new List<byte>();
            this.Values = new ReadOnlyCollection<byte>(this._Values);
        }

        public LZWNode(byte value) : this()
        {
            this.Add(value);
        }

        public LZWNode(IEnumerable<byte> collection) : this()
        {
            this.AddRange(collection);
        }

        public override string ToString() => $"[{string.Join(", ", this.Values)}]";

        public void Add(byte value)
        {
            this._Values.Add(value);
            this.HashCode = this.HashCode * 31 + value;
        }

        public void AddRange(IEnumerable<byte> collection)
        {
            foreach (var element in collection)
            {
                this.Add(element);
            }

        }

        public override int GetHashCode() => this.HashCode;

        public override bool Equals(object obj) => obj is LZWNode other && this.Equals(other);

        public bool Equals(LZWNode other) => other != null && this.Values.SequenceEqual(other.Values);
    }

}
