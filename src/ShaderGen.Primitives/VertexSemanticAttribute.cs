using System;

namespace ShaderGen
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class VertexSemanticAttribute : Attribute
    {
        public SemanticType Type { get; }
        public VertexSemanticAttribute(SemanticType type)
        {
            Type = type;
        }
    }

    public class PositionSemanticAttribute : VertexSemanticAttribute
    {
        public PositionSemanticAttribute() : base(SemanticType.Position) { }
    }

    public class NormalSemanticAttribute : VertexSemanticAttribute
    {
        public NormalSemanticAttribute() : base(SemanticType.Normal) { }
    }

    public class TextureCoordinateSemanticAttribute : VertexSemanticAttribute
    {
        public TextureCoordinateSemanticAttribute() : base(SemanticType.TextureCoordinate) { }
    }

    public class ColorSemanticAttribute : VertexSemanticAttribute
    {
        public ColorSemanticAttribute() : base(SemanticType.Color) { }
    }

    public class TangentSemanticAttribute : VertexSemanticAttribute
    {
        public TangentSemanticAttribute() : base(SemanticType.Tangent) { }
    }

    public class SystemPositionSemanticAttribute : VertexSemanticAttribute
    {
        public SystemPositionSemanticAttribute() : base(SemanticType.SystemPosition) { }
    }

    public class ColorTargetSemanticAttribute : VertexSemanticAttribute
    {
        public ColorTargetSemanticAttribute() : base(SemanticType.ColorTarget) { }
    }
}
