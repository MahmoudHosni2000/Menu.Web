using Menu.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Menu.Domain.Contracts
{
    public interface IUnitOfWork
    {
        IGenericRepo<T, TEntity> GetRepo<T, TEntity>() where TEntity : BaseEntity<T>;
        Task<int> SaveChangesAsync();
    }
}
