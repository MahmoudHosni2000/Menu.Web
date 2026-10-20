using Menu.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Menu.Domain.Contracts
{
    public interface IGenericRepo<T, TEntity> where TEntity : BaseEntity<T>
    {
        Task<IEnumerable<TEntity>> GetAllAsync(
            ISpecifications<T, TEntity>? specifications = null,
            bool changeTracker = false);
        Task<TEntity?> GetByIdAsync(ISpecifications<T, TEntity> specifications);
        Task AddAsync(TEntity entity);
        void Update(TEntity entity);
        void Delete(TEntity entity);
        Task<int> CountAsync(
        ISpecifications<T, TEntity> specifications);

        Task<TEntity?> GetByIdAsync(T id);
    }
}
