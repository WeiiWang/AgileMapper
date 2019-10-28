namespace AgileObjects.AgileMapper.DataSources
{
    using System.Collections;
    using System.Collections.Generic;
#if NET35
    using Microsoft.Scripting.Ast;
#else
    using System.Linq.Expressions;
#endif
    using Members;

    internal class EmptyDataSourceSet : IDataSourceSet
    {
        public EmptyDataSourceSet(IMemberMapperData mapperData)
        {
            MapperData = mapperData;
        }

        public IMemberMapperData MapperData { get; }

        public bool None => true;

        public bool HasValue => false;

        public bool IsConditional => false;

        public Expression SourceMemberTypeTest => null;

        public IList<ParameterExpression> Variables
            => Enumerable<ParameterExpression>.EmptyArray;

        public IDataSource this[int index] => null;

        public int Count => 0;

        public Expression BuildValue() => Constants.EmptyExpression;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IEnumerator<IDataSource> GetEnumerator()
            => Enumerable<IDataSource>.Empty.GetEnumerator();
    }
}