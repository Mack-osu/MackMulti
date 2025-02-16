using MackMultiBot.Database;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MackMulti.Database.Databases
{
	public class BaseDatabase<T> where T : class
	{
		protected readonly BotDatabaseContext _dbContext = new();

		public async Task<T?> GetAsync(int id)
		{
			return await _dbContext.Set<T>().FindAsync(id);
		}

		public async Task<IReadOnlyList<T>> GetAllAsync()
		{
			return await _dbContext.Set<T>().ToListAsync();
		}

		public async Task AddAsync(T entity)
		{
			await _dbContext.Set<T>().AddAsync(entity);
		}

		public async Task AddRangeAsync(IEnumerable<T> entities)
		{
			await _dbContext.Set<T>().AddRangeAsync(entities);
		}

		public void Delete(T entity)
		{
			_dbContext.Set<T>().Remove(entity);
		}

		public void DeleteRange(IEnumerable<T> entities)
		{
			_dbContext.Set<T>().RemoveRange(entities);
		}

		public async Task<long> CountAsync()
		{
			return await _dbContext.Set<T>().CountAsync();
		}

		public async Task SaveAsync()
		{
			await _dbContext.SaveChangesAsync();
		}

		public async ValueTask DisposeAsync()
		{
			await _dbContext.DisposeAsync();

			GC.SuppressFinalize(this);
		}
	}
}
