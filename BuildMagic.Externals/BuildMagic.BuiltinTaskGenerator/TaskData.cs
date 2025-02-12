// --------------------------------------------------------------
// Copyright 2024 CyberAgent, Inc.
// --------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace BuildMagic.BuiltinTaskGenerator
{
    public class TaskData
    {
        private const int IndentSize = 4;

        private readonly string _expectedName;
        private readonly ApiOptions? _options;
        private readonly TaskParameterData[] _parameters;
        private readonly HashSet<(UnityVersion version, ApiData data)> _versions = new();

        public TaskData(UnityVersion version, ApiData data, ApiOptions? options)
        {
            _options = options;
            _expectedName = data.ExpectedName;
            _parameters = data.Parameters.Select(p => new TaskParameterData(p)).ToArray();
            _versions.Add((version, data));
            LatestVersion = version;
            LatestVersionApiData = data;
        }

        public UnityVersion LatestVersion { get; private set; }
        public ApiData LatestVersionApiData { get; private set; }

        public bool MatchesParameterSignature(IEnumerable<ParameterData> parameters)
        {
            var i = 0;
            foreach (var parameterData in parameters)
            {
                if (!_parameters[i].IsCompatibleWith(parameterData)) return false;
                i++;
            }

            if (i != _parameters.Length) return false;

            return true;
        }

        public void VisitNewerVersion(UnityVersion version, ApiData data)
        {
            if (version < LatestVersion) throw new InvalidOperationException("Cannot visit older version");
            LatestVersion = version;
            LatestVersionApiData = data;

            var i = 0;
            foreach (var parameterData in data.Parameters)
            {
                _parameters[i].VisitNewerVersion(parameterData);
                i++;
            }

            if (i != _parameters.Length) throw new InvalidOperationException();
            _versions.Add((version, data));
        }

        private static RootWeavedTaskParameter[] WeaveRootParameters(IReadOnlyList<TaskParameterData> parameters,
            ReadOnlySpan<string> dictionaryKeyTypes)
        {
            return WeaveParameters(parameters.Select((p, i) => (p, i)).ToList(), dictionaryKeyTypes).Select(p =>
                new RootWeavedTaskParameter(p.result, p.source.Name, p.source.FormerlySerializedAsNames)).ToArray();
        }

        private static IEnumerable<(WeavedTaskParameter result, TaskParameterData source)> WeaveParameters(
            IReadOnlyList<(TaskParameterData parameter, int index)> parameters, ReadOnlySpan<string> dictionaryKeyTypes)
        {
            // Find the key parameter of the setting (e.g. NamedBuildTarget) by matching the arguments of the getter and setter, and automatically determine the type (NamedBuildTarget) of the key of the setting value as a dictionary
            // ex.
            //   void SetSomething(NamedBuildTarget target, object value)
            //   object GetSomething(NamedBuildTarget target)
            //    -> keys: [target], values: [value]
            // The key priority is based on the order in DictionaryKeyTypes in appsettings.json

            if (parameters.Count == 1)
                return
                [
                    (new WeavedTaskParameterSingle(parameters[0].parameter, parameters[0].index),
                        parameters[0].parameter)
                ];

            var keyIndex = parameters.Select(p => p.parameter.TypeExpression).FindIndex(dictionaryKeyTypes);

            if (keyIndex == -1) keyIndex = parameters.FindIndex(p => !p.parameter.IsOutputParameter);

            if (keyIndex >= 0)
            {
                var key = new WeavedTaskParameterSingle(parameters[keyIndex].parameter, parameters[keyIndex].index);

                var values = parameters.ToList();
                values.RemoveAt(keyIndex);

                var weavedValues = WeaveParameters(values, dictionaryKeyTypes).ToArray();

                if (weavedValues.Length == 0) throw new InvalidOperationException();

                WeavedTaskParameter value;
                if (weavedValues.Length == 1)
                    value = weavedValues[0].result;
                else
                    value = new WeavedTaskParameterTuple(weavedValues.Select(v => (v.result, v.source.Name)).ToList());

                return [(new WeavedTaskParameterDictionary(key, value), parameters[keyIndex].parameter)];
            }

            return parameters.Select(p =>
                ((WeavedTaskParameter)new WeavedTaskParameterSingle(p.parameter, p.index), p.parameter));
        }

        public void Generate(StringBuilder sourceBuilder, UnityVersionDefineConstraintBuilder defineBuilder,
            ILogger logger, ReadOnlySpan<string> dictionaryKeyTypes)
        {
            var versionDefine = defineBuilder.Get(_versions.Select(v => v.version), out var range, out _)?.ToString();

            var weavedRootParameters = WeaveRootParameters(_parameters, dictionaryKeyTypes);

            sourceBuilder.AppendLine(
                $"// {string.Join(", ", range.Segments.Select(s => $"[{s.Since} - {(s.Until == defineBuilder.Latest ? "(latest)" : s.Until == defineBuilder.GetLatestRevision(s.Until) ? $"({s.Until.Major}.{s.Until.Minor} latest)" : s.Until)}]"))}");
            if (versionDefine != null)
                // version defines of this task
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                      #if {{versionDefine}}
                      """);

            // TODO: generation of the parameter class should be shared with Source Generator. Currently, PlayerSettings does not have any items that require FormerlySerializedAs, so it is skipped
            var hasFormerSerializationNames = weavedRootParameters.Any(p => p.FormerSerializationNames.Any());

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                [global::BuildMagicEditor.GenerateBuildTaskAccessories(@"{{_options?.OverrideDisplayName ?? LatestVersionApiData.DisplayName}}", PropertyName = @"{{LatestVersionApiData.PropertyName}}")]
                """);

            // Mark as obsolete if the latest version is obsolete
            var obsoleteVersionDefine =
                defineBuilder.Get(_versions.Where(v => v.data.IsObsolete).Select(v => v.version),
                    out var isNeverObsolete);
            if (!isNeverObsolete && obsoleteVersionDefine != null)
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                      #if {{obsoleteVersionDefine.ToString()}}
                      [global::System.Obsolete]
                      #endif
                      """);

            // Task body

            // constructor

            sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                public class {{_expectedName}}Task : global::BuildMagicEditor.BuildTaskBase<global::BuildMagicEditor.IPreBuildContext>
                {
                    public {{_expectedName}}Task({{string.Join(", ", weavedRootParameters.Select(p => $"{p.Item.ToTypeExpression()} {p.FieldName}"))}})
                    {
                """);

            foreach (var parameter in weavedRootParameters)
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                              this.{{parameter.FieldName}} = {{parameter.FieldName}};
                      """);

            // Run

            sourceBuilder.AppendLine(
/*  lang=c# */"""
                  }
              
                  public override void Run(global::BuildMagicEditor.IPreBuildContext context)
                  {
              """);

            var isFirst = true;
            foreach (var grouping in _versions.GroupBy(v => v.data.SetterExpression))
            {
                // generate branches for each different setter expression among versions
                var setterExpressionVersionDefine =
                    defineBuilder.Get(grouping.Select(g => g.version), out _)?.ToString();

                if (setterExpressionVersionDefine == versionDefine) setterExpressionVersionDefine = null;

                if (setterExpressionVersionDefine != null)
                {
                    if (isFirst)
                        sourceBuilder.AppendLine(
                            /*  lang=c# */
                            $$"""
                              	    #if {{setterExpressionVersionDefine}}
                              """);
                    else
                        sourceBuilder.AppendLine(
                            /*  lang=c# */
                            $$"""
                              	    #elif {{setterExpressionVersionDefine}}
                              """);

                    isFirst = false;
                }
                else
                {
                    if (!isFirst) throw new InvalidOperationException();
                }

                try
                {
                    var cursor = grouping.Key;
                    var localCounter = 0;
                    for (var i = 0; i < weavedRootParameters.Length; i++)
                        cursor = weavedRootParameters[i].ExtractParameter(cursor, ref localCounter);

                    sourceBuilder.AppendLine(cursor);
                }
                catch (FormatException)
                {
                    logger.ZLogError(
                        $"Failed to format setter expression: {grouping.Key} with {_parameters.Length} parameters {string.Join(", ", _parameters.Select(p => $"this.{p.Name}"))} for version {LatestVersion}");
                    throw;
                }
            }

            if (!isFirst)
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    """	    #endif""");

            sourceBuilder.AppendLine(
/*  lang=c# */"""    }""");

            // fields

            foreach (var parameter in weavedRootParameters)
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    $$"""
                          private readonly {{parameter.Item.ToTypeExpression()}} {{parameter.FieldName}};
                      """);

            sourceBuilder.AppendLine(
/*  lang=c# */"""}""");

            // applier

            var applierVersionDefine =
                defineBuilder.Get(_versions.Where(v => v.data.GetterExpression != null).Select(v => v.version),
                    out var isNeverGathable)?.ToString();

            if (!isNeverGathable)
            {
                if (applierVersionDefine == versionDefine) applierVersionDefine = null;

                if (applierVersionDefine != null)
                    sourceBuilder.AppendLine(
                        /*  lang=c# */
                        $$"""
                          #if {{applierVersionDefine}}
                          """);

                // expression groupings

                sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                partial class {{_expectedName}}TaskConfiguration : global::BuildMagicEditor.IProjectSettingApplier
                {
                    void global::BuildMagicEditor.IProjectSettingApplier.ApplyProjectSetting()
                    {
                """);

                var isFirstGetterExpressionGroup = true;
                foreach (var groupingByGetterExpression in _versions.GroupBy(v => v.data.GetterExpression))
                {
                    var getterExpression = groupingByGetterExpression.Key;
                    if (string.IsNullOrEmpty(getterExpression)) continue;

                    var getterExpressionVersionDefine =
                        defineBuilder.Get(groupingByGetterExpression.Select(v => v.version),
                            out _)?.ToString();

                    if (getterExpressionVersionDefine == applierVersionDefine ||
                        getterExpressionVersionDefine == versionDefine) getterExpressionVersionDefine = null;

                    if (getterExpressionVersionDefine != null)
                    {
                        sourceBuilder.AppendLine(
/*  lang=c# */$$"""
                #{{(isFirstGetterExpressionGroup ? "if" : "elif")}} {{getterExpressionVersionDefine}}
                """);
                        isFirstGetterExpressionGroup = false;
                    }

                    try
                    {
                        var cursor = getterExpression;
                        var localCounter = 0;
                        var parameterValueGetter = "this.Value";
                        var parameterValueSetter = $"__BUILDMAGIC__{localCounter++}";

                        var needContainer = weavedRootParameters.Length != 1 || weavedRootParameters[0].Item.HasTuple;

                        if (needContainer)
                        {
                            sourceBuilder.AppendLine($"        var {parameterValueSetter} = {parameterValueGetter};");
                            sourceBuilder.AppendLine($"        {parameterValueSetter} = new();");
                            foreach (var parameter in weavedRootParameters)
                                cursor = parameter
                                    .ToParameter(
                                        new ContainerExpression(
                                            $"{parameterValueGetter}.{parameter.FieldName}",
                                            $"{parameterValueGetter}.{parameter.FieldName}"),
                                        cursor, ref localCounter);
                        }
                        else
                        {
                            foreach (var parameter in weavedRootParameters)
                                cursor = parameter
                                    .ToParameter(
                                        new ContainerExpression(parameterValueGetter, $"var {parameterValueSetter}"),
                                        cursor, ref localCounter);
                        }

                        sourceBuilder.AppendLine(cursor);
                        sourceBuilder.AppendLine($"        this.Value = {parameterValueSetter};");
                    }
                    catch (FormatException)
                    {
                        logger.ZLogError(
                            $"Failed to format setter expression: {getterExpression} with {_parameters.Length} parameters {string.Join(", ", _parameters.Select(p => $"this.{p.Name}"))} for version {LatestVersion}");
                        throw;
                    }
                }

                if (!isFirstGetterExpressionGroup)
                    sourceBuilder.AppendLine(
                        /*  lang=c# */
                        """#endif""");

                sourceBuilder.AppendLine(
/*  lang=c# */"""
                  }
              }
              """);

                if (applierVersionDefine != null)
                    sourceBuilder.AppendLine(
                        /*  lang=c# */
                        """#endif""");
            }

            if (versionDefine != null)
                sourceBuilder.AppendLine(
                    /*  lang=c# */
                    """#endif""");
        }

        #region Nested type: RootWeavedTaskParameter

        private record RootWeavedTaskParameter(
            WeavedTaskParameter Item,
            string FieldName,
            IEnumerable<string?> FormerSerializationNames)
        {
            public string ExtractParameter(string next, ref int localCounter)
            {
                return Item.ExtractParameter($"this.{FieldName}", next, ref localCounter);
            }

            public string ToParameter(ContainerExpression containerExpression, string next, ref int localCounter)
            {
                return Item.ToParameter(containerExpression, next, ref localCounter, 2);
            }
        }

        #endregion

        #region Nested type: WeavedTaskParameter

        private abstract record WeavedTaskParameter
        {
            public abstract bool HasTuple { get; }
            public abstract string ExtractParameter(string containerExpression, string next, ref int localCounter);

            public abstract string ToParameter(ContainerExpression containerExpression, string next,
                ref int localCounter, int indent);

            public abstract string ToTypeExpression();
        }

        #endregion

        #region Nested type: WeavedTaskParameterDictionary

        private record WeavedTaskParameterDictionary(WeavedTaskParameter Key, WeavedTaskParameter Value)
            : WeavedTaskParameter
        {
            public override bool HasTuple => Value.HasTuple || Key.HasTuple;

            public override string ExtractParameter(string containerExpression, string next, ref int localCounter)
            {
                var localKey = $"__BUILDMAGIC__{localCounter++}";
                var localValue = $"__BUILDMAGIC__{localCounter++}";
                return
/*  lang=c# */$$"""
                        foreach (var ({{localKey}}, {{localValue}}) in {{containerExpression}})
                        {
                {{Key.ExtractParameter(localKey, Value.ExtractParameter(localValue, next, ref localCounter), ref localCounter)}}
                        }
                """;
            }

            public override string ToParameter(ContainerExpression containerExpression, string next,
                ref int localCounter, int indent)
            {
                var dictionaryName = $"__BUILDMAGIC__{localCounter++}";
                var localKey = $"__BUILDMAGIC__{localCounter++}";
                var localValue = $"__BUILDMAGIC__{localCounter++}";

                var indentStr = new string(' ', indent * IndentSize);

                return
/*  lang=c# */$$"""
                {{indentStr}}var {{dictionaryName}} = {{containerExpression.Getter}};
                {{indentStr}}{{dictionaryName}} = new();
                {{indentStr}}foreach (var ({{localKey}}, {{localValue}}) in {{containerExpression.Getter}})
                {{indentStr}}{
                {{Key.ToParameter(new ContainerExpression(localKey, $"<INVALID-{localKey}>"), Value.ToParameter(new ContainerExpression(localValue, $"{dictionaryName}[{localKey}]"), next, ref localCounter, indent + 1), ref localCounter, indent + 1)}}
                {{indentStr}}}
                {{indentStr}}{{containerExpression.Setter}} = {{dictionaryName}};
                """;
            }

            public override string ToTypeExpression()
            {
                return
                    $"global::System.Collections.Generic.IReadOnlyDictionary<{Key.ToTypeExpression()}, {Value.ToTypeExpression()}>";
            }
        }

        #endregion

        #region Nested type: WeavedTaskParameterSingle

        private record WeavedTaskParameterSingle(TaskParameterData Parameter, int Index) : WeavedTaskParameter
        {
            public override bool HasTuple => false;

            public override string ExtractParameter(string containerExpression, string next, ref int localCounter)
            {
                return $"        {next.Replace($"{{{Index.ToString()}}}", $"{containerExpression}")}";
            }

            public override string ToParameter(ContainerExpression containerExpression, string next,
                ref int localCounter, int indent)
            {
                return
                    $"{new string(' ', indent * IndentSize)}{next.Replace($"{{{Index.ToString()}}}", Parameter.IsOutputParameter ? containerExpression.Setter : containerExpression.Getter)}";
            }

            public override string ToTypeExpression()
            {
                return Parameter.TypeExpression;
            }
        }

        #endregion

        #region Nested type: WeavedTaskParameterTuple

        private record WeavedTaskParameterTuple(IReadOnlyList<(WeavedTaskParameter item, string name)> Items)
            : WeavedTaskParameter
        {
            public override bool HasTuple => true;

            public override string ExtractParameter(string containerExpression, string next, ref int localCounter)
            {
                var cursor = next;
                foreach (var (item, name) in Items) cursor = item.ExtractParameter(name, cursor, ref localCounter);

                return
/*  lang=c# */$$"""
                        var ({{string.Join(", ", Items.Select(i => i.name))}}) = {{containerExpression}};
                {{cursor}}
                """;
            }

            public override string ToParameter(ContainerExpression containerExpression, string next,
                ref int localCounter, int indent)
            {
                var cursor = next;
                foreach (var (item, name) in Items)
                    cursor = item.ToParameter(new ContainerExpression($"<INVALID-{name}>", $"var {name}"), cursor,
                        ref localCounter, indent);

                return
/*  lang=c# */$$"""
                {{cursor}}
                {{new string(' ', indent * IndentSize)}}{{containerExpression.Setter}} = ({{string.Join(", ", Items.Select(i => i.name))}});
                """;
            }

            public override string ToTypeExpression()
            {
                return $"({string.Join(", ", Items.Select(i => $"{i.item.ToTypeExpression()} {i.name}"))})";
            }
        }

        #endregion
    }

    public record struct ContainerExpression
    {
        public ContainerExpression(string getter, string setter)
        {
            Getter = getter;
            Setter = setter;
        }

        public string Getter { get; }
        public string Setter { get; }
    }

    public class TaskParameterData
    {
        private readonly HashSet<string> _formerlySerializedAsNames = new();

        public TaskParameterData(ParameterData parameterData)
        {
            TypeExpression = parameterData.TypeExpression;
            Name = parameterData.Name;
            IsOutputParameter = parameterData.IsOutput;
        }

        public string TypeExpression { get; }

        public string Name { get; private set; }

        public bool IsOutputParameter { get; }

        public IEnumerable<string> FormerlySerializedAsNames => _formerlySerializedAsNames;

        public bool IsCompatibleWith(ParameterData parameterData)
        {
            // TODO: take into account MovedFrom etc.?
            if (parameterData.IsOutput != IsOutputParameter) return false;
            return TypeExpression == parameterData.TypeExpression;
        }

        public void VisitNewerVersion(ParameterData parameterData)
        {
            if (!IsCompatibleWith(parameterData)) throw new InvalidOperationException("Incompatible parameter data");
            if (Name != parameterData.Name)
            {
                _formerlySerializedAsNames.Add(Name);
                Name = parameterData.Name;
            }
        }
    }
}
