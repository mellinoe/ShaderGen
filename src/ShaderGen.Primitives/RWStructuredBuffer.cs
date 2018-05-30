using System;
using System.Collections.Generic;
using System.Linq;

namespace ShaderGen
{
    /// <summary>
    /// A writable structured buffer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class RWStructuredBuffer<T> where T : struct
    {
        /// <summary>
        /// The data.
        /// </summary>
        private readonly T[] _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="RWStructuredBuffer{T}" /> class.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <remarks>
        /// Note, use with caution as this sets the underlying data to the passed in array.
        /// TODO Implement shader code to mimic behaviour of calling constructor.
        /// </remarks>
        public RWStructuredBuffer(ref T[] data)
        {
            _data = data;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RWStructuredBuffer{T}" /> class.
        /// </summary>
        /// <param name="capacity">The capacity.</param>
        /// <remarks>
        /// TODO Implement shader code to mimic behaviour of calling constructor.
        /// </remarks>
        public RWStructuredBuffer(int capacity) : this((uint)capacity) { }
        public RWStructuredBuffer(uint capacity)
        {
            _data = new T[capacity];
        }

        /// <summary>
        /// Gets the <see cref="T" /> with the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="T" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public ref T this[int index] => ref _data[index];

        /// <summary>
        /// Gets the <see cref="T" /> with the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="T" />.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns></returns>
        public ref T this[uint index] => ref _data[index];
    }
}
