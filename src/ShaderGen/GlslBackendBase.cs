using Microsoft.CodeAnalysis;
using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Diagnostics;

namespace ShaderGen
{
    public abstract class GlslBackendBase : LanguageBackend
    {
        protected readonly HashSet<string> _uniformNames = new HashSet<string>();

        public GlslBackendBase(Compilation compilation) : base(compilation)
        {
        }

        protected void WriteStructure(StringBuilder sb, StructureDefinition sd)
        {
            sb.AppendLine($"struct {CSharpToShaderType(sd.Name)}");
            sb.AppendLine("{");
            StringBuilder fb = new StringBuilder();
            foreach (FieldDefinition field in sd.Fields)
            {
                fb.Append(CSharpToShaderType(field.Type.Name.Trim()));
                fb.Append(' ');
                fb.Append(CorrectIdentifier(field.Name.Trim()));
                int arrayCount = field.ArrayElementCount;
                if (arrayCount > 0)
                {
                    fb.Append('['); fb.Append(arrayCount); fb.Append(']');
                }
                fb.Append(';');
                sb.Append("    ");
                sb.AppendLine(fb.ToString());
                fb.Clear();
            }
            sb.AppendLine("};");
            sb.AppendLine();
        }


        protected override string GenerateFullTextCore(string setName, ShaderFunction function)
        {
            BackendContext context = GetContext(setName);
            StringBuilder sb = new StringBuilder();

            ShaderFunctionAndBlockSyntax entryPoint = context.Functions.SingleOrDefault(
                sfabs => sfabs.Function.Name == function.Name);
            if (entryPoint == null)
            {
                throw new ShaderGenerationException("Couldn't find given function: " + function.Name);
            }

            ValidateRequiredSemantics(setName, entryPoint.Function, function.Type);

            StructureDefinition input = GetRequiredStructureType(setName, entryPoint.Function.Parameters[0].Type);

            WriteVersionHeader(sb);

            StructureDefinition[] orderedStructures
                = StructureDependencyGraph.GetOrderedStructureList(Compilation, context.Structures);

            foreach (StructureDefinition sd in orderedStructures)
            {
                WriteStructure(sb, sd);
            }

            foreach (ResourceDefinition rd in context.Resources)
            {
                switch (rd.ResourceKind)
                {
                    case ShaderResourceKind.Uniform:
                        WriteUniform(sb, rd);
                        break;
                    case ShaderResourceKind.Texture2D:
                        WriteTexture2D(sb, rd);
                        break;
                    case ShaderResourceKind.TextureCube:
                        WriteTextureCube(sb, rd);
                        break;
                    case ShaderResourceKind.Texture2DMS:
                        WriteTexture2DMS(sb, rd);
                        break;
                    case ShaderResourceKind.Sampler:
                        WriteSampler(sb, rd);
                        break;
                    default: throw new ShaderGenerationException("Illegal resource kind: " + rd.ResourceKind);
                }
            }

            FunctionCallGraphDiscoverer fcgd = new FunctionCallGraphDiscoverer(
                Compilation,
                new TypeAndMethodName { TypeName = function.DeclaringType, MethodName = function.Name });
            fcgd.GenerateFullGraph();
            TypeAndMethodName[] orderedFunctionList = fcgd.GetOrderedCallList();

            foreach (TypeAndMethodName name in orderedFunctionList)
            {
                ShaderFunctionAndBlockSyntax f = context.Functions.Single(
                    sfabs => sfabs.Function.DeclaringType == name.TypeName && sfabs.Function.Name == name.MethodName);
                if (!f.Function.IsEntryPoint)
                {
                    sb.AppendLine(new ShaderMethodVisitor(Compilation, setName, f.Function, this).VisitFunction(f.Block));
                }
            }

            string result = new ShaderMethodVisitor(Compilation, setName, entryPoint.Function, this)
                .VisitFunction(entryPoint.Block);
            sb.AppendLine(result);

            WriteMainFunction(setName, sb, entryPoint.Function);

            return sb.ToString();
        }

