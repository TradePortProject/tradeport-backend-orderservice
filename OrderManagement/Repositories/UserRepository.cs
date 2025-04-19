using Microsoft.EntityFrameworkCore;
using OrderManagement.Data;

namespace OrderManagement.Repositories
{
    public class UserRepository :RepositoryBase<User>, IUserRepository
    {
        private readonly AppDbContext dbContext;

        public UserRepository(AppDbContext dbContextRepo) : base(dbContextRepo)
        {
            this.dbContext = dbContextRepo;
        }

        public async Task<Dictionary<Guid, User>> GetUserInfoByRetailerIdAsync(List<Guid> retailerIDs)
        {
            return await dbContext.Users
                .Where(user => retailerIDs.Contains(user.UserID))
                .ToDictionaryAsync(user => user.UserID, user => user);
        }
    }
}
