﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Microsoft.Extensions.Configuration.Binder.SourceGeneration
{
    public sealed partial class ConfigurationBindingGenerator
    {
        private sealed partial class Emitter
        {
            private enum InitializationKind
            {
                None = 0,
                SimpleAssignment = 1,
                AssignmentWithNullCheck = 2,
                Declaration = 3,
            }
            private static class Expression
            {
                public const string sectionKey = "section.Key";
                public const string sectionPath = "section.Path";
                public const string sectionValue = "section.Value";

                public const string GetBinderOptions = $"{FullyQualifiedDisplayString.CoreBindingHelper}.{Identifier.GetBinderOptions}";
            }

            private static class FullyQualifiedDisplayString
            {
                public const string ActionOfBinderOptions = $"global::System.Action<global::Microsoft.Extensions.Configuration.BinderOptions>";
                public const string AddSingleton = $"{ServiceCollectionServiceExtensions}.AddSingleton";
                public const string ConfigurationChangeTokenSource = "global::Microsoft.Extensions.Options.ConfigurationChangeTokenSource";
                public const string CoreBindingHelper = $"global::{ConfigurationBindingGenerator.ProjectName}.{Identifier.CoreBindingHelper}";
                public const string IConfiguration = "global::Microsoft.Extensions.Configuration.IConfiguration";
                public const string IConfigurationSection = IConfiguration + "Section";
                public const string IOptionsChangeTokenSource = "global::Microsoft.Extensions.Options.IOptionsChangeTokenSource";
                public const string InvalidOperationException = "global::System.InvalidOperationException";
                public const string IServiceCollection = "global::Microsoft.Extensions.DependencyInjection.IServiceCollection";
                public const string NotSupportedException = "global::System.NotSupportedException";
                public const string OptionsBuilderOfTOptions = $"global::Microsoft.Extensions.Options.OptionsBuilder<{Identifier.TOptions}>";
                public const string ServiceCollectionServiceExtensions = "global::Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions";
                public const string Type = $"global::System.Type";
            }

            private static class MinimalDisplayString
            {
                public const string NullableActionOfBinderOptions = "Action<BinderOptions>?";
            }

            private static class Identifier
            {
                public const string binderOptions = nameof(binderOptions);
                public const string configureOptions = nameof(configureOptions);
                public const string configuration = nameof(configuration);
                public const string configSectionPath = nameof(configSectionPath);
                public const string defaultValue = nameof(defaultValue);
                public const string element = nameof(element);
                public const string enumValue = nameof(enumValue);
                public const string exception = nameof(exception);
                public const string getPath = nameof(getPath);
                public const string key = nameof(key);
                public const string name = nameof(name);
                public const string obj = nameof(obj);
                public const string optionsBuilder = nameof(optionsBuilder);
                public const string originalCount = nameof(originalCount);
                public const string section = nameof(section);
                public const string services = nameof(services);
                public const string stringValue = nameof(stringValue);
                public const string temp = nameof(temp);
                public const string type = nameof(type);

                public const string Add = nameof(Add);
                public const string AddSingleton = nameof(AddSingleton);
                public const string Any = nameof(Any);
                public const string Array = nameof(Array);
                public const string Bind = nameof(Bind);
                public const string BindCore = nameof(BindCore);
                public const string BindCoreUntyped = nameof(BindCoreUntyped);
                public const string BinderOptions = nameof(BinderOptions);
                public const string Configure = nameof(Configure);
                public const string CopyTo = nameof(CopyTo);
                public const string ContainsKey = nameof(ContainsKey);
                public const string CoreBindingHelper = nameof(CoreBindingHelper);
                public const string Count = nameof(Count);
                public const string CultureInfo = nameof(CultureInfo);
                public const string CultureNotFoundException = nameof(CultureNotFoundException);
                public const string Enum = nameof(Enum);
                public const string ErrorOnUnknownConfiguration = nameof(ErrorOnUnknownConfiguration);
                public const string GeneratedConfigurationBinder = nameof(GeneratedConfigurationBinder);
                public const string GeneratedOptionsBuilderBinder = nameof(GeneratedOptionsBuilderBinder);
                public const string GeneratedServiceCollectionBinder = nameof(GeneratedServiceCollectionBinder);
                public const string Get = nameof(Get);
                public const string GetBinderOptions = nameof(GetBinderOptions);
                public const string GetCore = nameof(GetCore);
                public const string GetChildren = nameof(GetChildren);
                public const string GetSection = nameof(GetSection);
                public const string GetValue = nameof(GetValue);
                public const string GetValueCore = nameof(GetValueCore);
                public const string HasChildren = nameof(HasChildren);
                public const string HasConfig = nameof(HasConfig);
                public const string HasValueOrChildren = nameof(HasValueOrChildren);
                public const string HasValue = nameof(HasValue);
                public const string IConfiguration = nameof(IConfiguration);
                public const string IConfigurationSection = nameof(IConfigurationSection);
                public const string Int32 = "int";
                public const string InvalidOperationException = nameof(InvalidOperationException);
                public const string InvariantCulture = nameof(InvariantCulture);
                public const string Length = nameof(Length);
                public const string Parse = nameof(Parse);
                public const string Path = nameof(Path);
                public const string Resize = nameof(Resize);
                public const string Services = nameof(Services);
                public const string TOptions = nameof(TOptions);
                public const string TryCreate = nameof(TryCreate);
                public const string TryGetValue = nameof(TryGetValue);
                public const string TryParse = nameof(TryParse);
                public const string Uri = nameof(Uri);
                public const string Value = nameof(Value);
            }

            private bool ShouldEmitBinders() =>
                ShouldEmitMethods(MethodsToGen_ConfigurationBinder.Any) ||
                ShouldEmitMethods(MethodsToGen_Extensions_OptionsBuilder.Any) ||
                ShouldEmitMethods(MethodsToGen_Extensions_ServiceCollection.Any);

            private void EmitBlankLineIfRequired()
            {
                if (_precedingBlockExists)
                {
                    _writer.WriteBlankLine();
                }

                _precedingBlockExists = true;
            }

            private void EmitCheckForNullArgument_WithBlankLine_IfRequired(bool isValueType)
            {
                if (!isValueType)
                {
                    EmitCheckForNullArgument_WithBlankLine(Identifier.obj);
                }
            }

            private void EmitCheckForNullArgument_WithBlankLine(string paramName)
            {
                string exceptionTypeDisplayString = _useFullyQualifiedNames
                    ? "global::System.ArgumentNullException"
                    : "ArgumentNullException";

                _writer.WriteBlock($$"""
                    if ({{paramName}} is null)
                    {
                        throw new {{exceptionTypeDisplayString}}(nameof({{paramName}}));
                    }
                    """);

                _writer.WriteBlankLine();
            }

            private bool EmitInitException(TypeSpec type)
            {
                Debug.Assert(type.InitializationStrategy is not InitializationStrategy.None);

                if (!type.CanInitialize)
                {
                    _writer.WriteLine(GetInitException(type.InitExceptionMessage) + ";");
                    return true;
                }

                return false;
            }

            private string GetInitException(string message) => $@"throw new {GetInvalidOperationDisplayName()}(""{message}"")";

            private string GetIncrementalVarName(string prefix) => $"{prefix}{_parseValueCount++}";

            private string GetTypeDisplayString(TypeSpec type) => _useFullyQualifiedNames ? type.FullyQualifiedDisplayString : type.MinimalDisplayString;

            private string GetHelperMethodDisplayString(string methodName)
            {
                if (_useFullyQualifiedNames)
                {
                    methodName = FullyQualifiedDisplayString.CoreBindingHelper + "." + methodName;
                }

                return methodName;
            }

            private string GetInvalidOperationDisplayName() => _useFullyQualifiedNames ? FullyQualifiedDisplayString.InvalidOperationException : Identifier.InvalidOperationException;
        }
    }
}
