using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Menu.Domain.Contracts;
using Menu.Domain.Entities;
using Menu.Persistence.Data.Contexts;
using Menu.Persistence.Repos;

namespace Menu.Persistence
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly StoreDbContext _context;
        public UnitOfWork(StoreDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }


        // way 1: using a dictionary to store repositories

        //private Dictionary<string, object> _repositories = new Dictionary<string, object>();
        //public IGenericRepo<T, TEntity> GetRepository<T, TEntity>() where TEntity : BaseEntity<T>
        //{
        //    // Get the name of the entity type ..ex "Product" for ProductEntity
        //    var type = typeof(TEntity).Name; // e.g., "Product" for ProductEntity

        //    // Check if the repository for this entity type already exists in the dictionary
        //    if (context == null) throw new ArgumentNullException(nameof(context));

        //    // If it doesn't exist, create a new instance of the GenericRepo for this entity type and add it to the dictionary
        //    if (!_repositories.ContainsKey(type))
        //    {
        //        // Create a new instance of the GenericRepo for this entity type
        //        var repository = new GenericRepo<T, TEntity>(context);
        //        // Add the new repository instance to the dictionary
        //        _repositories.Add(type, repository);
        //    }

        //    // Return the repository instance from the dictionary, casting it to the appropriate type
        //    return (IGenericRepo<T, TEntity>)_repositories[type];
        //}

        // Way 2 : Using ConcurrentDictionary for thread-safe access to repositories

        private readonly ConcurrentDictionary<string, object> _repositories = new ConcurrentDictionary<string, object>();

        public IGenericRepo<T, TEntity> GetRepo<T, TEntity>() where TEntity : BaseEntity<T>
        {
            var repo = (IGenericRepo<T, TEntity>)_repositories.GetOrAdd(typeof(TEntity).Name, _ => new GenericRepo<T, TEntity>(_context));
            return repo;
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
