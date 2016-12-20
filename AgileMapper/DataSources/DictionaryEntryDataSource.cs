﻿namespace AgileObjects.AgileMapper.DataSources
{
    using System.Linq.Expressions;
    using Extensions;
    using Members;

    internal class DictionaryEntryDataSource : DataSourceBase
    {
        private readonly DictionaryEntryVariablePair _dictionaryVariables;
        private Expression _preCondition;

        public DictionaryEntryDataSource(DictionarySourceMember sourceMember, IMemberMapperData childMapperData)
            : this(
                sourceMember.EntryMember,
                new DictionaryEntryVariablePair(sourceMember, childMapperData),
                childMapperData)
        {
        }

        private DictionaryEntryDataSource(
            IQualifiedMember sourceMember,
            DictionaryEntryVariablePair dictionaryVariables,
            IMemberMapperData childMapperData)
            : base(
                sourceMember,
                dictionaryVariables.Variables,
                GetDictionaryEntryValue(dictionaryVariables, childMapperData),
                GetValidEntryExistsTest(dictionaryVariables))
        {
            _dictionaryVariables = dictionaryVariables;
        }

        private static Expression GetDictionaryEntryValue(
            DictionaryEntryVariablePair dictionaryVariables,
            IMemberMapperData childMapperData)
        {
            if (dictionaryVariables.UseDirectValueAccess)
            {
                return dictionaryVariables.GetEntryValueAccess();
            }

            var valueConversion = childMapperData
                .MapperContext
                .ValueConverters
                .GetConversion(dictionaryVariables.Value, childMapperData.TargetMember.Type);

            return valueConversion;
        }

        private static Expression GetValidEntryExistsTest(DictionaryEntryVariablePair dictionaryVariables)
        {
            if (dictionaryVariables.UseDirectValueAccess)
            {
                return null;
            }

            var dictionaryEntryAccess = dictionaryVariables.GetEntryValueAccess();
            var valueVariableAssignment = Expression.Assign(dictionaryVariables.Value, dictionaryEntryAccess);
            var valueNonNull = valueVariableAssignment.GetIsNotDefaultComparison();

            return valueNonNull;
        }

        public override Expression PreCondition => _preCondition ?? (_preCondition = CreatePreCondition());

        private Expression CreatePreCondition()
        {
            var matchingKeyExists = GetMatchingKeyExistsTest();

            if (_dictionaryVariables.HasConstantTargetMemberKey)
            {
                return matchingKeyExists;
            }

            // TODO: Test coverage: dictionary to enumerable of parameterised constructor types
            var keyAssignment = GetNonConstantKeyValueAssignment();

            return Expression.Block(keyAssignment, matchingKeyExists);
        }

        public override Expression AddPreCondition(Expression population)
        {
            var matchingKeyExists = GetMatchingKeyExistsTest();
            var ifKeyExistsPopulate = Expression.IfThen(matchingKeyExists, population);

            if (_dictionaryVariables.HasConstantTargetMemberKey)
            {
                return ifKeyExistsPopulate;
            }

            var keyAssignment = GetNonConstantKeyValueAssignment();

            return Expression.Block(keyAssignment, ifKeyExistsPopulate);
        }

        private Expression GetMatchingKeyExistsTest()
        {
            var keyVariableAssignment = _dictionaryVariables.GetMatchingKeyAssignment();
            var matchingKeyExists = keyVariableAssignment.GetIsNotDefaultComparison();

            return matchingKeyExists;
        }

        private Expression GetNonConstantKeyValueAssignment()
            => _dictionaryVariables.GetKeyAssignment(_dictionaryVariables.TargetMemberKey);

        //public override Expression AddCondition(Expression value, Expression alternateBranch = null)
        //{
        //    var conditional = base.AddCondition(value, alternateBranch);

        //    if (_dictionaryVariables.UseDirectValueAccess)
        //    {
        //        return conditional;
        //    }

        //    return Expression.Block(new[] { _dictionaryVariables.Value }, conditional);
        //}
    }
}