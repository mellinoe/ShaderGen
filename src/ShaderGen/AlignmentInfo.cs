using System;

namespace ShaderGen
{
    /// <summary>
    /// Holds information about a types alignment and size
    /// </summary>
    /// <seealso cref="System.IEquatable{AlignmentInfo}" />
    public struct AlignmentInfo : IEquatable<AlignmentInfo>
    {
        public readonly int CSharpSize;
        public readonly int ShaderSize;
        public readonly int CSharpAlignment;
        public readonly int ShaderAlignment;

        /// <summary>
        /// Initializes a new instance of the <see cref="AlignmentInfo"/> struct.
        /// </summary>
        /// <param name="csharpSize">Size of the csharp.</param>
        /// <param name="shaderSize">Size of the shader.</param>
        /// <param name="csharpAlignment">The csharp alignment.</param>
        /// <param name="shaderAlignment">The shader alignment.</param>
        public AlignmentInfo(int csharpSize, int shaderSize, int csharpAlignment, int shaderAlignment)
        {
            CSharpSize = csharpSize;
            ShaderSize = shaderSize;
            CSharpAlignment = csharpAlignment;
            ShaderAlignment = shaderAlignment;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj) => obj is AlignmentInfo info && Equals(info);

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        /// true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.
        /// </returns>
        public bool Equals(AlignmentInfo other)
            => CSharpSize == other.CSharpSize &&
               ShaderSize == other.ShaderSize &&
               CSharpAlignment == other.CSharpAlignment &&
               ShaderAlignment == other.ShaderAlignment;

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hashCode = 999318856;
            hashCode = hashCode * -1521134295 + CSharpSize.GetHashCode();
            hashCode = hashCode * -1521134295 + ShaderSize.GetHashCode();
            hashCode = hashCode * -1521134295 + CSharpAlignment.GetHashCode();
            hashCode = hashCode * -1521134295 + ShaderAlignment.GetHashCode();
            return hashCode;
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        /// <param name="info1">The info1.</param>
        /// <param name="info2">The info2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator ==(AlignmentInfo info1, AlignmentInfo info2) => info1.Equals(info2);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        /// <param name="info1">The info1.</param>
        /// <param name="info2">The info2.</param>
        /// <returns>
        /// The result of the operator.
        /// </returns>
        public static bool operator !=(AlignmentInfo info1, AlignmentInfo info2) => !(info1 == info2);

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
            => $"Size: C#={CSharpSize}, Shader={ShaderSize}; Alignment: C#={CSharpAlignment}, Shader={ShaderAlignment}";
    }
}