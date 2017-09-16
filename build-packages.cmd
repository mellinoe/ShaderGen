@echo off
dotnet pack src\ShaderGen.Primitives\ShaderGen.Primitives.csproj -c Release
dotnet pack src\ShaderGen.Build\ShaderGen.Build.csproj -c Release
