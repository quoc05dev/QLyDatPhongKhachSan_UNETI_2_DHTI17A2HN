using Microsoft.EntityFrameworkCore;
using QLyDatPhongKhachSan.Data;

namespace QLyDatPhongKhachSan.Repositories
{
    /// <summary>
    /// Cau truc Repository generic dung chung cho toan bo du an.
    /// Cac Repository cung mot scope se chia se mot AppDbContext (Unit of Work ngam).
    /// </summary>
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        private readonly AppDbContext _context;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
        }

        public IQueryable<T> Query() => _context.Set<T>();

        public ValueTask<T?> FindAsync(object id) => _context.Set<T>().FindAsync(id);

        public void Add(T entity) => _context.Set<T>().Add(entity);

        public void Update(T entity) => _context.Set<T>().Update(entity);

        public void Remove(T entity) => _context.Set<T>().Remove(entity);

        public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

        public int SaveChanges() => _context.SaveChanges();
    }
}
