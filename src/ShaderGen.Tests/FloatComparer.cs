using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ShaderGen.Tests
{
    /// <summary>
    /// Compares floats approximately, and any structures that can be broken down into floats.
    /// </summary>
    /// <seealso cref="object" />
    public class FloatComparer : IEqualityComparer<object>
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<FieldInfo>> _childFieldInfos
            = new ConcurrentDictionary<Type, IReadOnlyCollection<FieldInfo>>();

        /// <summary>
        /// The epsilon for comparing floats.
        /// </summary>
        public readonly float Epsilon;

        /// <summary>
        /// Initializes a new instance of the <see cref="FloatComparer" /> class.
        /// </summary>
        /// <param name="epsilon">The epsilon.</param>
        public FloatComparer(float epsilon)
        {
            Epsilon = epsilon;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="a">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <param name="b">The b.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(object a, object b)
        {
            if (object.Equals(a, b))
            {
                return true;
            }

            Type currentType = a?.GetType() ?? b.GetType();
            if (currentType == typeof(float))
            {
                return ((float)a).ApproximatelyEqual((float)b, Epsilon);
            }

            object aValue = a;
            object bValue = b;
            Stack<(Type currentType, object aValue, object bValue)> stack
                = new Stack<(Type currentType, object aValue, object bValue)>();
            stack.Push((currentType, aValue, bValue));

            while (stack.Count > 0)
            {
                // Pop top of stack.
                (currentType, aValue, bValue) = stack.Pop();

                // Get fields (cached)
                IReadOnlyCollection<FieldInfo> childFields = _childFieldInfos.GetOrAdd(currentType, t => t.GetFields().Where(f => !f.IsStatic).ToArray());

                if (childFields.Count < 1)
                {
                    // No child fields, we have an inequality
                    return false;
                }

                foreach (FieldInfo childField in childFields)
                {
                    object aMemberValue = childField.GetValue(aValue);
                    object bMemberValue = childField.GetValue(bValue);

                    currentType = childField.FieldType;
                    // Short cut equality
                    if (object.Equals(aMemberValue, bMemberValue) ||
                        currentType == typeof(float) && ((float)aMemberValue).ApproximatelyEqual((float)bMemberValue, Epsilon))
                    {
                        continue;
                    }

                    stack.Push((currentType, aMemberValue, bMemberValue));
                }
            }

            return true;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public int GetHashCode(object obj)
        {
            // Note we will get loads of collisions and rely instead on equality.
            return 0;
        }
    }
}