// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Azure.Bicep.Types.Concrete;

public class NamespaceFunctionType : TypeBase
{
    [JsonConstructor]
    public NamespaceFunctionType(
        string name,
        string? description,
        string? evaluatedLanguageExpression,
        IReadOnlyList<NamespaceFunctionParameter> parameters,
        ITypeReference outputType,
        BicepSourceFileKind? visibleInFileKind)
        => (Name, Description, EvaluatedLanguageExpression, Parameters, OutputType, VisibleInFileKind) = (name, description, evaluatedLanguageExpression, parameters, outputType, visibleInFileKind);

    public string Name { get; }
    public string? Description { get; }

    /// <summary>
    /// The ARM language expression that this function should be reduced to at compile time.
    /// When <code>null</code>, the function is evaluated at runtime via the extensibility framework.
    /// </summary>
    public string? EvaluatedLanguageExpression { get; }

    public IReadOnlyList<NamespaceFunctionParameter> Parameters { get; }

    public ITypeReference OutputType { get; }

    /// <summary>
    /// The kind of Bicep source file this function is visible in.
    /// If <code>null</code>, the function is visible in all bicep file kinds.
    /// </summary>
    public BicepSourceFileKind? VisibleInFileKind { get; }
}

public class NamespaceFunctionParameter
{
    [JsonConstructor]
    public NamespaceFunctionParameter(string name, ITypeReference type, string? description, NamespaceFunctionParameterFlags flags)
        => (Name, Type, Description, Flags) = (name, type, description, flags);

    public string Name { get; }

    public ITypeReference Type { get; }

    public string? Description { get; }

    public NamespaceFunctionParameterFlags Flags { get; }
}

[Flags]
public enum NamespaceFunctionParameterFlags
{
    None = 0,

    /// <summary>
    /// An argument for this parameter must be supplied when the function is called.
    /// </summary>
    Required = 1,

    /// <summary>
    /// The argument supplied for this parameter must be a compile-time constant value. Expressions are not permitted.
    /// </summary>
    CompileTimeConstant = 1 << 1,

    /// <summary>
    /// The property only accepts deploy-time constants whose values must be known at the start of the deployment, and do not require inlining. Expressions are permitted, but they must be resolvable during template preprocessing.
    /// </summary>
    DeployTimeConstant = 1 << 2,
}

public enum BicepSourceFileKind
{
    BicepFile = 1,
    ParamsFile = 2
}