        private void WriteMainFunction(string setName, StringBuilder sb, ShaderFunction entryFunction)
        {
            ParameterDefinition input = entryFunction.Parameters[0];
            StructureDefinition inputType = GetRequiredStructureType(setName, input.Type);
            StructureDefinition outputType =
                entryFunction.ReturnType.Name != "System.Numerics.Vector4"
                && entryFunction.ReturnType.Name != "System.Void"
                    ? GetRequiredStructureType(setName, entryFunction.ReturnType)
                    : null;

            // Declare "in" variables
            int inVarIndex = 0;
            string fragCoordName = null;
            foreach (FieldDefinition field in inputType.Fields)
            {
                if (entryFunction.Type == ShaderFunctionType.FragmentEntryPoint
                    && fragCoordName == null
                    && field.SemanticType == SemanticType.SystemPosition)
                {
                    fragCoordName = field.Name;
                }
                else
                {
                    WriteInOutVariable(
                        sb,
                        true,
                        entryFunction.Type == ShaderFunctionType.VertexEntryPoint,
                        CSharpToShaderType(field.Type.Name),
                        CorrectIdentifier(field.Name),
                        inVarIndex);
                    inVarIndex += 1;
                }
            }

            string mappedReturnType = CSharpToShaderType(entryFunction.ReturnType.Name);

            // Declare "out" variables
            if (entryFunction.Type == ShaderFunctionType.VertexEntryPoint)
            {
                int outVarIndex = 0;
                foreach (FieldDefinition field in outputType.Fields)
                {
                    if (field.SemanticType == SemanticType.SystemPosition)
                    {
                        continue;
                    }
                    else
                    {
                        WriteInOutVariable(
                            sb,
                            false,
                            true,
                            CSharpToShaderType(field.Type.Name),
                            "out_" + CorrectIdentifier(field.Name),
                            outVarIndex);
                        outVarIndex += 1;
                    }
                }
            }
            else
            {
                Debug.Assert(entryFunction.Type == ShaderFunctionType.FragmentEntryPoint);

                if (mappedReturnType == "vec4")
                {
                    WriteInOutVariable(sb, false, false, "vec4", "_outputColor_", 0);
                }
                else if (mappedReturnType != "void")
                {
                    // Composite struct -- declare an out variable for each.
                    int colorTargetIndex = 0;
                    foreach (FieldDefinition field in outputType.Fields)
                    {
                        Debug.Assert(field.SemanticType == SemanticType.ColorTarget);
                        Debug.Assert(field.Type.Name == "System.Numerics.Vector4");
                        int index = colorTargetIndex++;
                        sb.AppendLine($"    layout(location = {index}) out vec4 _outputColor_{index};");
                    }
                }
            }

            sb.AppendLine();

            string inTypeName = CSharpToShaderType(inputType.Name);

            sb.AppendLine($"void main()");
            sb.AppendLine("{");
            sb.AppendLine($"    {inTypeName} {CorrectIdentifier("input")};");

            // Assign synthetic "in" variables (with real field name) to structure passed to actual function.
            int inoutIndex = 0;
            bool foundSystemPosition = false;
            foreach (FieldDefinition field in inputType.Fields)
            {
                if (entryFunction.Type == ShaderFunctionType.VertexEntryPoint)
                {
                    sb.AppendLine($"    {CorrectIdentifier("input")}.{CorrectIdentifier(field.Name)} = {CorrectIdentifier(field.Name)};");
                }
                else
                {
                    if (field.SemanticType == SemanticType.SystemPosition && !foundSystemPosition)
                    {
                        Debug.Assert(field.Name == fragCoordName);
                        foundSystemPosition = true;
                        sb.AppendLine($"    {CorrectIdentifier("input")}.{CorrectIdentifier(field.Name)} = gl_FragCoord;");
                    }
                    else
                    {
                        sb.AppendLine($"    {CorrectIdentifier("input")}.{CorrectIdentifier(field.Name)} = fsin_{inoutIndex++};");
                    }
                }
            }

            // Call actual function.
            if (mappedReturnType != "void")
            {
                sb.AppendLine($"    {mappedReturnType} {CorrectIdentifier("output")} = {entryFunction.Name}({CorrectIdentifier("input")});");
            }
            else
            {
                sb.Append($"    {entryFunction.Name}({CorrectIdentifier("input")});");
            }

            // Assign output fields to synthetic "out" variables with normalized "fsin_#" names.
            if (entryFunction.Type == ShaderFunctionType.VertexEntryPoint)
            {
                inoutIndex = 0;
                FieldDefinition systemPositionField = null;
                foreach (FieldDefinition field in outputType.Fields)
                {
                    if (systemPositionField == null && field.SemanticType == SemanticType.SystemPosition)
                    {
                        systemPositionField = field;
                    }
                    else
                    {
                        sb.AppendLine($"    fsin_{inoutIndex++} = {CorrectIdentifier("output")}.{CorrectIdentifier(field.Name)};");
                    }
                }

                if (systemPositionField == null)
                {
                    // TODO: Should be caught earlier.
                    throw new ShaderGenerationException("Vertex functions must output a SystemPosition semantic.");
                }

                sb.AppendLine($"    gl_Position = {CorrectIdentifier("output")}.{CorrectIdentifier(systemPositionField.Name)};");
                EmitGlPositionCorrection(sb);
            }
            else
            {
                Debug.Assert(entryFunction.Type == ShaderFunctionType.FragmentEntryPoint);
                if (mappedReturnType == "vec4")
                {
                    sb.AppendLine($"    _outputColor_ = {CorrectIdentifier("output")};");
                }
                else if (mappedReturnType != "void")
                {
                    // Composite struct -- assign each field to output
                    int colorTargetIndex = 0;
                    foreach (FieldDefinition field in outputType.Fields)
                    {
                        Debug.Assert(field.SemanticType == SemanticType.ColorTarget);
                        sb.AppendLine($"    _outputColor_{colorTargetIndex++} = {CorrectIdentifier("output")}.{CorrectIdentifier(field.Name)};");
                    }
                }
            }
            sb.AppendLine("}");
        }

