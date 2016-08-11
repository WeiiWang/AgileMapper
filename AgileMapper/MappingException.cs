﻿namespace AgileObjects.AgileMapper
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Members;
    using ReadableExpressions.Extensions;

    public class MappingException : Exception
    {
        internal static readonly ConstructorInfo ConstructorInfo =
            typeof(MappingException).GetConstructors(Constants.NonPublicInstance).First();

        public MappingException()
        {
        }

        internal MappingException(IMemberMapperCreationData data, Exception innerException)
            : base(GetMessage(data.MapperData), innerException)
        {
        }

        private static string GetMessage(MemberMapperData data)
        {
            var rootData = GetRootMapperData(data);

            var sourcePath = GetMemberPath(rootData.SourceType, data.SourceMember, "Source");
            var targetPath = GetMemberPath(rootData.TargetType, data.TargetMember, "Target");

            return $"An exception occurred mapping {sourcePath} -> {targetPath} with rule set {data.RuleSet.Name}.";
        }

        private static BasicMapperData GetRootMapperData(BasicMapperData data)
        {
            while (data.Parent != null)
            {
                data = data.Parent;
            }

            return data;
        }

        private static string GetMemberPath(Type rootType, IQualifiedMember member, string rootMemberName)
        {
            var rootTargetType = rootType.GetFriendlyName();
            var memberPath = member.GetPath();

            if (memberPath == rootMemberName)
            {
                return rootTargetType;
            }

            if (memberPath.StartsWith(rootMemberName, StringComparison.Ordinal))
            {
                return rootTargetType + memberPath.Substring(rootMemberName.Length);
            }

            var path = memberPath.Replace("omc." + rootMemberName + ".", rootTargetType + ".");

            return path;
        }
    }
}