using ComplejoDeportivo.Infrastructure.Persistence;
using ComplejoDeportivo.Domain;
using ComplejoDeportivo.Application.Repositories;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ComplejoDeportivo.Infrastructure.Repositories
{

	public class CanchaRepository : ICanchaRepository
	{
		private readonly ComplejoDeportivoContext _context;

		public CanchaRepository(ComplejoDeportivoContext context)
		{
			_context = context;
		}

		public async Task<Cancha> CreateAsync(Cancha cancha)
		{
			_context.Canchas.Add(cancha);
			await _context.SaveChangesAsync();
			return cancha;
		}

		public async Task<bool> DeleteAsync(int id)
		{
			var cancha = await _context.Canchas.FindAsync(id);
			if (cancha == null)
			{
				return false;
			}
			_context.Canchas.Remove(cancha);
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<IEnumerable<Cancha>> GetAllAsync()
		{
			return await _context.Canchas.ToListAsync();
		}

		public async Task<Cancha?> GetByIdAsync(int id)
		{
			return await _context.Canchas.FindAsync(id);
		}

		public async Task<bool> UpdateAsync(Cancha cancha)
		{
			_context.Entry(cancha).State = EntityState.Modified;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> ActivarAsync(int id)
		{
			var cancha = await _context.Canchas.FindAsync(id);
			if (cancha == null)
			{
				return false;
			}
			cancha.Activa = true;
			_context.Entry(cancha).State = EntityState.Modified;
			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DesactivarAsync(int id)
		{
			var cancha = await _context.Canchas.FindAsync(id);
			if (cancha == null)
			{
				return false;
			}
			cancha.Activa = false;
			_context.Entry(cancha).State = EntityState.Modified;
			await _context.SaveChangesAsync();
			return true;
		}
        // [NUEVO] (Problema 4)
        public async Task<IEnumerable<Cancha>> GetByComplejoIdAsync(int complejoId)
        {
            return await _context.Canchas
                .Where(c => c.ComplejoId == complejoId)
                .ToListAsync();
        }
    }
}