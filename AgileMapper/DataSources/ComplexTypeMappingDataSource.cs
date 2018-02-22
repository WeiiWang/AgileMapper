﻿namespace AgileObjects.AgileMapper.DataSources
{
    using System.Linq.Expressions;
    using Members;
    using ObjectPopulation;

    internal class ComplexTypeMappingDataSource : DataSourceBase
    {
        public ComplexTypeMappingDataSource(
            IDataSource complexTypeDataSource,
            int dataSourceIndex,
            IChildMemberMappingData complexTypeMappingData)
            : base(
                  complexTypeDataSource,
                  GetMapping(complexTypeDataSource, dataSourceIndex, complexTypeMappingData))
        {
        }

        private static Expression GetMapping(
            IDataSource complexTypeDataSource,
            int dataSourceIndex,
            IChildMemberMappingData complexTypeMappingData)
        {
            if (complexTypeMappingData.IsRepeatMapping(complexTypeDataSource.Value.Type))
            {
                
            }

            var mapping = MappingFactory.GetChildMapping(
                complexTypeDataSource.SourceMember,
                complexTypeDataSource.Value,
                dataSourceIndex,
                complexTypeMappingData);

            return mapping;
        }

        public ComplexTypeMappingDataSource(int dataSourceIndex, IChildMemberMappingData complexTypeMappingData)
            : base(complexTypeMappingData.MapperData.SourceMember, GetMapping(dataSourceIndex, complexTypeMappingData))
        {
        }

        private static Expression GetMapping(int dataSourceIndex, IChildMemberMappingData complexTypeMappingData)
        {
            var complexTypeMapperData = complexTypeMappingData.MapperData;
            var relativeMember = complexTypeMapperData.SourceMember.RelativeTo(complexTypeMapperData.SourceMember);
            var sourceMemberAccess = relativeMember.GetQualifiedAccess(complexTypeMapperData);

            return MappingFactory.GetChildMapping(
                relativeMember,
                sourceMemberAccess,
                dataSourceIndex,
                complexTypeMappingData);
        }
    }
}