        protected override string CSharpToIdentifierNameCore(string typeName, string identifier)
        {
            return GlslKnownIdentifiers.GetMappedIdentifier(typeName, identifier);
        }

        internal override string CorrectIdentifier(string identifier)
        {
            if (s_glslKeywords.Contains(identifier))
            {
                return identifier + "_";
            }

            return identifier;
        }

        internal override void AddResource(string setName, ResourceDefinition rd)
        {
            if (rd.ResourceKind == ShaderResourceKind.Uniform)
            {
                _uniformNames.Add(rd.Name);
            }

            base.AddResource(setName, rd);
        }

        internal override string CorrectFieldAccess(SymbolInfo symbolInfo)
        {
            string originalName = symbolInfo.Symbol.Name;
            string mapped = CSharpToShaderIdentifierName(symbolInfo);
            string identifier = CorrectIdentifier(mapped);
            if (_uniformNames.Contains(originalName))
            {
                return "field_" + identifier;
            }
            else
            {
                return identifier;
            }
        }

        private static readonly HashSet<string> s_glslKeywords = new HashSet<string>()
        {
            "input", "output",
        };

        protected abstract void WriteVersionHeader(StringBuilder sb);
        protected abstract void WriteUniform(StringBuilder sb, ResourceDefinition rd);
        protected abstract void WriteSampler(StringBuilder sb, ResourceDefinition rd);
        protected abstract void WriteTexture2D(StringBuilder sb, ResourceDefinition rd);
        protected abstract void WriteTextureCube(StringBuilder sb, ResourceDefinition rd);
        protected abstract void WriteTexture2DMS(StringBuilder sb, ResourceDefinition rd);
        protected abstract void WriteInOutVariable(
            StringBuilder sb,
            bool isInVar,
            bool isVertexStage,
            string normalizedType,
            string normalizedIdentifier,
            int index);
        protected abstract void EmitGlPositionCorrection(StringBuilder sb);
    }
}
