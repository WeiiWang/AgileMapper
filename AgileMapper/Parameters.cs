namespace AgileObjects.AgileMapper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using Extensions;
    using Members;
    using ObjectPopulation;

    internal static class Parameters
    {
        public static readonly ParameterExpression MappingContext = Create<MappingContext>();
        public static readonly ParameterExpression MappingData = Create<IMappingData>();
        public static readonly ParameterExpression ObjectMapperData = Create<ObjectMapperData>();
        public static readonly ParameterExpression ObjectMappingCreationData = Create<IObjectMapperCreationData>();

        public static readonly ParameterExpression EnumerableIndex = Create<int>("i");
        public static readonly ParameterExpression EnumerableIndexNullable = Create<int?>("i");

        public static ParameterExpression Create<T>(string name = null) => Create(typeof(T), name);

        public static ParameterExpression Create(Type type) => Create(type, type.GetShortVariableName());

        public static ParameterExpression Create(Type type, string name)
            => Expression.Parameter(type, name ?? type.GetShortVariableName());

        #region Parameter Swapping

        public static Func<LambdaExpression, MemberMapperData, Expression> SwapNothing = (lambda, mapperData) => lambda.Body;

        public static Func<LambdaExpression, MemberMapperData, Expression> SwapForContextParameter = (lambda, mapperData) =>
        {
            var contextParameter = lambda.Parameters[0];
            var contextType = contextParameter.Type;

            if (contextType.IsAssignableFrom(mapperData.Parameter.Type))
            {
                return lambda.ReplaceParameterWith(mapperData.Parameter);
            }

            var contextTypes = contextType.GetGenericArguments();
            var contextInfo = GetAppropriateMappingContext(contextTypes, mapperData);

            if (lambda.Body.NodeType != ExpressionType.Invoke)
            {
                var memberContextType = (contextTypes.Length == 2) ? contextType : contextType.GetInterfaces()[0];
                var sourceProperty = memberContextType.GetProperty("Source", Constants.PublicInstance);
                var targetProperty = memberContextType.GetProperty("Target", Constants.PublicInstance);
                var indexProperty = memberContextType.GetProperty("EnumerableIndex", Constants.PublicInstance);

                var replacementsByTarget = new Dictionary<Expression, Expression>(EquivalentMemberAccessComparer.Instance)
                {
                    [Expression.Property(contextParameter, sourceProperty)] = contextInfo.SourceAccess,
                    [Expression.Property(contextParameter, targetProperty)] = contextInfo.TargetAccess,
                    [Expression.Property(contextParameter, indexProperty)] = contextInfo.Index
                };

                if (contextTypes.Length == 3)
                {
                    replacementsByTarget.Add(
                        Expression.Property(contextParameter, "CreatedObject"),
                        contextInfo.InstanceVariable);
                }

                return lambda.Body.Replace(replacementsByTarget);
            }

            return GetInvocationContextArgument(contextInfo, lambda);
        };

        private static Expression GetInvocationContextArgument(MappingContextInfo contextInfo, LambdaExpression lambda)
        {
            if (contextInfo.ContextTypes.Length == 2)
            {
                return lambda.ReplaceParameterWith(contextInfo.MappingDataAccess);
            }

            var objectCreationContextCreateCall = Expression.Call(
                ObjectCreationContext.CreateMethod.MakeGenericMethod(contextInfo.ContextTypes),
                contextInfo.MappingDataAccess,
                contextInfo.InstanceVariable);

            return lambda.ReplaceParameterWith(objectCreationContextCreateCall);
        }

        public static Func<LambdaExpression, MemberMapperData, Expression> SwapForSourceAndTarget = (lambda, mapperData) =>
            ReplaceParameters(lambda, mapperData, c => c.SourceAccess, c => c.TargetAccess);

        public static Func<LambdaExpression, MemberMapperData, Expression> SwapForSourceTargetAndIndex = (lambda, mapperData) =>
            ReplaceParameters(lambda, mapperData, c => c.SourceAccess, c => c.TargetAccess, c => c.Index);

        public static Func<LambdaExpression, MemberMapperData, Expression> SwapForSourceTargetAndInstance = (lambda, mapperData) =>
            ReplaceParameters(lambda, mapperData, c => c.SourceAccess, c => c.TargetAccess, c => c.InstanceVariable);

        public static Func<LambdaExpression, MemberMapperData, Expression> SwapForSourceTargetInstanceAndIndex = (lambda, mapperData) =>
            ReplaceParameters(lambda, mapperData, c => c.SourceAccess, c => c.TargetAccess, c => c.InstanceVariable, c => c.Index);

        private static Expression ReplaceParameters(
            LambdaExpression lambda,
            MemberMapperData mapperData,
            params Func<MappingContextInfo, Expression>[] parameterFactories)
        {
            var contextInfo = GetAppropriateMappingContext(lambda, mapperData);
            return lambda.ReplaceParametersWith(parameterFactories.Select(f => f.Invoke(contextInfo)).ToArray());
        }

        private static MappingContextInfo GetAppropriateMappingContext(LambdaExpression lambda, MemberMapperData mapperData)
            => GetAppropriateMappingContext(new[] { lambda.Parameters[0].Type, lambda.Parameters[1].Type }, mapperData);

        private static MappingContextInfo GetAppropriateMappingContext(Type[] contextTypes, MemberMapperData mapperData)
        {
            if (TypesMatch(contextTypes, mapperData))
            {
                return new MappingContextInfo(mapperData, contextTypes);
            }

            var originalContext = mapperData;
            var dataAccess = GetAppropriateMappingContextAccess(contextTypes, mapperData);

            return new MappingContextInfo(originalContext, dataAccess, contextTypes);
        }

        private static Expression GetAppropriateMappingContextAccess(Type[] contextTypes, MemberMapperData mapperData)
        {
            if (TypesMatch(contextTypes, mapperData))
            {
                return mapperData.Parameter;
            }

            Expression dataAccess = mapperData.Parameter;

            if (mapperData.TargetMember.IsSimple)
            {
                mapperData = mapperData.Parent;
            }

            while (!TypesMatch(contextTypes, mapperData))
            {
                dataAccess = Expression.Property(dataAccess, "Parent");
                mapperData = mapperData.Parent;
            }

            return dataAccess;
        }

        public static Expression GetAppropriateTypedMappingContextAccess(Type[] contextTypes, MemberMapperData mapperData)
        {
            var access = GetAppropriateMappingContextAccess(contextTypes, mapperData);
            var typedAccess = MappingContextInfo.GetTypedContextAccess(mapperData, access, contextTypes);

            return typedAccess;
        }

        private static bool TypesMatch(IList<Type> contextTypes, BasicMapperData data)
            => contextTypes[0].IsAssignableFrom(data.SourceType) && contextTypes[1].IsAssignableFrom(data.TargetType);

        private class MappingContextInfo
        {
            private static readonly MethodInfo _getSourceMethod = typeof(IMappingData).GetMethod("GetSource", Constants.PublicInstance);
            private static readonly MethodInfo _getTargetMethod = typeof(IMappingData).GetMethod("GetTarget", Constants.PublicInstance);
            private static readonly MethodInfo _asMethod = typeof(IMappingData).GetMethod("As", Constants.PublicInstance);

            public MappingContextInfo(MemberMapperData data, Type[] contextTypes)
                : this(data, data.Parameter, contextTypes)
            {
            }

            public MappingContextInfo(
                MemberMapperData data,
                Expression contextAccess,
                Type[] contextTypes)
            {
                ContextTypes = contextTypes;
                InstanceVariable = data.InstanceVariable;
                SourceAccess = GetAccess(data, contextAccess, _getSourceMethod, contextTypes[0], data.SourceObject);
                TargetAccess = GetAccess(data, contextAccess, _getTargetMethod, contextTypes[1], data.TargetObject);
                Index = data.EnumerableIndex;
                MappingDataAccess = GetTypedContextAccess(data, contextAccess, contextTypes);
            }

            private static Expression GetAccess(
                MemberMapperData data,
                Expression contextAccess,
                MethodInfo accessMethod,
                Type type,
                Expression directAccessExpression)
            {
                if (contextAccess == data.Parameter)
                {
                    return directAccessExpression;
                }

                return Expression.Call(contextAccess, accessMethod.MakeGenericMethod(type));
            }

            public static Expression GetTypedContextAccess(MemberMapperData data, Expression contextAccess, Type[] contextTypes)
            {
                if (contextAccess == data.Parameter)
                {
                    return data.Parameter;
                }

                return Expression.Call(
                    contextAccess,
                    _asMethod.MakeGenericMethod(contextTypes[0], contextTypes[1]));
            }

            public Type[] ContextTypes { get; }

            public Expression InstanceVariable { get; }

            public Expression MappingDataAccess { get; }

            public Expression SourceAccess { get; }

            public Expression TargetAccess { get; }

            public Expression Index { get; }
        }

        private class EquivalentMemberAccessComparer : IEqualityComparer<Expression>
        {
            public static readonly IEqualityComparer<Expression> Instance = new EquivalentMemberAccessComparer();

            public bool Equals(Expression x, Expression y)
            {
                if (x.NodeType != y.NodeType)
                {
                    return false;
                }

                var memberAccessX = (MemberExpression)x;
                var memberAccessY = (MemberExpression)y;

                return memberAccessX.Member == memberAccessY.Member;
            }

            public int GetHashCode(Expression obj) => 0;
        }

        #endregion
    }
}