using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrderManagement.Tests.TestEntities
{
    [ExcludeFromCodeCoverage]
    public class TestEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}
