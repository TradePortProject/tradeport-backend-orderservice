using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderManagement.Data;
using OrderManagement.Tests.TestEntities;

namespace OrderManagement.Tests.TestRepositories
{
    [ExcludeFromCodeCoverage]
    public class TestRepository : RepositoryBase<TestEntity>
    {
        public TestRepository(AppDbContext context) : base(context)
        {
        }
    }
}
