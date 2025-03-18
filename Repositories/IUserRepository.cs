namespace OrderManagement.Repositories
{
    public interface IUserRepository
    {
        Task<Dictionary<Guid, User>> GetUserInfoByRetailerIdAsync(List<Guid> retailerIDs);
    }
}
