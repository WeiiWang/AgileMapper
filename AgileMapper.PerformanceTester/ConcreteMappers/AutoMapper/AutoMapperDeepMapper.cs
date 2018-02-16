﻿namespace AgileObjects.AgileMapper.PerformanceTester.ConcreteMappers.AutoMapper
{
    using AbstractMappers;
    using global::AutoMapper;
    using TestClasses;

    internal class AutoMapperDeepMapper : DeepMapperBase
    {
        private IMapper _mapper;

        public override void Initialise()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<Address, Address>();
                cfg.CreateMap<Address, AddressDto>();
                cfg.CreateMap<Customer, CustomerDto>();
            });

            _mapper = config.CreateMapper();
        }

        protected override CustomerDto Map(Customer customer)
        {
            return _mapper.Map<Customer, CustomerDto>(customer);
        }
    }
}