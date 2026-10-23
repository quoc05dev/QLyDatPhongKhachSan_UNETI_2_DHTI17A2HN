using System.Linq;

namespace QLyDatPhongKhachSan.Repositories
{
    /// <summary>
    /// Kho generic cho thuc don Repository - Interface rieng de Controller
    /// khong truc tiep truy cap AppDbContext.
    /// </summary>
    public interface IGenericRepository<T> where T : class
    {
        IQueryable<T> Query();
        ValueTask<T?> FindAsync(object id);
        void Add(T entity);
        void Update(T entity);
        void Remove(T entity);
        Task<int> SaveChangesAsync();
        int SaveChanges();
    }
}
