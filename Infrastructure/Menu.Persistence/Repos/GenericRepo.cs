using Menu.Domain.Contracts;
using Menu.Domain.Entities;
using Menu.Persistence.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Menu.Persistence.Repos
{
    public class GenericRepo<T, TEntity>(StoreDbContext _context) : IGenericRepo<T, TEntity> where TEntity : BaseEntity<T>
    {
        public async Task<IEnumerable<TEntity>> GetAllAsync(ISpecifications<T, TEntity>? specifications = null, bool changeTracker = false)
        {
            IQueryable<TEntity> query = _context.Set<TEntity>();

            if(specifications != null)
            {
                query = ApplySpecifications(specifications);
            }

            return changeTracker ?
                await query.ToListAsync()
                :
                await query.AsNoTracking().ToListAsync();
        }

        public async Task<TEntity?> GetByIdAsync(ISpecifications<T, TEntity> specifications)
        {
            // ApplySpecifications → AsNoTracking → FirstOrDefaultAsync → return
            var query = ApplySpecifications(specifications);
            return await query.AsNoTracking().FirstOrDefaultAsync();
        }

        public async Task AddAsync(TEntity entity)
        {
            await _context.Set<TEntity>().AddAsync(entity);
        }
        public void Update(TEntity entity)
        {
            _context.Set<TEntity>().Update(entity);
        }
        public void Delete(TEntity entity)
        {
            _context.Set<TEntity>().Remove(entity);
        }

        // Connects the Evaluator to the Repository
        private IQueryable<TEntity> ApplySpecifications(ISpecifications<T, TEntity> specifications)
        {
            return SpecificationEvaluator<T, TEntity>.GetQuery(_context.Set<TEntity>(), specifications);
        }

        public Task<int> CountAsync(ISpecifications<T, TEntity> specifications)
        {
            var query = ApplySpecifications(specifications);
            return query.CountAsync();
        }

        public async Task<TEntity?> GetByIdAsync(T id)
        {
            return await _context.Set<TEntity>()
                .FindAsync(id);
        }
    }
}
